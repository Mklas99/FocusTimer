namespace FocusTimer.Core.Tests;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

public class TodayStatsServiceTests
{
    [Fact]
    public async Task RefreshTodayAsync_OnDstTransition_QueriesExactLocalDayBoundaries()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Central European Standard Time" : "Europe/Vienna");
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 3, 29, 12, 0, 0, TimeSpan.Zero), timeZone);
        var store = new CapturingWorklogStore();
        var service = new TodayStatsService(store, NullLogger.Instance, clock);

        await service.RefreshTodayAsync();

        var query = Assert.IsType<WorklogQuery>(store.LastQuery);
        Assert.Equal(new DateTimeOffset(2026, 3, 29, 0, 0, 0, TimeSpan.FromHours(1)), query.StartInclusive);
        Assert.Equal(new DateTimeOffset(2026, 3, 30, 0, 0, 0, TimeSpan.FromHours(2)), query.EndExclusive);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;
        private readonly TimeZoneInfo _timeZone;

        public FixedTimeProvider(DateTimeOffset utcNow, TimeZoneInfo timeZone)
        {
            this._utcNow = utcNow;
            this._timeZone = timeZone;
        }

        public override TimeZoneInfo LocalTimeZone => this._timeZone;

        public override DateTimeOffset GetUtcNow() => this._utcNow;
    }

    private sealed class CapturingWorklogStore : IWorklogStore
    {
        public WorklogQuery? LastQuery { get; private set; }

        public Task<WorklogOutcome> AppendAsync(IReadOnlyCollection<TimeEntry> entries,
            CancellationToken cancellationToken = default) => Task.FromResult(WorklogOutcome.Success());

        public Task<WorklogReadResult> GetAsync(string entryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorklogReadResult(WorklogOutcome.Success(), []));

        public Task<WorklogReadResult> QueryAsync(WorklogQuery query, CancellationToken cancellationToken = default)
        {
            this.LastQuery = query;
            return Task.FromResult(new WorklogReadResult(WorklogOutcome.Success(), []));
        }

        public Task<WorklogOutcome> PatchAsync(string entryId, int expectedRevision, WorklogPatch patch,
            CancellationToken cancellationToken = default) => Task.FromResult(WorklogOutcome.Success());

        public Task<WorklogOutcome> DeleteAsync(string entryId, int expectedRevision,
            CancellationToken cancellationToken = default) => Task.FromResult(WorklogOutcome.Success());
    }
}
