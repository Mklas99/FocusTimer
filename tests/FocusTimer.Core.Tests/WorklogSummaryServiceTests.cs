namespace FocusTimer.Core.Tests;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

public class WorklogSummaryServiceTests
{
    private static readonly DateTimeOffset DayStart = new(2026, 5, 4, 0, 0, 0, TimeSpan.Zero);
    private static readonly SummaryRange Day = new(DayStart, DayStart.AddDays(1));

    [Fact]
    public void Today_OnNormalDay_SpansLocalMidnightToMidnight()
    {
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 5, 4, 10, 0, 0, TimeSpan.Zero), TimeZoneInfo.Utc);

        var range = SummaryRanges.Today(clock);

        Assert.Equal(DayStart, range.StartInclusive);
        Assert.Equal(DayStart.AddDays(1), range.EndExclusive);
    }

    [Fact]
    public void Today_OnDstChangeDay_UsesOffsetsOfEachBoundary()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Central European Standard Time" : "Europe/Vienna");
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 3, 29, 12, 0, 0, TimeSpan.Zero), timeZone);

        var range = SummaryRanges.Today(clock);

        Assert.Equal(new DateTimeOffset(2026, 3, 29, 0, 0, 0, TimeSpan.FromHours(1)), range.StartInclusive);
        Assert.Equal(new DateTimeOffset(2026, 3, 30, 0, 0, 0, TimeSpan.FromHours(2)), range.EndExclusive);
    }

    [Fact]
    public void Registry_KeepsOrderAndFindsByIdIgnoringCase()
    {
        var registry = new WorklogGroupingRegistry([new ApplicationGrouping(), new ProjectGrouping()]);

        Assert.Equal(["app", "project"], registry.All.Select(g => g.Id));
        Assert.True(registry.TryGet("PROJECT", out var found));
        Assert.Equal("project", found.Id);
        Assert.False(registry.TryGet("missing", out _));
    }

    [Fact]
    public void Registry_DuplicateId_IsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new WorklogGroupingRegistry([new ApplicationGrouping(), new FakeGrouping("APP")]));
    }

    [Theory]
    [InlineData("Alpha", "Alpha")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void StoredProjectResolver_ReturnsStoredTag(string? tag, string? expected)
    {
        Assert.Equal(expected, new StoredProjectResolver().Resolve(Entry("a", 0, 60, project: tag)));
    }

    [Fact]
    public void ApplicationGrouping_MergesNamesThatDifferOnlyByCase()
    {
        var grouping = new ApplicationGrouping();
        var context = new GroupingContext(null);

        var first = grouping.Select(Entry("Code", 0, 10), context);
        var second = grouping.Select(Entry("code", 10, 20), context);

        Assert.Equal(first.Key, second.Key);
    }

    [Fact]
    public void ProjectGrouping_TrimsAndIgnoresCase_AndKeepsUnassignedApart()
    {
        var grouping = new ProjectGrouping();
        var a = grouping.Select(Entry("x", 0, 1), new GroupingContext("  Alpha "));
        var b = grouping.Select(Entry("x", 0, 1), new GroupingContext("alpha"));
        var none = grouping.Select(Entry("x", 0, 1), new GroupingContext(null));
        var blank = grouping.Select(Entry("x", 0, 1), new GroupingContext("  "));
        var named = grouping.Select(Entry("x", 0, 1), new GroupingContext("Unassigned"));

        Assert.Equal(a.Key, b.Key);
        Assert.Equal(none.Key, blank.Key);
        Assert.True(none.IsUnassigned);
        Assert.False(named.IsUnassigned);
        Assert.NotEqual(none.Key, named.Key);
    }

    [Fact]
    public async Task Summarize_ByApplication_GroupsSumsAndSortsLongestFirst()
    {
        var service = CreateService(new FakeStore(
            Entry("Chrome", 0, 30),
            Entry("Code", 30, 150),
            Entry("chrome", 150, 180)));

        var summary = await service.SummarizeAsync(new WorklogSummaryRequest(Day, "app"));

        Assert.True(summary.IsSuccess);
        Assert.Equal(["Code", "Chrome"], summary.Rows.Select(r => r.Label));
        Assert.Equal(TimeSpan.FromMinutes(120), summary.Rows[0].Duration);
        Assert.Equal(TimeSpan.FromMinutes(60), summary.Rows[1].Duration);
        Assert.Equal(2, summary.Rows[1].EntryCount);
        Assert.Equal(TimeSpan.FromMinutes(180), summary.Total);
    }

    [Fact]
    public async Task Summarize_SharesSumToOne()
    {
        var service = CreateService(new FakeStore(Entry("a", 0, 10), Entry("b", 10, 40), Entry("c", 40, 47)));

        var summary = await service.SummarizeAsync(new WorklogSummaryRequest(Day, "app"));

        Assert.Equal(1.0, summary.Rows.Sum(r => r.Share), 6);
        Assert.Equal(30.0 / 47.0, summary.Rows[0].Share, 6);
    }

    [Fact]
    public async Task Summarize_NoEntries_IsSuccessfulAndEmpty()
    {
        var service = CreateService(new FakeStore());

        var summary = await service.SummarizeAsync(new WorklogSummaryRequest(Day, "app"));

        Assert.True(summary.IsSuccess);
        Assert.Empty(summary.Rows);
        Assert.Equal(TimeSpan.Zero, summary.Total);
    }

    [Fact]
    public async Task Summarize_ByProject_PutsUntaggedTimeInOneUnassignedRow()
    {
        var service = CreateService(new FakeStore(
            Entry("a", 0, 30, project: "Alpha"),
            Entry("a", 30, 50, project: null),
            Entry("a", 50, 60, project: " "),
            Entry("a", 60, 70, project: "Unassigned")));

        var summary = await service.SummarizeAsync(new WorklogSummaryRequest(Day, "project"));

        Assert.Equal(3, summary.Rows.Count);
        var unassigned = Assert.Single(summary.Rows, r => r.IsUnassigned);
        Assert.Equal(TimeSpan.FromMinutes(30), unassigned.Duration);
        Assert.Equal(TimeSpan.FromMinutes(70), summary.Total);
        Assert.Single(summary.Rows, r => !r.IsUnassigned && r.Label == "Unassigned");
    }

    [Fact]
    public async Task Summarize_ByApplicationAndByProject_HaveTheSameTotal()
    {
        var service = CreateService(new FakeStore(Entry("a", 0, 30, project: "P"), Entry("b", 30, 50)));

        var byApp = await service.SummarizeAsync(new WorklogSummaryRequest(Day, "app"));
        var byProject = await service.SummarizeAsync(new WorklogSummaryRequest(Day, "project"));

        Assert.Equal(byApp.Total, byProject.Total);
    }

    [Fact]
    public async Task Summarize_ApplicationFilter_IsPassedToTheStoreAndOthersAreExcluded()
    {
        var store = new FakeStore(Entry("Code", 0, 30), Entry("Chrome", 30, 50));
        var service = CreateService(store);

        var summary = await service.SummarizeAsync(
            new WorklogSummaryRequest(Day, "app", new SummaryFilter(Application: "code")));

        Assert.Equal("code", store.LastQuery!.Application);
        Assert.Equal(TimeSpan.FromMinutes(30), summary.Total);
        Assert.Equal(Day.StartInclusive, store.LastQuery.StartInclusive);
        Assert.Equal(Day.EndExclusive, store.LastQuery.EndExclusive);
    }

    [Fact]
    public async Task Summarize_ProjectFilter_UsesResolvedProject()
    {
        var service = new WorklogSummaryService(
            new FakeStore(Entry("a", 0, 30, project: "stored"), Entry("b", 30, 50, project: "stored")),
            DefaultRegistry(),
            new FakeResolver(e => e.AppName == "a" ? "Alpha" : null),
            NullLogger.Instance);

        var summary = await service.SummarizeAsync(
            new WorklogSummaryRequest(Day, "app", new SummaryFilter(Project: " alpha ")));

        Assert.Equal(TimeSpan.FromMinutes(30), summary.Total);
    }

    [Fact]
    public async Task Summarize_FilterThatMatchesNothing_IsSuccessfulAndEmpty()
    {
        var service = CreateService(new FakeStore(Entry("a", 0, 30, project: "P")));

        var summary = await service.SummarizeAsync(
            new WorklogSummaryRequest(Day, "app", new SummaryFilter(Project: "other")));

        Assert.True(summary.IsSuccess);
        Assert.Empty(summary.Rows);
    }

    [Fact]
    public async Task Summarize_BlankFilters_BehaveLikeNoFilter()
    {
        var store = new FakeStore(Entry("a", 0, 30));
        var service = CreateService(store);

        var summary = await service.SummarizeAsync(
            new WorklogSummaryRequest(Day, "app", new SummaryFilter(" ", "")));

        Assert.Null(store.LastQuery!.Application);
        Assert.Equal(TimeSpan.FromMinutes(30), summary.Total);
    }

    [Fact]
    public async Task Summarize_EntryCrossingRangeBoundaries_CountsOnlyTheOverlap()
    {
        var service = CreateService(new FakeStore(
            new[] { Entry("a", -30, 30), Entry("a", 1420, 1500) }.ToArray()));

        var summary = await service.SummarizeAsync(new WorklogSummaryRequest(Day, "app"));

        Assert.Equal(TimeSpan.FromMinutes(30 + 20), summary.Total);
    }

    [Fact]
    public async Task Summarize_UnreadableWorklog_ReportsFailureNotZero()
    {
        var store = new FakeStore { Outcome = new WorklogOutcome(WorklogOutcomeKind.MalformedData, "bad file") };
        var service = CreateService(store);

        var summary = await service.SummarizeAsync(new WorklogSummaryRequest(Day, "app"));

        Assert.False(summary.IsSuccess);
        Assert.Equal(WorklogOutcomeKind.MalformedData, summary.Outcome.Kind);
        Assert.Equal("bad file", summary.Outcome.Message);
        Assert.Empty(summary.Rows);
    }

    [Fact]
    public async Task Summarize_UnsupportedSchema_ReportsFailure()
    {
        var store = new FakeStore { Outcome = new WorklogOutcome(WorklogOutcomeKind.UnsupportedSchema, "old") };

        var summary = await CreateService(store).SummarizeAsync(new WorklogSummaryRequest(Day, "app"));

        Assert.Equal(WorklogOutcomeKind.UnsupportedSchema, summary.Outcome.Kind);
    }

    [Fact]
    public async Task Summarize_PartialRead_KeepsEntriesAndExposesWarnings()
    {
        var warning = new WorklogWarning("file.csv", 4, "skipped record");
        var store = new FakeStore(Entry("a", 0, 30)) { Outcome = WorklogOutcome.Success([warning]) };

        var summary = await CreateService(store).SummarizeAsync(new WorklogSummaryRequest(Day, "app"));

        Assert.True(summary.IsSuccess);
        Assert.Equal(TimeSpan.FromMinutes(30), summary.Total);
        Assert.Equal(warning, Assert.Single(summary.Warnings));
    }

    [Fact]
    public async Task Summarize_StoreThrows_ReportsIoFailure()
    {
        var store = new FakeStore { Throw = new IOException("disk") };

        var summary = await CreateService(store).SummarizeAsync(new WorklogSummaryRequest(Day, "app"));

        Assert.Equal(WorklogOutcomeKind.IoFailure, summary.Outcome.Kind);
    }

    [Fact]
    public async Task Summarize_UnknownGroupingOrInvalidRange_IsValidationFailure()
    {
        var service = CreateService(new FakeStore());

        var unknown = await service.SummarizeAsync(new WorklogSummaryRequest(Day, "nope"));
        var invalid = await service.SummarizeAsync(
            new WorklogSummaryRequest(new SummaryRange(DayStart, DayStart), "app"));

        Assert.Equal(WorklogOutcomeKind.ValidationFailure, unknown.Outcome.Kind);
        Assert.Equal(WorklogOutcomeKind.ValidationFailure, invalid.Outcome.Kind);
    }

    [Fact]
    public async Task Summarize_TodayTotal_EqualsTodayStatsTotal()
    {
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 5, 4, 12, 0, 0, TimeSpan.Zero), TimeZoneInfo.Utc);
        var store = new FakeStore(Entry("a", 60, 100), Entry("b", 200, 215), Entry("a", 900, 1000));
        var stats = new TodayStatsService(store, NullLogger.Instance, clock);
        await stats.RefreshTodayAsync();

        var summary = await CreateService(store).SummarizeAsync(
            new WorklogSummaryRequest(SummaryRanges.Today(clock), "app"));

        Assert.Equal(stats.GetTodayTotal(), summary.Total);
    }

    [Fact]
    public async Task Summarize_NewGroupingAndResolver_WorkWithoutChangingTheService()
    {
        var byDay = new FakeGrouping("day", e => new GroupKey(e.StartedAt.ToString("yyyy-MM-dd"), e.StartedAt.ToString("dd MMM")));
        var registry = new WorklogGroupingRegistry([new ApplicationGrouping(), new ProjectGrouping(), byDay]);
        var service = new WorklogSummaryService(
            new FakeStore(Entry("a", 10, 20, project: "x"), Entry("a", 30, 60, project: "x")),
            registry,
            new FakeResolver(_ => "Resolved"),
            NullLogger.Instance);

        var byDayResult = await service.SummarizeAsync(new WorklogSummaryRequest(Day, "day"));
        var byProject = await service.SummarizeAsync(new WorklogSummaryRequest(Day, "project"));

        Assert.Single(byDayResult.Rows);
        Assert.Equal(TimeSpan.FromMinutes(40), byDayResult.Total);
        Assert.Equal("Resolved", Assert.Single(byProject.Rows).Label);
    }

    private static WorklogSummaryService CreateService(FakeStore store) =>
        new(store, DefaultRegistry(), new StoredProjectResolver(), NullLogger.Instance);

    private static WorklogGroupingRegistry DefaultRegistry() =>
        new([new ApplicationGrouping(), new ProjectGrouping()]);

    private static TimeEntry Entry(string app, int startMinute, int endMinute, string? project = null) => new(
        Guid.NewGuid().ToString(),
        "session",
        DayStart.AddMinutes(startMinute),
        DayStart.AddMinutes(endMinute),
        app,
        "title",
        project,
        ProjectAssignmentSource.Unassigned,
        null,
        ActivityKind.Active,
        EndReason.ApplicationChange,
        CaptureSource.ActiveWindow,
        SourcePlatform.Windows,
        null,
        1,
        DayStart);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow, TimeZoneInfo timeZone) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => timeZone;

        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class FakeGrouping(string id, Func<TimeEntry, GroupKey>? select = null) : IWorklogGrouping
    {
        public string Id => id;

        public string DisplayName => id;

        public GroupKey Select(TimeEntry entry, GroupingContext context) =>
            select?.Invoke(entry) ?? new GroupKey(id, id);
    }

    private sealed class FakeResolver(Func<TimeEntry, string?> resolve) : IProjectResolver
    {
        public string? Resolve(TimeEntry entry) => resolve(entry);
    }

    private sealed class FakeStore(params TimeEntry[] entries) : IWorklogStore
    {
        public WorklogQuery? LastQuery { get; private set; }

        public WorklogOutcome Outcome { get; init; } = WorklogOutcome.Success();

        public Exception? Throw { get; init; }

        public Task<WorklogReadResult> QueryAsync(WorklogQuery query, CancellationToken cancellationToken = default)
        {
            this.LastQuery = query;
            if (this.Throw is not null)
            {
                throw this.Throw;
            }

            return Task.FromResult(new WorklogReadResult(
                this.Outcome,
                this.Outcome.IsSuccess
                    ? entries.Where(e => e.EndedAt > query.StartInclusive
                        && e.StartedAt < query.EndExclusive
                        && (query.Application is null
                            || string.Equals(e.AppName, query.Application, StringComparison.OrdinalIgnoreCase))).ToList()
                    : []));
        }

        public Task<WorklogOutcome> AppendAsync(
            IReadOnlyCollection<TimeEntry> items,
            CancellationToken cancellationToken = default) => Task.FromResult(WorklogOutcome.Success());

        public Task<WorklogReadResult> GetAsync(string entryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorklogReadResult(WorklogOutcome.Success(), []));

        public Task<WorklogOutcome> PatchAsync(
            string entryId,
            int expectedRevision,
            WorklogPatch patch,
            CancellationToken cancellationToken = default) => Task.FromResult(WorklogOutcome.Success());

        public Task<WorklogOutcome> DeleteAsync(
            string entryId,
            int expectedRevision,
            CancellationToken cancellationToken = default) => Task.FromResult(WorklogOutcome.Success());
    }
}
