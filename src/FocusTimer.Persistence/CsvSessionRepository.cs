#pragma warning disable

namespace FocusTimer.Persistence;

using System.Collections.Concurrent;
using System.Globalization;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

/// <summary>Current-schema RFC 4180 CSV implementation of <see cref="IWorklogStore"/>.</summary>
public sealed class CsvSessionRepository : IWorklogStore
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new(StringComparer.OrdinalIgnoreCase);
    private readonly ISettingsProvider _settingsProvider;
    private readonly IAppLogger? _logger;

    /// <summary>Initializes the store.</summary>
    public CsvSessionRepository(ISettingsProvider settingsProvider, IAppLogger? logger = null) { this._settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider)); this._logger = logger; }

    /// <inheritdoc/>
    public async Task<WorklogOutcome> AppendAsync(IReadOnlyCollection<TimeEntry> entries, CancellationToken cancellationToken = default)
    {
        if (entries is null || entries.Count == 0)
            return WorklogOutcome.Success();
        var errors = entries.SelectMany(WorklogEntryValidator.Validate).ToList();
        if (errors.Count > 0)
            return new(WorklogOutcomeKind.ValidationFailure, string.Join(" ", errors));
        var settings = await this._settingsProvider.LoadAsync();
        var root = ResolveRoot(settings);
        foreach (var group in entries.GroupBy(e => e.StartedAt.Date))
        {
            var path = GetPath(root, group.Key);
            var gate = Locks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(cancellationToken);
            try
            {
                var read = await ReadFileAsync(path, cancellationToken);
                if (read.Outcome.Kind == WorklogOutcomeKind.NotFound)
                {
                    read = new FileRead(WorklogOutcome.Success(), []);
                }

                if (!read.Outcome.IsSuccess)
                    return read.Outcome;
                var existing = read.Entries.ToDictionary(e => e.EntryId, StringComparer.Ordinal);
                foreach (var entry in group)
                {
                    if (existing.TryGetValue(entry.EntryId, out var prior))
                    { if (prior != entry) return new(WorklogOutcomeKind.Conflict, "Entry ID already exists with different content."); }
                    else
                    { existing.Add(entry.EntryId, entry); read.Entries.Add(entry); }
                }
                await WriteAtomicallyAsync(path, read.Entries, cancellationToken);
            }
            catch (IOException ex) { return new(WorklogOutcomeKind.FileInUse, ex.Message); }
            catch (Exception ex) { return new(WorklogOutcomeKind.IoFailure, ex.Message); }
            finally { gate.Release(); }
        }
        return WorklogOutcome.Success();
    }

    /// <inheritdoc/>
    public async Task<WorklogReadResult> GetAsync(string entryId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entryId))
            return new(new(WorklogOutcomeKind.ValidationFailure, "Entry ID is required."), []);
        var root = ResolveRoot(await this._settingsProvider.LoadAsync());
        if (!Directory.Exists(root))
            return new(WorklogOutcome.Success(), []);

        var matches = new List<TimeEntry>();
        foreach (var path in Directory.EnumerateFiles(root, "*-worklog.csv", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var read = await ReadFileAsync(path, cancellationToken);
            if (!read.Outcome.IsSuccess)
                return new(read.Outcome, matches);
            matches.AddRange(read.Entries.Where(entry => entry.EntryId == entryId));
        }

        return matches.Count > 1
            ? new(new(WorklogOutcomeKind.Conflict, "Entry ID exists in more than one worklog file."), matches)
            : new(WorklogOutcome.Success(), matches);
    }

    /// <inheritdoc/>
    public async Task<WorklogReadResult> QueryAsync(WorklogQuery query, CancellationToken cancellationToken = default)
    {
        if (!query.IsValid)
            return new(new(WorklogOutcomeKind.ValidationFailure, "Query end must be later than start."), []);
        var root = ResolveRoot(await this._settingsProvider.LoadAsync());
        var entries = new List<TimeEntry>();
        var warnings = new List<WorklogWarning>();
        var finalDate = query.EndExclusive.TimeOfDay == TimeSpan.Zero
            ? query.EndExclusive.Date.AddDays(-1)
            : query.EndExclusive.Date;
        for (var day = query.StartInclusive.Date; day <= finalDate; day = day.AddDays(1))
        {
            var read = await ReadFileAsync(GetPath(root, day), cancellationToken);
            if (!read.Outcome.IsSuccess)
            {
                if (read.Outcome.Kind == WorklogOutcomeKind.NotFound)
                    continue;
                return new(read.Outcome, entries);
            }
            entries.AddRange(read.Entries);
            if (read.Outcome.Warnings is not null)
                warnings.AddRange(read.Outcome.Warnings);
        }
        bool Eq(string? left, string? right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        return new(WorklogOutcome.Success(warnings), entries.Where(e => e.StartedAt < query.EndExclusive && e.EndedAt > query.StartInclusive &&
            (query.Application is null || Eq(e.AppName, query.Application)) && (query.Project is null || Eq(e.ProjectTag, query.Project)) &&
            (query.SessionId is null || e.SessionId == query.SessionId) && (!query.ActivityKind.HasValue || e.ActivityKind == query.ActivityKind) &&
            (!query.CaptureSource.HasValue || e.CaptureSource == query.CaptureSource)).OrderBy(e => e.StartedAt).ToList());
    }

    /// <inheritdoc/>
    public Task<WorklogOutcome> PatchAsync(string entryId, int expectedRevision, WorklogPatch patch, CancellationToken cancellationToken = default) => this.MutateAsync(entryId, expectedRevision, old => old with { StartedAt = patch.StartedAt, EndedAt = patch.EndedAt, AppName = patch.AppName, WindowTitle = patch.WindowTitle, ProjectTag = patch.ProjectTag, ProjectAssignmentSource = patch.ProjectAssignmentSource, ProjectRuleId = patch.ProjectRuleId, ActivityKind = patch.ActivityKind, EndReason = patch.EndReason, CaptureSource = patch.CaptureSource, Revision = old.Revision + 1, LastModifiedAtUtc = DateTimeOffset.UtcNow }, cancellationToken);
    /// <inheritdoc/>
    public Task<WorklogOutcome> DeleteAsync(string entryId, int expectedRevision, CancellationToken cancellationToken = default) => this.MutateAsync(entryId, expectedRevision, _ => null, cancellationToken);

    private async Task<WorklogOutcome> MutateAsync(string entryId, int expectedRevision, Func<TimeEntry, TimeEntry?> mutate, CancellationToken cancellationToken)
    {
        var found = await this.GetAsync(entryId, cancellationToken);
        if (!found.Outcome.IsSuccess)
            return found.Outcome;
        var old = found.Entries.SingleOrDefault();
        if (old is null)
            return new(WorklogOutcomeKind.NotFound);
        if (old.Revision != expectedRevision)
            return new(WorklogOutcomeKind.Conflict, "Revision does not match.");
        var changed = mutate(old);
        if (changed is not null && WorklogEntryValidator.Validate(changed).Count > 0)
            return new(WorklogOutcomeKind.ValidationFailure, "Patched entry is invalid.");
        if (changed is not null && changed.StartedAt.Date != old.StartedAt.Date)
            return new(WorklogOutcomeKind.ValidationFailure, "An entry cannot move to another local day.");
        var path = GetPath(ResolveRoot(await this._settingsProvider.LoadAsync()), old.StartedAt.Date);
        var gate = Locks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        { var read = await ReadFileAsync(path, cancellationToken); if (!read.Outcome.IsSuccess) return read.Outcome; var index = read.Entries.FindIndex(e => e.EntryId == entryId); if (index < 0) return new(WorklogOutcomeKind.NotFound); if (read.Entries[index].Revision != expectedRevision) return new(WorklogOutcomeKind.Conflict); if (changed is null) read.Entries.RemoveAt(index); else read.Entries[index] = changed; await WriteAtomicallyAsync(path, read.Entries, cancellationToken); return WorklogOutcome.Success(); }
        catch (IOException ex) { return new(WorklogOutcomeKind.FileInUse, ex.Message); }
        catch (Exception ex) { return new(WorklogOutcomeKind.IoFailure, ex.Message); }
        finally { gate.Release(); }
    }

    private static string ResolveRoot(Settings settings) => string.IsNullOrWhiteSpace(settings.WorklogDirectory) ? Settings.DefaultWorklogDirectory : settings.WorklogDirectory;
    private static string GetPath(string root, DateTime date) => Path.Combine(root, date.ToString("yyyy", CultureInfo.InvariantCulture), date.ToString("MM", CultureInfo.InvariantCulture), $"{date:yyyy-MM-dd}-worklog.csv");
    private static async Task WriteAtomicallyAsync(string path, List<TimeEntry> entries, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + ".worklog.tmp";
        await using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
        {
            await new CsvWorklogCodec().WriteAsync(stream, entries.OrderBy(entry => entry.StartedAt), ct);
        }
        var validate = await ReadFileAsync(temp, ct);
        if (!validate.Outcome.IsSuccess || validate.Entries.Count != entries.Count)
            throw new InvalidDataException("Replacement validation failed.");
        File.Move(temp, path, true);
    }
    private static async Task<FileRead> ReadFileAsync(string path, CancellationToken ct)
    {
        if (!File.Exists(path))
            return new(new(WorklogOutcomeKind.NotFound), []);
        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var read = await new CsvWorklogCodec().ReadAsync(stream, path, ct);
            return new(read.Outcome, read.Entries.ToList());
        }
        catch (IOException ex) { return new(new(WorklogOutcomeKind.FileInUse, ex.Message), []); }
        catch (Exception ex) { return new(new(WorklogOutcomeKind.MalformedData, ex.Message), []); }
    }
    private sealed record FileRead(WorklogOutcome Outcome, List<TimeEntry> Entries);
}
