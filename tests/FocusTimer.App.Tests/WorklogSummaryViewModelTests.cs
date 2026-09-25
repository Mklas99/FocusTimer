namespace FocusTimer.App.Tests;

using FocusTimer.App.ViewModels;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

public class WorklogSummaryViewModelTests
{
    private static readonly DateTimeOffset Start = new(2026, 5, 4, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RefreshAsync_WithRows_IsReadyWithTotalAndRows()
    {
        var service = new FakeSummaryService(request => Success(request, ("Code", 90, 2), ("Chrome", 30, 1)));
        var vm = Create(service);

        await vm.RefreshAsync();

        Assert.Equal(SummaryViewStatus.Ready, vm.Status);
        Assert.True(vm.IsReady);
        Assert.Equal("2h 00m", vm.TotalText);
        Assert.Equal(["Code", "Chrome"], vm.Rows.Select(r => r.Label));
        Assert.Equal("75%", vm.Rows[0].ShareText);
        Assert.Equal("2 entries", vm.Rows[0].EntryCountText);
        Assert.Equal("1 entry", vm.Rows[1].EntryCountText);
    }

    [Fact]
    public async Task RefreshAsync_UsesFirstGroupingAndTheTodayRange()
    {
        var service = new FakeSummaryService(request => Success(request));
        var vm = Create(service);

        await vm.RefreshAsync();

        var request = Assert.Single(service.Requests);
        Assert.Equal("app", request.GroupingId);
        Assert.Equal(Start, request.Range.StartInclusive);
        Assert.Equal(Start.AddDays(1), request.Range.EndExclusive);
        Assert.Equal(SummaryFilter.None, request.Filter);
    }

    [Fact]
    public async Task RefreshAsync_NoRows_IsNoDataWithMessage()
    {
        var vm = Create(new FakeSummaryService(request => Success(request)));

        await vm.RefreshAsync();

        Assert.Equal(SummaryViewStatus.NoData, vm.Status);
        Assert.True(vm.IsMessageVisible);
        Assert.Equal("No time tracked today.", vm.StatusMessage);
        Assert.Empty(vm.Rows);
        Assert.Equal(string.Empty, vm.TotalText);
    }

    [Fact]
    public async Task RefreshAsync_FailedRead_IsErrorWithoutTotal()
    {
        var vm = Create(new FakeSummaryService(request => new WorklogSummary(
            request,
            new WorklogOutcome(WorklogOutcomeKind.MalformedData, "bad file"),
            [],
            TimeSpan.Zero,
            [])));

        await vm.RefreshAsync();

        Assert.Equal(SummaryViewStatus.Error, vm.Status);
        Assert.Equal("bad file", vm.StatusMessage);
        Assert.Equal(string.Empty, vm.TotalText);
        Assert.Empty(vm.Rows);
    }

    [Fact]
    public async Task RefreshAsync_ServiceThrows_IsError()
    {
        var vm = Create(new FakeSummaryService(_ => throw new InvalidOperationException("boom")));

        await vm.RefreshAsync();

        Assert.Equal(SummaryViewStatus.Error, vm.Status);
        Assert.False(string.IsNullOrEmpty(vm.StatusMessage));
    }

    [Fact]
    public async Task RefreshAsync_WithWarnings_KeepsRowsAndShowsNote()
    {
        var warnings = new[] { new WorklogWarning("f.csv", 3, "skipped") };
        var vm = Create(new FakeSummaryService(request => Success(request, ("Code", 30, 1)) with { Warnings = warnings }));

        await vm.RefreshAsync();

        Assert.Equal(SummaryViewStatus.Ready, vm.Status);
        Assert.True(vm.HasWarning);
        Assert.Contains("1 worklog record", vm.WarningText);
    }

    [Fact]
    public async Task SelectedGrouping_Changed_ReloadsSameDayWithNewGrouping()
    {
        var service = new FakeSummaryService(request => Success(request, ("X", 60, 1)));
        var vm = Create(service);
        await vm.RefreshAsync();
        var total = vm.TotalText;

        vm.SelectedGrouping = vm.Groupings.Single(g => g.Id == "project");
        await vm.PendingRefresh;

        Assert.Equal(["app", "project"], service.Requests.Select(r => r.GroupingId));
        Assert.Equal(service.Requests[0].Range, service.Requests[1].Range);
        Assert.Equal(total, vm.TotalText);
    }

    [Fact]
    public async Task RefreshAsync_NewerRefreshSupersedesOlderResult()
    {
        var first = new TaskCompletionSource<WorklogSummary>();
        var service = new FakeSummaryService(request => Success(request, ("New", 60, 1)));
        service.NextOverride = first.Task;
        var vm = Create(service);

        var older = vm.RefreshAsync();
        var newer = vm.RefreshAsync();
        await newer;
        first.SetResult(Success(service.Requests[0], ("Old", 10, 1)));
        await older;

        Assert.Equal(["New"], vm.Rows.Select(r => r.Label));
    }

    [Fact]
    public void Groupings_ComeFromTheRegistry()
    {
        var vm = Create(new FakeSummaryService(request => Success(request)));

        Assert.Equal(["By application", "By project"], vm.Groupings.Select(g => g.DisplayName));
    }

    [Theory]
    [InlineData(0, "0h 00m")]
    [InlineData(20, "<1m")]
    [InlineData(60, "0h 01m")]
    [InlineData(3599, "0h 59m")]
    [InlineData(43500, "12h 05m")]
    public void Duration_FormatsLikeTheTrayText(int seconds, string expected)
    {
        Assert.Equal(expected, SummaryFormatting.Duration(TimeSpan.FromSeconds(seconds)));
    }

    [Theory]
    [InlineData(0.0, "0%")]
    [InlineData(0.004, "<1%")]
    [InlineData(0.5, "50%")]
    [InlineData(1.0, "100%")]
    public void Percent_RoundsToWholeNumbers(double share, string expected)
    {
        Assert.Equal(expected, SummaryFormatting.Percent(share));
    }

    [Fact]
    public async Task UnassignedRow_IsFlagged()
    {
        var vm = Create(new FakeSummaryService(request => new WorklogSummary(
            request,
            WorklogOutcome.Success(),
            [new SummaryRow("none:", "Unassigned", TimeSpan.FromMinutes(5), 1, 1, true)],
            TimeSpan.FromMinutes(5),
            [])));

        await vm.RefreshAsync();

        Assert.True(vm.Rows[0].IsUnassigned);
    }

    private static WorklogSummaryViewModel Create(FakeSummaryService service) => new(
        service,
        new WorklogGroupingRegistry([new ApplicationGrouping(), new ProjectGrouping()]),
        new FixedTimeProvider(Start.AddHours(10)));

    private static WorklogSummary Success(WorklogSummaryRequest request, params (string Label, int Minutes, int Count)[] rows)
    {
        var total = TimeSpan.FromMinutes(rows.Sum(r => r.Minutes));
        var mapped = rows
            .Select(r => new SummaryRow(r.Label, r.Label, TimeSpan.FromMinutes(r.Minutes), total > TimeSpan.Zero ? (double)r.Minutes / rows.Sum(x => x.Minutes) : 0, r.Count, false))
            .ToList();
        return new WorklogSummary(request, WorklogOutcome.Success(), mapped, total, []);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class FakeSummaryService(Func<WorklogSummaryRequest, WorklogSummary> respond) : IWorklogSummaryService
    {
        public List<WorklogSummaryRequest> Requests { get; } = [];

        public Task<WorklogSummary>? NextOverride { get; set; }

        public Task<WorklogSummary> SummarizeAsync(WorklogSummaryRequest request, CancellationToken cancellationToken = default)
        {
            this.Requests.Add(request);
            if (this.NextOverride is not null)
            {
                var pending = this.NextOverride;
                this.NextOverride = null;
                return pending;
            }

            return Task.FromResult(respond(request));
        }
    }
}
