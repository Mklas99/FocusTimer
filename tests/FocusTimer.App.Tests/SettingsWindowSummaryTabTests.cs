namespace FocusTimer.App.Tests;

using Avalonia.Controls;
using System.Reactive;
using System.Reactive.Threading.Tasks;
using FocusTimer.App.Services;
using FocusTimer.App.ViewModels;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;
using FocusTimer.Core.Stubs;
using FocusTimer.Persistence;
using ReactiveUI;

public class SettingsWindowSummaryTabTests
{
    [Fact]
    public async Task DelayedLoad_BlocksEditingAndCommitUntilSettingsArrive()
    {
        var provider = new ControlledSettingsProvider();
        var vm = Create(new CountingSummaryService(), provider: provider);

        Assert.False(vm.IsSettingsLoaded);
        Assert.False(vm.IsDraftVisible);
        await ((ReactiveCommand<Unit, Unit>)vm.ApplyCommand).Execute().ToTask();
        Assert.Equal(0, provider.SaveCalls);

        provider.CompleteLoad(new Settings { BreakIntervalMinutes = 25 });
        await WaitUntilAsync(() => vm.IsSettingsLoaded);
        Assert.Equal(25, vm.Settings.BreakIntervalMinutes);
        vm.Dispose();
    }

    [Fact]
    public async Task FailedLoad_RetryPreservesExistingFileAndEnablesCommitOnlyAfterSuccess()
    {
        var provider = new ControlledSettingsProvider();
        var vm = Create(new CountingSummaryService(), provider: provider);
        int startupContinued = 0;
        vm.SetRecoveryCompleted(() =>
        {
            startupContinued++;
            return Task.CompletedTask;
        });
        provider.FailLoad();
        await WaitUntilAsync(() => vm.HasLoadError);

        Assert.False(vm.IsSettingsLoaded);
        await ((ReactiveCommand<Unit, Unit>)vm.ApplyCommand).Execute().ToTask();
        Assert.Equal(0, provider.SaveCalls);

        provider.NextLoad = new Settings { BreakIntervalMinutes = 35 };
        await ((ReactiveCommand<Unit, Unit>)vm.RetryLoadCommand).Execute().ToTask();
        Assert.True(vm.IsSettingsLoaded);
        Assert.False(vm.HasLoadError);
        Assert.Equal(35, vm.Settings.BreakIntervalMinutes);
        Assert.Equal(1, startupContinued);
        vm.Dispose();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ThemeImportFinishingDuringApply_DoesNotChangeCommittedPreview(bool finishAfterApply)
    {
        var themes = new DelayedImportThemeService();
        var activation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var manager = new ThemeManager();
        var vm = Create(new CountingSummaryService(), themeManager: manager, themeService: themes);
        vm.SetRuntimeActivator(_ => activation.Task);

        Task import = vm.ImportThemeFileAsync("delayed.fttheme");
        Task apply = ((ReactiveCommand<Unit, Unit>)vm.ApplyCommand).Execute().ToTask();
        await WaitUntilAsync(() => vm.IsCommitting);
        if (finishAfterApply)
        {
            activation.SetResult();
            await apply;
        }

        themes.CompleteImport(new Theme { ThemeName = "Imported" });
        await import;
        activation.TrySetResult();
        await apply;

        Assert.True(vm.LastApplySucceeded);
        Assert.NotEqual("Imported", vm.Settings.Theme.ThemeName);
        Assert.NotEqual("Imported", manager.ActiveTheme?.ThemeName);
        vm.Dispose();
    }

    [Fact]
    public void AutoStartMismatch_KeepsSavedChoiceAndCancelDoesNotChangeRegistration()
    {
        var autoStart = new StubAutoStartService();
        var vm = Create(
            new CountingSummaryService(),
            settings: new Settings { AutoStartOnLogin = true },
            autoStart: autoStart);

        Assert.True(vm.Settings.AutoStartOnLogin);
        Assert.False(vm.ObservedAutoStartEnabled);
        Assert.True(vm.HasAutoStartDrift);
        vm.Settings.AutoStartOnLogin = false;
        vm.RestoreAppearancePreview();

        Assert.Equal(0, autoStart.SetCalls);
        vm.Dispose();
    }

    [Fact]
    public async Task UnrelatedApply_ReconcilesSavedAutoStartChoice()
    {
        var autoStart = new StubAutoStartService();
        var vm = Create(
            new CountingSummaryService(),
            settings: new Settings { AutoStartOnLogin = true },
            autoStart: autoStart);
        vm.Settings.BreakIntervalMinutes = 25;

        await ((ReactiveCommand<Unit, Unit>)vm.ApplyCommand).Execute().ToTask();

        Assert.True(vm.LastApplySucceeded);
        Assert.True(autoStart.Enabled);
        Assert.False(vm.HasAutoStartDrift);
        vm.Dispose();
    }

    [Fact]
    public async Task FailedAutoStartReconciliation_LeavesDraftAndShowsError()
    {
        var autoStart = new StubAutoStartService { FailWhenEnabled = true };
        var vm = Create(
            new CountingSummaryService(),
            settings: new Settings { AutoStartOnLogin = true },
            autoStart: autoStart);
        vm.Settings.BreakIntervalMinutes = 25;

        await ((ReactiveCommand<Unit, Unit>)vm.ApplyCommand).Execute().ToTask();

        Assert.False(vm.LastApplySucceeded);
        Assert.True(vm.HasCommitError);
        Assert.Equal(25, vm.Settings.BreakIntervalMinutes);
        Assert.False(autoStart.Enabled);
        vm.Dispose();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DelayedCommit_PreventsDiscardUntilApplyOrOkFinishes(bool useOk)
    {
        var activation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var vm = Create(new CountingSummaryService(), settings: new Settings());
        vm.SetRuntimeActivator(_ => activation.Task);
        vm.Settings.BreakIntervalMinutes = 25;
        var command = (ReactiveCommand<Unit, Unit>)(useOk ? vm.OkCommand : vm.ApplyCommand);

        Task commit = command.Execute().ToTask();
        await WaitUntilAsync(() => vm.IsCommitting);
        Assert.False(vm.CanClose);
        Assert.False(vm.CanCommit);
        Assert.False(vm.CanEdit);
        Assert.True(vm.IsDraftVisible);
        Assert.Equal("Saving...", vm.SavingStatus);
        bool committedMode = vm.Settings.UseCompactMode;
        vm.ToggleCompactModePreview();
        Assert.Equal(committedMode, vm.Settings.UseCompactMode);
        string originalAccent = vm.Settings.Theme.AccentPrimary;
        vm.SetThemeColor(nameof(Theme.AccentPrimary), "#123456");
        Assert.Equal(originalAccent, vm.Settings.Theme.AccentPrimary);
        vm.RestoreAppearancePreview();
        Assert.Equal(25, vm.Settings.BreakIntervalMinutes);

        activation.SetResult();
        await commit;
        Assert.True(vm.CanClose);
        Assert.Equal(string.Empty, vm.SavingStatus);
        Assert.True(vm.IsDraftVisible);
        Assert.True(vm.LastApplySucceeded);
        vm.Dispose();
    }

    [Fact]
    public async Task RecoveryRequired_CloseWarnsBeforeLeavingPendingState()
    {
        var provider = new ControlledSettingsProvider();
        var vm = Create(new CountingSummaryService(), provider: provider);
        provider.FailLoad(new SettingsRecoveryRequiredException());
        await WaitUntilAsync(() => vm.RecoveryRequired);

        Assert.False(vm.TryDiscardAndClose());
        Assert.Contains("Close again", vm.CommitError);
        Assert.True(vm.TryDiscardAndClose());
        vm.Dispose();
    }

    [Fact]
    public async Task ApplyThenFurtherEditsAndCancel_RestoresAppliedValuesAcrossTabs()
    {
        var provider = new StubSettingsProvider(new Settings());
        var manager = new ThemeManager();
        var vm = Create(new CountingSummaryService(), manager, provider: provider);
        vm.Settings.BreakIntervalMinutes = 25;
        vm.ActivityPollingIntervalInput = 5;
        vm.SelectedBlurMode = WidgetBlurModes.Solid;

        await ((ReactiveCommand<Unit, Unit>)vm.ApplyCommand).Execute().ToTask();
        Assert.True(vm.LastApplySucceeded);
        vm.Settings.BreakIntervalMinutes = 35;
        vm.ActivityPollingIntervalInput = 60;
        vm.SelectedBlurMode = WidgetBlurModes.Off;

        Assert.True(vm.TryDiscardAndClose());
        Assert.Equal(25, vm.Settings.BreakIntervalMinutes);
        Assert.Equal(5, vm.ActivityPollingIntervalInput);
        Assert.Equal(WidgetBlurModes.Solid, manager.ActiveTheme!.WidgetBlurMode);
        Assert.Equal(25, provider.Saved.BreakIntervalMinutes);
        vm.Dispose();
    }

    [Fact]
    public async Task FailedSave_KeepsDraftVisibleAndAllowsRetry()
    {
        var provider = new StubSettingsProvider(new Settings()) { FailSave = true };
        var vm = Create(new CountingSummaryService(), provider: provider);
        vm.Settings.BreakIntervalMinutes = 25;

        await ((ReactiveCommand<Unit, Unit>)vm.ApplyCommand).Execute().ToTask();
        Assert.False(vm.LastApplySucceeded);
        Assert.True(vm.HasCommitError);
        Assert.Equal(string.Empty, vm.SavingStatus);
        Assert.Equal(25, vm.Settings.BreakIntervalMinutes);
        Assert.Equal(50, provider.Saved.BreakIntervalMinutes);

        provider.FailSave = false;
        await ((ReactiveCommand<Unit, Unit>)vm.ApplyCommand).Execute().ToTask();
        Assert.True(vm.LastApplySucceeded);
        Assert.False(vm.HasCommitError);
        Assert.Equal(25, provider.Saved.BreakIntervalMinutes);
        vm.Dispose();
    }

    [Fact]
    public async Task RecoveryRetry_ReenablesEditingAfterPendingLoadIsResolved()
    {
        var provider = new ControlledSettingsProvider();
        var vm = Create(new CountingSummaryService(), provider: provider);
        provider.FailLoad(new SettingsRecoveryRequiredException());
        await WaitUntilAsync(() => vm.RecoveryRequired);
        Assert.False(vm.CanCommit);

        provider.NextLoad = new Settings { BreakIntervalMinutes = 25 };
        await ((ReactiveCommand<Unit, Unit>)vm.RetryRecoveryCommand).Execute().ToTask();

        Assert.True(vm.IsSettingsLoaded);
        Assert.True(vm.CanCommit);
        Assert.False(vm.RecoveryRequired);
        Assert.Equal(25, vm.Settings.BreakIntervalMinutes);
        vm.Dispose();
    }

    [Fact]
    public void ThemePresetAndReset_PreviewDraftWithoutSavingOrMutatingThemeService()
    {
        var provider = new StubSettingsProvider(new Settings());
        var themeService = new ThemeService();
        var manager = new ThemeManager();
        var vm = Create(new CountingSummaryService(), manager, provider: provider, themeService: themeService);

        vm.SelectedThemeName = "Light";
        Assert.Equal("Light", manager.ActiveTheme!.ThemeName);
        Assert.Equal("Dark", themeService.CurrentTheme.ThemeName);

        vm.ResetThemeCommand.Execute(null);
        Assert.Equal("Dark", manager.ActiveTheme!.ThemeName);
        Assert.Equal("Dark", themeService.CurrentTheme.ThemeName);
        Assert.Equal(0, provider.SaveCalls);
        vm.Dispose();
    }

    [Fact]
    public void Discard_RestoresFullDraftAndIsIdempotent()
    {
        var saved = new Settings { BreakIntervalMinutes = 25, AutoStartOnLogin = true };
        var vm = Create(new CountingSummaryService(), settings: saved);
        vm.Settings.BreakIntervalMinutes = 35;
        vm.Settings.AutoStartOnLogin = false;
        vm.SelectedTabIndex = SettingsWindowViewModel.SummaryTabIndex;

        vm.RestoreAppearancePreview();
        vm.RestoreAppearancePreview();

        Assert.Equal(25, vm.Settings.BreakIntervalMinutes);
        Assert.True(vm.Settings.AutoStartOnLogin);
        Assert.Equal(25, saved.BreakIntervalMinutes);
        vm.Dispose();
    }

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
                WidgetBlurMode = WidgetBlurModes.Off,
            },
        };
        var vm = Create(new CountingSummaryService(), themeManager, savedSettings);

        vm.BackgroundOpacityPercent = 25;
        vm.SelectedBlurMode = WidgetBlurModes.Solid;

        Assert.Equal(0.25, themeManager.ActiveTheme!.BackgroundOpacity);
        Assert.Equal(WidgetBlurModes.Solid, themeManager.ActiveTheme.WidgetBlurMode);

        vm.RestoreAppearancePreview();

        Assert.Equal(0.8, themeManager.ActiveTheme!.BackgroundOpacity);
        Assert.Equal(WidgetBlurModes.Off, themeManager.ActiveTheme.WidgetBlurMode);
        vm.Dispose();
    }

    [Fact]
    public async Task WidgetAppearancePreview_ApplyAdvancesRestorePoint()
    {
        var themeManager = new ThemeManager();
        var vm = Create(new CountingSummaryService(), themeManager, new Settings());

        vm.SelectedBlurMode = WidgetBlurModes.Solid;
        await ((ReactiveCommand<Unit, Unit>)vm.ApplyCommand).Execute().ToTask();
        Assert.True(vm.LastApplySucceeded);

        vm.SelectedBlurMode = WidgetBlurModes.Off;
        vm.RestoreAppearancePreview();

        Assert.Equal(WidgetBlurModes.Solid, themeManager.ActiveTheme!.WidgetBlurMode);
        vm.Dispose();
    }

    [Fact]
    public void FirstRunSettings_UsesTheSameDarkPresetAsTheWidget()
    {
        var vm = Create(new CountingSummaryService(), new ThemeManager(), new Settings());

        Assert.Equal("Dark", vm.Settings.Theme.ThemeName);
        Assert.Equal(0.8, vm.Settings.Theme.BackgroundOpacity);
        Assert.Equal(WidgetBlurModes.Off, vm.SelectedBlurMode);
        Assert.Equal([WidgetBlurModes.Off, WidgetBlurModes.Solid], vm.AvailableBlurModes);
        vm.Dispose();
    }

    [Fact]
    public void TransparencyDiagnostics_ReportsAchievedLevelAndSolidFallback()
    {
        var manager = new ThemeManager();
        var vm = Create(new CountingSummaryService(), manager, new Settings());

        manager.ReportActualWidgetTransparency(WindowTransparencyLevel.Transparent);
        Assert.Contains("Requested: Off | Active: Transparent | Surface: transparent backdrop", vm.TransparencyDiagnostics);

        vm.SelectedBlurMode = WidgetBlurModes.Solid;
        Assert.False(vm.IsBackgroundTintOpacityAvailable);
        Assert.Contains("Requested: Solid | Active: Transparent | Surface: solid fill", vm.TransparencyDiagnostics);

        vm.SelectedBlurMode = WidgetBlurModes.Off;
        Assert.Contains("Requested: Off | Active: Transparent | Surface: transparent backdrop", vm.TransparencyDiagnostics);

        manager.ReportActualWidgetTransparency(WindowTransparencyLevel.None);
        Assert.Contains("Requested: Off | Active: None | Surface: solid fallback", vm.TransparencyDiagnostics);
        vm.Dispose();
    }

    [Fact]
    public void DiscardAfterLoadingMismatchedTheme_KeepsSolarizedForSliderPreview()
    {
        var themes = new ThemeService();
        var manager = new ThemeManager();
        var saved = new Settings
        {
            ActiveThemeName = "Solarized Dark",
            Theme = themes.GetBuiltInTheme("Monokai")!,
        };
        var vm = Create(new CountingSummaryService(), manager, saved);

        Assert.Equal("Solarized Dark", vm.Settings.Theme.ThemeName);
        vm.RestoreAppearancePreview();
        vm.BackgroundOpacityPercent = 25;
        vm.ClockOpacityPercent = 50;

        Assert.Equal("Solarized Dark", vm.Settings.Theme.ThemeName);
        Assert.Equal("Solarized Dark", manager.ActiveTheme!.ThemeName);
        Assert.Equal(themes.GetBuiltInTheme("Solarized Dark")!.WindowBackground, manager.ActiveTheme.WindowBackground);
        Assert.Equal(0.25, manager.ActiveTheme.BackgroundOpacity);
        Assert.Equal(0.5, manager.ActiveTheme.TimerOpacity);
        vm.Dispose();
    }

    [Fact]
    public void RebindingSelectedTheme_DoesNotResetSliderEdits()
    {
        var themes = new ThemeService();
        var manager = new ThemeManager();
        var vm = Create(new CountingSummaryService(), manager, new Settings
        {
            ActiveThemeName = "Solarized Dark",
            Theme = themes.GetBuiltInTheme("Solarized Dark")!,
        });
        vm.BackgroundOpacityPercent = 25;
        vm.ClockOpacityPercent = 50;

        vm.SelectedThemeName = "Solarized Dark";

        Assert.Equal(0.25, vm.Settings.Theme.BackgroundOpacity);
        Assert.Equal(0.5, vm.Settings.Theme.TimerOpacity);
        Assert.Equal("Solarized Dark", manager.ActiveTheme!.ThemeName);
        vm.Dispose();
    }

    [Theory]
    [InlineData("Apply")]
    [InlineData("OK")]
    [InlineData("SaveFailure")]
    [InlineData("ActivationFailure")]
    public async Task AppearanceLifecycle_PreviewsWholeDraftAndRestoresLastSuccessfulCommit(string action)
    {
        var saved = new Settings
        {
            WorkLoggingEnabled = false,
            Theme = new ThemeService().GetBuiltInTheme("Solarized Dark")!,
            ActiveThemeName = "Solarized Dark",
        };
        var provider = new StubSettingsProvider(saved);
        var manager = new ThemeManager();
        var notifications = new LinuxNotificationServiceStub();
        var tracker = new SessionTracker(new LinuxActiveWindowServiceStub(), NullAppLogger.Instance);
        using var timer = new TimerService(tracker);
        using var reminders = new BreakReminderService(notifications, provider);
        using var widget = new TimerWidgetViewModel(
            provider, NullAppLogger.Instance, new CsvSessionRepository(provider), notifications,
            tracker, reminders, timer, null, new EventBus());
        widget.ApplySettings(saved.Clone());
        double originalFontSize = widget.MainTimerFontSize;
        var vm = Create(new CountingSummaryService(), manager, provider: provider);
        vm.SetAppearancePreview(widget.PreviewAppearance);
        widget.SetCompactModeDraftToggle(() =>
        {
            vm.ToggleCompactModePreview();
            return true;
        });
        bool failActivation = action == "ActivationFailure";
        vm.SetRuntimeActivator(async candidate =>
        {
            manager.ApplyTheme(candidate.Theme);
            await widget.ActivateSettingsAsync(candidate);
            if (failActivation)
            {
                failActivation = false;
                throw new IOException("Synthetic activation failure.");
            }
        });

        vm.BackgroundOpacityPercent = 25;
        vm.ClockOpacityPercent = 50;
        vm.ButtonsOpacityPercent = 60;
        vm.OverallFadePercent = 40;
        vm.Settings.WidgetScale = 1.5;
        await ((ReactiveCommand<Unit, Unit>)widget.ToggleCompactModeCommand).Execute().ToTask();
        Assert.True(vm.Settings.UseCompactMode);
        vm.Settings.BreakIntervalMinutes = 25;
        vm.Settings.Theme.TimerText = "#123456";
        vm.PlayPauseColor = "#112233";
        vm.Settings.Theme.ButtonHover = "#223344";
        vm.Settings.Theme.ButtonPressed = "#334455";
        vm.Settings.Theme.ButtonDisabled = "#445566";
        vm.Settings.Theme.SuccessColor = "#556677";
        vm.Settings.Theme.DangerColor = "#667788";

        Assert.Equal(originalFontSize * 1.5, widget.MainTimerFontSize);
        Assert.True(widget.UseCompactMode);
        Assert.Equal(0.4, widget.OverallOpacity);
        Assert.Equal(0.5, widget.EffectiveClockOpacity);
        Assert.Equal(0.6, widget.EffectiveControlsOpacity);
        Assert.False(widget.Settings.UseCompactMode);
        Assert.Equal(saved.BreakIntervalMinutes, widget.Settings.BreakIntervalMinutes);
        Assert.Equal(0, provider.SaveCalls);
        Assert.Equal(1, provider.Saved.WidgetScale);
        Assert.Equal("#123456", manager.ActiveTheme!.TimerText);

        provider.FailSave = action == "SaveFailure";
        var command = (ReactiveCommand<Unit, Unit>)(action == "OK" ? vm.OkCommand : vm.ApplyCommand);
        await command.Execute().ToTask();
        bool success = action is "Apply" or "OK";
        Assert.Equal(success, vm.LastApplySucceeded);
        Assert.Equal(originalFontSize * 1.5, widget.MainTimerFontSize);
        Assert.Equal(0.4, widget.OverallOpacity);
        Assert.True(widget.UseCompactMode);
        Assert.Equal("#123456", manager.ActiveTheme!.TimerText);
        if (success)
        {
            Assert.Equal(1.5, provider.Saved.WidgetScale);
            Assert.Equal(0.4, provider.Saved.WidgetOpacity);
            Assert.Equal(0.5, provider.Saved.Theme.TimerOpacity);
            Assert.Equal(0.6, provider.Saved.Theme.ButtonOpacity);
            Assert.Equal("#123456", provider.Saved.Theme.TimerText);
            Assert.Equal("#112233", provider.Saved.Theme.PlayPauseColor);
            Assert.Equal("#223344", provider.Saved.Theme.ButtonHover);
            Assert.Equal("#334455", provider.Saved.Theme.ButtonPressed);
            Assert.Equal("#445566", provider.Saved.Theme.ButtonDisabled);
            Assert.Equal("#556677", provider.Saved.Theme.SuccessColor);
            Assert.Equal("#667788", provider.Saved.Theme.DangerColor);
            Assert.True(provider.Saved.UseCompactMode);
        }
        else
        {
            Assert.True(vm.HasCommitError);
            Assert.Equal(1, provider.Saved.WidgetScale);
            Assert.False(provider.Saved.UseCompactMode);
        }

        vm.Settings.WidgetScale = 2;
        vm.PlayPauseColor = "#FFFFFF";
        await ((ReactiveCommand<Unit, Unit>)widget.ToggleCompactModeCommand).Execute().ToTask();
        Assert.False(vm.Settings.UseCompactMode);
        vm.OverallFadePercent = 70;
        vm.ClockOpacityPercent = 20;
        Assert.True(vm.TryDiscardAndClose());
        widget.ClearAppearancePreview();

        Assert.Equal(originalFontSize * (success ? 1.5 : 1), widget.MainTimerFontSize);
        Assert.Equal(success, widget.UseCompactMode);
        Assert.Equal(success ? 0.4 : saved.WidgetOpacity, widget.OverallOpacity);
        Assert.Equal(success ? 0.5 : saved.Theme.TimerOpacity, widget.EffectiveClockOpacity);
        Assert.Equal(success ? 25 : saved.BreakIntervalMinutes, widget.Settings.BreakIntervalMinutes);
        Assert.Equal(success ? "#123456" : saved.Theme.TimerText, manager.ActiveTheme!.TimerText);
        Assert.Equal(success ? "#112233" : saved.Theme.PlayPauseColor, manager.ActiveTheme.PlayPauseColor);
        vm.Dispose();

        widget.SetCompactModeDraftToggle(() => false);
        var reopened = Create(new CountingSummaryService(), provider: provider);
        Assert.Equal(success ? "#112233" : saved.Theme.PlayPauseColor, reopened.Settings.Theme.PlayPauseColor);
        Assert.Equal(success ? "#556677" : saved.Theme.SuccessColor, reopened.Settings.Theme.SuccessColor);
        Assert.Equal(success ? "#667788" : saved.Theme.DangerColor, reopened.Settings.Theme.DangerColor);
        reopened.Dispose();
        provider.FailSave = false;
        int savesBeforeWidgetToggle = provider.SaveCalls;
        await ((ReactiveCommand<Unit, Unit>)widget.ToggleCompactModeCommand).Execute().ToTask();
        Assert.Equal(!success, widget.UseCompactMode);
        Assert.Equal(!success, provider.Saved.UseCompactMode);
        Assert.Equal(savesBeforeWidgetToggle + 1, provider.SaveCalls);
    }

    [Theory]
    [InlineData(nameof(Theme.TimerText))]
    [InlineData(nameof(Theme.PlayPauseColor))]
    public async Task InvalidThemeColor_LeavesLastValidPreviewAndCannotBeSaved(string colorProperty)
    {
        var manager = new ThemeManager();
        var provider = new StubSettingsProvider(new Settings());
        var vm = Create(new CountingSummaryService(), manager, provider: provider);
        vm.SetAppearancePreview(_ => { });
        string originalColor = manager.ActiveTheme!.TimerText;
        vm.SetThemeColor(colorProperty, "#12");
        Assert.Equal(originalColor, manager.ActiveTheme.TimerText);

        await ((ReactiveCommand<Unit, Unit>)vm.ApplyCommand).Execute().ToTask();

        Assert.False(vm.LastApplySucceeded);
        Assert.Contains(colorProperty, vm.CommitError);
        Assert.Equal(string.Empty, vm.SavingStatus);
        Assert.Equal(0, provider.SaveCalls);
        vm.Dispose();
    }

    internal static SettingsWindowViewModel CreateAppearanceEditor() => Create(new CountingSummaryService());

    private static SettingsWindowViewModel Create(
        CountingSummaryService summary,
        ThemeManager? themeManager = null,
        Settings? settings = null,
        ISettingsProvider? provider = null,
        IAutoStartService? autoStart = null,
        IThemeService? themeService = null) => new(
        provider ?? new StubSettingsProvider(settings),
        autoStart ?? new StubAutoStartService(),
        themeService ?? new ThemeService(),
        themeManager ?? new ThemeManager(),
        NullAppLogger.Instance,
        new WorklogSummaryViewModel(
            summary,
            new WorklogGroupingRegistry([new ApplicationGrouping(), new ProjectGrouping()]),
            TimeProvider.System));

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (int attempt = 0; attempt < 100 && !condition(); attempt++)
        {
            await Task.Delay(10);
        }

        Assert.True(condition());
    }

    private sealed class ControlledSettingsProvider : ISettingsProvider
    {
        private readonly TaskCompletionSource<Settings> _firstLoad = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _loadCalls;

        public Settings NextLoad { get; set; } = new();

        public int SaveCalls { get; private set; }

        public Task<Settings> LoadAsync() => Interlocked.Increment(ref this._loadCalls) == 1
            ? this._firstLoad.Task : Task.FromResult(this.NextLoad);

        public Task SaveAsync(Settings settings)
        {
            this.SaveCalls++;
            return Task.CompletedTask;
        }

        public void CompleteLoad(Settings settings) => this._firstLoad.SetResult(settings);

        public void FailLoad(Exception? exception = null) => this._firstLoad.SetException(
            exception ?? new IOException("Existing settings cannot be read."));
    }

    private sealed class DelayedImportThemeService : IThemeService
    {
        private readonly ThemeService _inner = new();
        private readonly TaskCompletionSource<Theme> _import =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Theme CurrentTheme => this._inner.CurrentTheme;

        public IReadOnlyList<Theme> BuiltInThemes => this._inner.BuiltInThemes;

        public void CompleteImport(Theme theme) => this._import.SetResult(theme);

        public void ApplyTheme(Theme theme) => this._inner.ApplyTheme(theme);

        public Task<Theme> LoadThemeFromFileAsync(string filePath) => this._import.Task;

        public Task SaveThemeToFileAsync(Theme theme, string filePath) =>
            this._inner.SaveThemeToFileAsync(theme, filePath);

        public Theme? GetBuiltInTheme(string themeName) => this._inner.GetBuiltInTheme(themeName);

        public void ResetToDefault() => this._inner.ResetToDefault();

        public bool ValidateTheme(Theme theme) => this._inner.ValidateTheme(theme);
    }

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
        public Settings Saved { get; private set; } = settings?.Clone() ?? new Settings();

        public bool FailSave { get; set; }

        public int SaveCalls { get; private set; }

        public Task<Settings> LoadAsync() => Task.FromResult(this.Saved.Clone());

        public Task SaveAsync(Settings settings)
        {
            if (this.FailSave)
            {
                throw new IOException("Synthetic save failure.");
            }

            this.Saved = settings.Clone();
            this.SaveCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class StubAutoStartService : IAutoStartService
    {
        public int SetCalls { get; private set; }

        public bool Enabled { get; private set; }

        public bool FailWhenEnabled { get; set; }

        public void SetAutoStart(bool enabled)
        {
            this.SetCalls++;
            if (enabled && this.FailWhenEnabled)
            {
                throw new IOException("Registration denied.");
            }

            this.Enabled = enabled;
        }

        public bool IsAutoStartEnabled() => this.Enabled;
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
