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
    private readonly IAtomicWorklogFileOperations _fileOperations;
    private readonly TimeProvider _timeProvider;
    private DateTime _lastRetentionCleanupDate = DateTime.MinValue;

    /// <summary>Initializes the store.</summary>
    public CsvSessionRepository(ISettingsProvider settingsProvider, IAppLogger? logger = null,
        IAtomicWorklogFileOperations? fileOperations = null, TimeProvider? timeProvider = null)
    {
        this._settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
        this._logger = logger;
        this._fileOperations = fileOperations ?? new AtomicWorklogFileOperations();
        this._timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    public async Task<WorklogOutcome> AppendAsync(IReadOnlyCollection<TimeEntry> entries, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (entries is null || entries.Count == 0)
        {
            return WorklogOutcome.Success();
        }

        var errors = entries.SelectMany(WorklogEntryValidator.Validate).ToList();
        if (errors.Count > 0)
        {
            return new(WorklogOutcomeKind.ValidationFailure, string.Join(" ", errors));
        }

        var settings = await this._settingsProvider.LoadAsync();
        var root = ResolveRoot(settings);
        await this.EnforceRetentionPolicyAsync(settings, root, cancellationToken);
        var groups = entries
            .GroupBy(entry => GetPath(root, entry.StartedAt.Date), StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var acquiredLocks = new List<SemaphoreSlim>(groups.Length);
        try
        {
            foreach (var group in groups)
            {
                var gate = Locks.GetOrAdd(group.Key, _ => new SemaphoreSlim(1, 1));
                await gate.WaitAsync(cancellationToken);
                acquiredLocks.Add(gate);
            }

            var plans = new List<AppendPlan>(groups.Length);
            foreach (var group in groups)
            {
                var read = await ReadFileAsync(group.Key, cancellationToken);
                if (read.Outcome.Kind == WorklogOutcomeKind.NotFound)
                {
                    read = new FileRead(WorklogOutcome.Success(), []);
                }

                if (!read.Outcome.IsSuccess)
                {
                    return read.Outcome;
                }

                if (read.Outcome.Warnings?.Count > 0)
                {
                    return new(WorklogOutcomeKind.MalformedData, "Cannot append to a worklog with malformed records.", read.Outcome.Warnings);
                }

                var existing = new Dictionary<string, TimeEntry>(StringComparer.Ordinal);
                foreach (var stored in read.Entries)
                {
                    if (!existing.TryAdd(stored.EntryId, stored))
                    {
                        return new(WorklogOutcomeKind.Conflict, "The worklog contains duplicate entry IDs.");
                    }
                }

                var entriesToAppend = new List<TimeEntry>();
                foreach (var entry in group)
                {
                    if (existing.TryGetValue(entry.EntryId, out var prior))
                    {
                        if (prior != entry)
                        {
                            return new(WorklogOutcomeKind.Conflict, "Entry ID already exists with different content.");
                        }

                        continue;
                    }

                    existing.Add(entry.EntryId, entry);
                    entriesToAppend.Add(entry);
                }

                plans.Add(new AppendPlan(group.Key, read.Entries, entriesToAppend));
            }

            foreach (var plan in plans.Where(plan => plan.EntriesToAppend.Count > 0))
            {
                plan.ExistingEntries.AddRange(plan.EntriesToAppend);
                await this.WriteAtomicallyAsync(plan.Path, plan.ExistingEntries, cancellationToken);
            }

            return WorklogOutcome.Success();
        }
        catch (IOException exception)
        {
            return new(IsFileInUse(exception) ? WorklogOutcomeKind.FileInUse : WorklogOutcomeKind.IoFailure, exception.Message);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new(WorklogOutcomeKind.IoFailure, exception.Message);
        }
        finally
        {
            foreach (var gate in acquiredLocks.AsEnumerable().Reverse())
            {
                gate.Release();
            }
        }
    }

    /// <inheritdoc/>
    public async Task<WorklogReadResult> GetAsync(string entryId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(entryId))
            return new(new(WorklogOutcomeKind.ValidationFailure, "Entry ID is required."), []);
        var root = ResolveRoot(await this._settingsProvider.LoadAsync());
        if (File.Exists(root))
            return new(new(WorklogOutcomeKind.IoFailure, "The configured worklog root is a file."), []);
        if (!Directory.Exists(root))
            return new(WorklogOutcome.Success(), []);

        var matches = new List<TimeEntry>();
        var warnings = new List<WorklogWarning>();
        try
        {
            foreach (var path in Directory.EnumerateFiles(root, "*-worklog.csv", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var read = await ReadFileAsync(path, cancellationToken);
                if (!read.Outcome.IsSuccess)
                    return new(read.Outcome, matches);
                matches.AddRange(read.Entries.Where(entry => entry.EntryId == entryId));
                if (read.Outcome.Warnings is not null)
                    warnings.AddRange(read.Outcome.Warnings);
            }
        }
        catch (IOException exception)
        {
            return new(new(IsFileInUse(exception) ? WorklogOutcomeKind.FileInUse : WorklogOutcomeKind.IoFailure, exception.Message), matches);
        }
        catch (UnauthorizedAccessException exception)
        {
            return new(new(WorklogOutcomeKind.IoFailure, exception.Message), matches);
        }

        return matches.Count > 1
            ? new(new(WorklogOutcomeKind.Conflict, "Entry ID exists in more than one worklog file."), matches)
            : new(WorklogOutcome.Success(warnings), matches);
    }

    /// <inheritdoc/>
    public async Task<WorklogReadResult> QueryAsync(WorklogQuery query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!query.IsValid)
            return new(new(WorklogOutcomeKind.ValidationFailure, "Query end must be later than start."), []);
        var root = ResolveRoot(await this._settingsProvider.LoadAsync());
        if (File.Exists(root))
            return new(new(WorklogOutcomeKind.IoFailure, "The configured worklog root is a file."), []);
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
    public Task<WorklogOutcome> PatchAsync(string entryId, int expectedRevision, WorklogPatch patch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patch);
        return this.MutateAsync(entryId, expectedRevision, old => old with
        {
            StartedAt = patch.StartedAt,
            EndedAt = patch.EndedAt,
            AppName = patch.AppName,
            WindowTitle = patch.WindowTitle,
            ProjectTag = patch.ProjectTag,
            ProjectAssignmentSource = patch.ProjectAssignmentSource,
            ProjectRuleId = patch.ProjectRuleId,
            ActivityKind = patch.ActivityKind,
            EndReason = patch.EndReason,
            CaptureSource = patch.CaptureSource,
            Revision = old.Revision + 1,
            LastModifiedAtUtc = this._timeProvider.GetUtcNow(),
        }, cancellationToken);
    }
    /// <inheritdoc/>
    public Task<WorklogOutcome> DeleteAsync(string entryId, int expectedRevision, CancellationToken cancellationToken = default) => this.MutateAsync(entryId, expectedRevision, _ => null, cancellationToken);

    private async Task<WorklogOutcome> MutateAsync(string entryId, int expectedRevision, Func<TimeEntry, TimeEntry?> mutate, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(entryId) || expectedRevision < 1)
            return new(WorklogOutcomeKind.ValidationFailure, "Entry ID and a positive expected revision are required.");
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
        if (changed is not null && (changed.StartedAt.Date != old.StartedAt.Date ||
            changed.EndedAt.Date != old.EndedAt.Date))
            return new(WorklogOutcomeKind.ValidationFailure, "An entry cannot move to another local day.");
        var path = GetPath(ResolveRoot(await this._settingsProvider.LoadAsync()), old.StartedAt.Date);
        var gate = Locks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            this._fileOperations.CleanupStaleTemporaryFiles(path);
            var read = await ReadFileAsync(path, cancellationToken);
            if (!read.Outcome.IsSuccess)
                return read.Outcome;
            var index = read.Entries.FindIndex(e => e.EntryId == entryId);
            if (index < 0)
                return new(WorklogOutcomeKind.NotFound);
            if (read.Entries[index].Revision != expectedRevision)
                return new(WorklogOutcomeKind.Conflict, "Revision does not match.");
            if (changed is null)
                read.Entries.RemoveAt(index);
            else
                read.Entries[index] = changed;
            await this.WriteAtomicallyAsync(path, read.Entries, cancellationToken);
            return WorklogOutcome.Success();
        }
        catch (IOException ex) { return new(IsFileInUse(ex) ? WorklogOutcomeKind.FileInUse : WorklogOutcomeKind.IoFailure, ex.Message); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return new(WorklogOutcomeKind.IoFailure, ex.Message); }
        finally { gate.Release(); }
    }

    private static string ResolveRoot(Settings settings) => string.IsNullOrWhiteSpace(settings.WorklogDirectory) ? Settings.DefaultWorklogDirectory : settings.WorklogDirectory;

    private async Task EnforceRetentionPolicyAsync(Settings settings, string root, CancellationToken cancellationToken)
    {
        if (settings.DataRetentionDays <= 0 || !Directory.Exists(root))
        {
            return;
        }

        var today = this._timeProvider.GetLocalNow().Date;
        if (this._lastRetentionCleanupDate == today)
        {
            return;
        }

        var cutoff = today.AddDays(-settings.DataRetentionDays);
        foreach (var path in Directory.EnumerateFiles(root, "*-worklog.csv", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fileName = Path.GetFileNameWithoutExtension(path);
            if (fileName.Length < 10 || !DateTime.TryParseExact(fileName[..10], "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) || date >= cutoff)
            {
                continue;
            }

            var gate = Locks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(cancellationToken);
            try
            {
                var read = await ReadFileAsync(path, cancellationToken);
                if (read.Outcome.IsSuccess)
                {
                    File.Delete(path);
                    this._logger?.LogInformation($"Removed expired worklog file: {path}");
                }
                else
                {
                    this._logger?.LogWarning($"Skipped retention cleanup for {path}: {read.Outcome.Kind} {read.Outcome.Message}");
                }
            }
            finally
            {
                gate.Release();
            }
        }

        this._lastRetentionCleanupDate = today;
    }

    private static string GetPath(string root, DateTime date) => Path.Combine(root, date.ToString("yyyy", CultureInfo.InvariantCulture), date.ToString("MM", CultureInfo.InvariantCulture), $"{date:yyyy-MM-dd}-worklog.csv");
    private async Task WriteAtomicallyAsync(string path, List<TimeEntry> entries, CancellationToken ct)
    {
        string? temp = null;
        try
        {
            await using (var stream = this._fileOperations.CreateTemporaryFile(path, out temp))
            {
                await new CsvWorklogCodec().WriteAsync(stream, entries.OrderBy(entry => entry.StartedAt), ct);
                await stream.FlushAsync(ct);
                stream.Flush(flushToDisk: true);
            }

            var validate = await ReadFileAsync(temp, ct);
            if (!validate.Outcome.IsSuccess || validate.Entries.Count != entries.Count ||
                !validate.Entries.SequenceEqual(entries.OrderBy(entry => entry.StartedAt)))
                throw new InvalidDataException("Replacement validation failed.");
            this._fileOperations.ActivateReplacement(temp, path);
            temp = null;
        }
        finally
        {
            if (temp is not null)
                this._fileOperations.DeleteTemporaryFile(temp);
            this._fileOperations.CleanupStaleTemporaryFiles(path);
        }
    }
    private static async Task<FileRead> ReadFileAsync(string path, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!File.Exists(path))
            return new(new(WorklogOutcomeKind.NotFound), []);
        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var read = await new CsvWorklogCodec().ReadAsync(stream, path, ct);
            return new(read.Outcome, read.Entries.ToList());
        }
        catch (IOException ex) { return new(new(IsFileInUse(ex) ? WorklogOutcomeKind.FileInUse : WorklogOutcomeKind.IoFailure, ex.Message), []); }
        catch (UnauthorizedAccessException ex) { return new(new(WorklogOutcomeKind.IoFailure, ex.Message), []); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return new(new(WorklogOutcomeKind.MalformedData, ex.Message), []); }
    }

    private static bool IsFileInUse(IOException exception) => exception.HResult is unchecked((int)0x80070020) or unchecked((int)0x80070021);

    private sealed record FileRead(WorklogOutcome Outcome, List<TimeEntry> Entries);

    private sealed record AppendPlan(string Path, List<TimeEntry> ExistingEntries, List<TimeEntry> EntriesToAppend);
}
