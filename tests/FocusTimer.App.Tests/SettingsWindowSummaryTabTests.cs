namespace FocusTimer.App.Tests;

using Avalonia.Controls;
using System.Reactive;
using System.Reactive.Threading.Tasks;
using FocusTimer.App.Services;
using FocusTimer.App.ViewModels;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;
using ReactiveUI;

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

    [Fact]
    public void WidgetAppearancePreview_CancelRestoresLastAppliedTheme()
    {
        var themeManager = new ThemeManager();
        var savedSettings = new Settings
        {
            Theme = new Theme
            {
                ThemeName = "Dark",
                BackgroundOpacity = 0.8,
                WidgetBlurMode = WidgetBlurModes.Strong,
            },
        };
        var vm = Create(new CountingSummaryService(), themeManager, savedSettings);

        vm.BackgroundOpacityPercent = 25;
        vm.SelectedBlurMode = WidgetBlurModes.Soft;

        Assert.Equal(0.25, themeManager.ActiveTheme!.BackgroundOpacity);
        Assert.Equal(WidgetBlurModes.Soft, themeManager.ActiveTheme.WidgetBlurMode);

        vm.RestoreAppearancePreview();

        Assert.Equal(0.8, themeManager.ActiveTheme!.BackgroundOpacity);
        Assert.Equal(WidgetBlurModes.Strong, themeManager.ActiveTheme.WidgetBlurMode);
        vm.Dispose();
    }

    [Fact]
    public async Task WidgetAppearancePreview_ApplyAdvancesRestorePoint()
    {
        var themeManager = new ThemeManager();
        var vm = Create(new CountingSummaryService(), themeManager, new Settings());

        vm.SelectedBlurMode = WidgetBlurModes.Soft;
        await ((ReactiveCommand<Unit, Unit>)vm.ApplyCommand).Execute().ToTask();
        Assert.True(vm.LastApplySucceeded);

        vm.SelectedBlurMode = WidgetBlurModes.Off;
        vm.RestoreAppearancePreview();

        Assert.Equal(WidgetBlurModes.Soft, themeManager.ActiveTheme!.WidgetBlurMode);
        vm.Dispose();
    }

    [Fact]
    public void FirstRunSettings_UsesTheSameDarkPresetAsTheWidget()
    {
        var vm = Create(new CountingSummaryService(), new ThemeManager(), new Settings());

        Assert.Equal("Dark", vm.Settings.Theme.ThemeName);
        Assert.Equal(0.8, vm.Settings.Theme.BackgroundOpacity);
        Assert.Equal(WidgetBlurModes.Strong, vm.SelectedBlurMode);
        vm.Dispose();
    }

    [Fact]
    public void TransparencyDiagnostics_ReportsAchievedLevelAndSolidFallback()
    {
        var manager = new ThemeManager();
        var vm = Create(new CountingSummaryService(), manager, new Settings());

        manager.ReportActualWidgetTransparency(WindowTransparencyLevel.AcrylicBlur);
        Assert.Contains("Requested: Strong | Active: AcrylicBlur | Surface: platform blur", vm.TransparencyDiagnostics);

        manager.ReportActualWidgetTransparency(WindowTransparencyLevel.Blur);
        Assert.Contains("Requested: Strong | Active: Blur | Surface: platform blur", vm.TransparencyDiagnostics);

        manager.ReportActualWidgetTransparency(WindowTransparencyLevel.Transparent);
        Assert.Contains("Requested: Strong | Active: Transparent (blur unavailable) | Surface: transparent backdrop", vm.TransparencyDiagnostics);

        vm.SelectedBlurMode = WidgetBlurModes.Off;
        Assert.Contains("Requested: Off | Active: Transparent | Surface: transparent backdrop", vm.TransparencyDiagnostics);

        manager.ReportActualWidgetTransparency(WindowTransparencyLevel.None);
        Assert.Contains("Requested: Off | Active: None | Surface: solid fallback", vm.TransparencyDiagnostics);
        vm.Dispose();
    }

    private static SettingsWindowViewModel Create(
        CountingSummaryService summary,
        ThemeManager? themeManager = null,
        Settings? settings = null) => new(
        new StubSettingsProvider(settings),
        new StubAutoStartService(),
        new ThemeService(),
        themeManager ?? new ThemeManager(),
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

    private sealed class StubSettingsProvider(Settings? settings) : ISettingsProvider
    {
        public Task<Settings> LoadAsync() => Task.FromResult(settings ?? new Settings());

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
