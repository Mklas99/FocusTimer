#pragma warning disable

namespace FocusTimer.Persistence;

using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

/// <summary>Current-schema RFC 4180 CSV implementation of <see cref="IWorklogStore"/>.</summary>
public sealed class CsvSessionRepository : IWorklogStore
{
    private const string SchemaVersion = "1";
    private static readonly string[] Header = ["SchemaVersion", "EntryId", "SessionId", "StartedAt", "EndedAt", "DurationSeconds", "AppName", "WindowTitle", "ProjectTag", "ProjectAssignmentSource", "ProjectRuleId", "ActivityKind", "EndReason", "CaptureSource", "SourcePlatform", "SourceDeviceId", "Revision", "LastModifiedAtUtc"];
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
        await using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
        { await writer.WriteLineAsync(string.Join(',', Header)); foreach (var entry in entries.OrderBy(e => e.StartedAt)) { ct.ThrowIfCancellationRequested(); await writer.WriteLineAsync(Format(entry)); } await writer.FlushAsync(ct); }
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
            var text = await File.ReadAllTextAsync(path, ct);
            var rows = Parse(text);
            if (rows.Count == 0 || !rows[0].SequenceEqual(Header))
                return new(new(WorklogOutcomeKind.UnsupportedSchema, "The file does not use the current worklog schema."), []);
            var entries = new List<TimeEntry>();
            var warnings = new List<WorklogWarning>();
            for (var i = 1; i < rows.Count; i++)
            { try { entries.Add(ParseEntry(rows[i])); } catch (Exception ex) { warnings.Add(new(path, i + 1, ex.Message)); } }
            return new(WorklogOutcome.Success(warnings), entries);
        }
        catch (IOException ex) { return new(new(WorklogOutcomeKind.FileInUse, ex.Message), []); }
        catch (Exception ex) { return new(new(WorklogOutcomeKind.MalformedData, ex.Message), []); }
    }
    private static TimeEntry ParseEntry(IReadOnlyList<string> v) => new(v[1], v[2], DateTimeOffset.Parse(v[3], CultureInfo.InvariantCulture), DateTimeOffset.Parse(v[4], CultureInfo.InvariantCulture), v[6], v[7], Empty(v[8]), ParseProject(v[9]), Empty(v[10]), ActivityKind.Active, ParseEnd(v[12]), CaptureSource.ActiveWindow, ParsePlatform(v[14]), Empty(v[15]), int.Parse(v[16], CultureInfo.InvariantCulture), DateTimeOffset.Parse(v[17], CultureInfo.InvariantCulture));
    private static string? Empty(string value) => value.Length == 0 ? null : value;
    private static ProjectAssignmentSource ParseProject(string value) => value switch { "session" => ProjectAssignmentSource.Session, "rule" => ProjectAssignmentSource.Rule, "editor" => ProjectAssignmentSource.Editor, "imported" => ProjectAssignmentSource.Imported, _ => ProjectAssignmentSource.Unassigned };
    private static EndReason ParseEnd(string value) => value switch { "application-change" => EndReason.ApplicationChange, "manual-pause" => EndReason.ManualPause, "idle-pause" => EndReason.IdlePause, "day-boundary" => EndReason.DayBoundary, "application-exit" => EndReason.ApplicationExit, _ => EndReason.Unknown };
    private static SourcePlatform ParsePlatform(string value) => value switch { "windows" => SourcePlatform.Windows, "linux" => SourcePlatform.Linux, "macos" => SourcePlatform.MacOS, _ => SourcePlatform.Unknown };
    private static string Format(TimeEntry e) => string.Join(',', new[] { SchemaVersion, e.EntryId, e.SessionId, e.StartedAt.ToString("O"), e.EndedAt.ToString("O"), e.Duration.TotalSeconds.ToString(CultureInfo.InvariantCulture), e.AppName, e.WindowTitle, e.ProjectTag ?? string.Empty, WorklogValueCodec.ToStoredValue(e.ProjectAssignmentSource), e.ProjectRuleId ?? string.Empty, WorklogValueCodec.ToStoredValue(e.ActivityKind), WorklogValueCodec.ToStoredValue(e.EndReason), WorklogValueCodec.ToStoredValue(e.CaptureSource), WorklogValueCodec.ToStoredValue(e.SourcePlatform), e.SourceDeviceId ?? string.Empty, e.Revision.ToString(CultureInfo.InvariantCulture), e.LastModifiedAtUtc.ToString("O") }.Select(Escape));
    private static string Escape(string value) => value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0 ? '"' + value.Replace("\"", "\"\"") + '"' : value;
    private static List<List<string>> Parse(string text) { var rows = new List<List<string>>(); var row = new List<string>(); var value = new StringBuilder(); var quote = false; for (var i = 0; i < text.Length; i++) { var c = text[i]; if (quote) { if (c == '"' && i + 1 < text.Length && text[i + 1] == '"') { value.Append(c); i++; } else if (c == '"') quote = false; else value.Append(c); } else if (c == '"') quote = true; else if (c == ',') { row.Add(value.ToString()); value.Clear(); } else if (c == '\r' || c == '\n') { if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++; row.Add(value.ToString()); value.Clear(); rows.Add(row); row = new(); } else value.Append(c); } if (quote) throw new InvalidDataException("Unterminated quoted CSV field."); if (value.Length > 0 || row.Count > 0) { row.Add(value.ToString()); rows.Add(row); } return rows; }
    private sealed record FileRead(WorklogOutcome Outcome, List<TimeEntry> Entries);
}
