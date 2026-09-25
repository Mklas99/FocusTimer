namespace FocusTimer.App.Tests;

using FocusTimer.App.Services;
using FocusTimer.App.ViewModels;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

public class SettingsWindowSummaryTabTests
{
    [Fact]
    public void SelectingTheSummaryTab_ReloadsTheBreakdownOnce()
    {
        var summary = new CountingSummaryService();
        var vm = Create(summary);

        vm.SelectedTabIndex = SettingsWindowViewModel.SummaryTabIndex;
        vm.SelectedTabIndex = SettingsWindowViewModel.SummaryTabIndex;

        Assert.Equal(1, summary.Calls);
    }

    [Fact]
    public void SelectingAnotherTab_DoesNotReloadTheBreakdown()
    {
        var summary = new CountingSummaryService();
        var vm = Create(summary);

        vm.SelectedTabIndex = 1;

        Assert.Equal(0, summary.Calls);
    }

    [Fact]
    public void ReturningToTheSummaryTab_ReloadsAgain()
    {
        var summary = new CountingSummaryService();
        var vm = Create(summary);

        vm.SelectedTabIndex = SettingsWindowViewModel.SummaryTabIndex;
        vm.SelectedTabIndex = 0;
        vm.SelectedTabIndex = SettingsWindowViewModel.SummaryTabIndex;

        Assert.Equal(2, summary.Calls);
    }

    private static SettingsWindowViewModel Create(CountingSummaryService summary) => new(
        new StubSettingsProvider(),
        new StubAutoStartService(),
        new ThemeService(),
        new ThemeManager(),
        NullAppLogger.Instance,
        new WorklogSummaryViewModel(
            summary,
            new WorklogGroupingRegistry([new ApplicationGrouping(), new ProjectGrouping()]),
            TimeProvider.System));

    private sealed class CountingSummaryService : IWorklogSummaryService
    {
        public int Calls { get; private set; }

        public Task<WorklogSummary> SummarizeAsync(
            WorklogSummaryRequest request,
            CancellationToken cancellationToken = default)
        {
            this.Calls++;
            return Task.FromResult(new WorklogSummary(request, WorklogOutcome.Success(), [], TimeSpan.Zero, []));
        }
    }

    private sealed class StubSettingsProvider : ISettingsProvider
    {
        public Task<Settings> LoadAsync() => Task.FromResult(new Settings());

        public Task SaveAsync(Settings settings) => Task.CompletedTask;
    }

    private sealed class StubAutoStartService : IAutoStartService
    {
        public void SetAutoStart(bool enabled)
        {
        }

        public bool IsAutoStartEnabled() => false;
    }

    private sealed class NullAppLogger : IAppLogger
    {
        public static readonly NullAppLogger Instance = new();

        public void LogCritical(string message, Exception? ex = null)
        {
        }

        public void LogDebug(string message)
        {
        }

        public void LogError(string message, Exception? ex = null)
        {
        }

        public void LogInformation(string message)
        {
        }

        public void LogWarning(string message)
        {
        }
    }
}
