namespace FocusTimer.App.Tests;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

internal sealed class MutableClock : TimeProvider
{
    private DateTimeOffset _utcNow;

    public MutableClock(DateTimeOffset utcNow, TimeZoneInfo? timeZone = null)
    {
        this._utcNow = utcNow;
        this.LocalTimeZone = timeZone ?? TimeZoneInfo.Utc;
    }

    public override TimeZoneInfo LocalTimeZone { get; }

    public void Advance(TimeSpan by) => this._utcNow += by;

    public override DateTimeOffset GetUtcNow() => this._utcNow;
}

internal sealed class MemoryWorklogStore : IWorklogStore
{
    private readonly List<TimeEntry> _entries = [];

    public List<WorklogQuery> Queries { get; } = [];

    public int QueryCount => this.Queries.Count;

    public WorklogOutcome? NextQueryOutcome { get; set; }

    public Task<WorklogReadResult>? NextQueryOverride { get; set; }

    public int PatchCalls { get; private set; }

    public int DeleteCalls { get; private set; }

    public IReadOnlyList<TimeEntry> Entries => this._entries;

    public WorklogOutcome? NextWriteOutcome { get; set; }

    public Action? BeforeWrite { get; set; }

    public void Add(params TimeEntry[] entries) => this._entries.AddRange(entries);

    public void Clear() => this._entries.Clear();

    public void ReplaceFirst(TimeEntry entry) => this._entries[0] = entry;

    public Task<WorklogOutcome> AppendAsync(IReadOnlyCollection<TimeEntry> entries, CancellationToken cancellationToken = default)
    {
        this.BeforeWrite?.Invoke();
        if (this.NextWriteOutcome is { } forced)
        {
            return Task.FromResult(forced);
        }

        this._entries.AddRange(entries);
        return Task.FromResult(WorklogOutcome.Success());
    }

    public Task<WorklogReadResult> GetAsync(string entryId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new WorklogReadResult(WorklogOutcome.Success(), this._entries.Where(e => e.EntryId == entryId).ToList()));

    public Task<WorklogReadResult> QueryAsync(WorklogQuery query, CancellationToken cancellationToken = default)
    {
        this.Queries.Add(query);
        if (this.NextQueryOverride is { } pending)
        {
            this.NextQueryOverride = null;
            return pending;
        }

        if (this.NextQueryOutcome is { } outcome)
        {
            return Task.FromResult(new WorklogReadResult(outcome, []));
        }

        var found = this._entries
            .Where(e => e.StartedAt < query.EndExclusive && e.EndedAt > query.StartInclusive)
            .ToList();
        return Task.FromResult(new WorklogReadResult(WorklogOutcome.Success(), found));
    }

    public Task<WorklogOutcome> PatchAsync(string entryId, int expectedRevision, WorklogPatch patch, CancellationToken cancellationToken = default)
    {
        this.PatchCalls++;
        this.BeforeWrite?.Invoke();
        if (this.NextWriteOutcome is { } forced)
        {
            return Task.FromResult(forced);
        }

        var index = this._entries.FindIndex(e => e.EntryId == entryId);
        if (index < 0)
        {
            return Task.FromResult(new WorklogOutcome(WorklogOutcomeKind.NotFound));
        }

        if (this._entries[index].Revision != expectedRevision)
        {
            return Task.FromResult(new WorklogOutcome(WorklogOutcomeKind.Conflict, "Revision does not match."));
        }

        this._entries[index] = this._entries[index] with
        {
            StartedAt = patch.StartedAt,
            EndedAt = patch.EndedAt,
            WindowTitle = patch.WindowTitle,
            ProjectTag = patch.ProjectTag,
            ProjectAssignmentSource = patch.ProjectAssignmentSource,
            ProjectRuleId = patch.ProjectRuleId,
            Revision = expectedRevision + 1,
        };
        return Task.FromResult(WorklogOutcome.Success());
    }

    public Task<WorklogOutcome> DeleteAsync(string entryId, int expectedRevision, CancellationToken cancellationToken = default)
    {
        this.DeleteCalls++;
        this.BeforeWrite?.Invoke();
        if (this.NextWriteOutcome is { } forced)
        {
            return Task.FromResult(forced);
        }

        var index = this._entries.FindIndex(e => e.EntryId == entryId);
        if (index < 0)
        {
            return Task.FromResult(new WorklogOutcome(WorklogOutcomeKind.NotFound));
        }

        if (this._entries[index].Revision != expectedRevision)
        {
            return Task.FromResult(new WorklogOutcome(WorklogOutcomeKind.Conflict, "Revision does not match."));
        }

        this._entries.RemoveAt(index);
        return Task.FromResult(WorklogOutcome.Success());
    }
}

internal sealed class SettingsStub : ISettingsProvider
{
    private readonly Settings _settings;

    public SettingsStub(Settings? settings = null) => this._settings = settings ?? new Settings();

    public Task<Settings> LoadAsync() => Task.FromResult(this._settings);

    public Task SaveAsync(Settings settings) => Task.CompletedTask;
}

internal sealed class RecordingSummaryService : IWorklogSummaryService
{
    public List<WorklogSummaryRequest> Requests { get; } = [];

    public Task<WorklogSummary> SummarizeAsync(WorklogSummaryRequest request, CancellationToken cancellationToken = default)
    {
        this.Requests.Add(request);
        return Task.FromResult(new WorklogSummary(request, WorklogOutcome.Success(), [], TimeSpan.Zero, []));
    }
}

internal static class WorklogTestData
{
    public static TimeEntry Tracked(string id, DateTimeOffset start, int minutes, string app = "Code", string window = "Window") => new(
        id, "session-" + id, start, start.AddMinutes(minutes), app, window, null, ProjectAssignmentSource.Unassigned, null,
        ActivityKind.Active, EndReason.ApplicationChange, CaptureSource.ActiveWindow, SourcePlatform.Windows, "device-1", 1,
        new DateTimeOffset(2026, 5, 4, 12, 0, 0, TimeSpan.Zero));
}
