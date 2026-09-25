namespace FocusTimer.Core.Services;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

/// <summary>Summarizes worklog entries using the storage-neutral query contract.</summary>
public sealed class WorklogSummaryService : IWorklogSummaryService
{
    private readonly IWorklogStore _store;
    private readonly WorklogGroupingRegistry _groupings;
    private readonly IProjectResolver _projectResolver;
    private readonly IAppLogger _logger;

    /// <summary>Initializes a new instance of the <see cref="WorklogSummaryService"/> class.</summary>
    /// <param name="store">The worklog to read.</param>
    /// <param name="groupings">The available groupings.</param>
    /// <param name="projectResolver">Decides which project each entry belongs to.</param>
    /// <param name="logger">The application logger.</param>
    public WorklogSummaryService(
        IWorklogStore store,
        WorklogGroupingRegistry groupings,
        IProjectResolver projectResolver,
        IAppLogger logger)
    {
        this._store = store;
        this._groupings = groupings;
        this._projectResolver = projectResolver;
        this._logger = logger;
    }

    /// <inheritdoc/>
    public async Task<WorklogSummary> SummarizeAsync(
        WorklogSummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.Range.IsValid)
        {
            return Failed(request, WorklogOutcomeKind.ValidationFailure, "The summary range must end after it starts.");
        }

        if (!this._groupings.TryGet(request.GroupingId, out var grouping))
        {
            return Failed(request, WorklogOutcomeKind.ValidationFailure, $"Unknown grouping '{request.GroupingId}'.");
        }

        var filter = request.Filter ?? SummaryFilter.None;
        WorklogReadResult read;
        try
        {
            var query = new WorklogQuery(
                request.Range.StartInclusive,
                request.Range.EndExclusive,
                Application: NullIfBlank(filter.Application));
            read = await this._store.QueryAsync(query, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            this._logger.LogError("Failed to read the worklog for a summary.", ex);
            return Failed(request, WorklogOutcomeKind.IoFailure, "The worklog could not be read.");
        }

        var warnings = read.Outcome.Warnings ?? [];
        if (!read.Outcome.IsSuccess)
        {
            return new WorklogSummary(request, read.Outcome, [], TimeSpan.Zero, warnings);
        }

        var groups = new Dictionary<string, Accumulator>(StringComparer.Ordinal);
        var projectFilter = NullIfBlank(filter.Project);
        foreach (var entry in read.Entries)
        {
            var project = this._projectResolver.Resolve(entry);
            if (projectFilter is not null
                && !string.Equals(project?.Trim(), projectFilter.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var counted = Clip(entry, request.Range);
            if (counted <= TimeSpan.Zero)
            {
                continue;
            }

            var key = grouping.Select(entry, new GroupingContext(project));
            if (!groups.TryGetValue(key.Key, out var group))
            {
                group = new Accumulator(key);
                groups[key.Key] = group;
            }

            group.Duration += counted;
            group.Count++;
        }

        var total = groups.Values.Aggregate(TimeSpan.Zero, (sum, g) => sum + g.Duration);
        var rows = groups.Values
            .OrderByDescending(g => g.Duration)
            .ThenBy(g => g.Key.Label, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SummaryRow(
                g.Key.Key,
                g.Key.Label,
                g.Duration,
                total > TimeSpan.Zero ? g.Duration / total : 0,
                g.Count,
                g.Key.IsUnassigned))
            .ToList();

        return new WorklogSummary(request, read.Outcome, rows, total, warnings);
    }

    private static TimeSpan Clip(TimeEntry entry, SummaryRange range)
    {
        var start = entry.StartedAt > range.StartInclusive ? entry.StartedAt : range.StartInclusive;
        var end = entry.EndedAt < range.EndExclusive ? entry.EndedAt : range.EndExclusive;
        return end - start;
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static WorklogSummary Failed(WorklogSummaryRequest request, WorklogOutcomeKind kind, string message) =>
        new(request, new WorklogOutcome(kind, message), [], TimeSpan.Zero, []);

    private sealed class Accumulator(GroupKey key)
    {
        public GroupKey Key { get; } = key;

        public TimeSpan Duration { get; set; }

        public int Count { get; set; }
    }
}
