namespace FocusTimer.App.Tests;

using System.Reactive;
using System.Reactive.Threading.Tasks;
using Avalonia.Layout;
using Avalonia.Media;
using FocusTimer.App.Styles;
using FocusTimer.App.ViewModels;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;
using FocusTimer.Core.Stubs;
using ReactiveUI;

public class TimerWidgetBehaviorTests
{
    [Theory]
    [InlineData(TimerState.Idle)]
    [InlineData(TimerState.Paused)]
    [InlineData(TimerState.Running)]
    public async Task Toggle_UsesTimerStateAndPassesTheProjectTag(TimerState state)
    {
        using var fixture = new Fixture();
        fixture.Timer.State = state;
        fixture.ViewModel.ProjectTag = "customer work";

        await ExecuteAsync(fixture.ViewModel.ToggleCommand);

        Assert.Equal(state == TimerState.Running ? 0 : 1, fixture.Timer.Starts);
        Assert.Equal(state == TimerState.Running ? 1 : 0, fixture.Timer.Pauses.Count);
        if (state != TimerState.Running)
            Assert.Equal("customer work", fixture.Timer.LastProjectTag);
        else
            Assert.Equal(EndReason.ManualPause, Assert.Single(fixture.Timer.Pauses));
    }

    [Fact]
    public async Task ResetAndIdlePause_KeepTheirDistinctTimerSemantics()
    {
        using var fixture = new Fixture();

        await ExecuteAsync(fixture.ViewModel.ResetCommand);
        fixture.ViewModel.PauseForIdle();

        Assert.Equal(1, fixture.Timer.Resets);
        Assert.Equal(EndReason.IdlePause, Assert.Single(fixture.Timer.Pauses));
    }

    [Fact]
    public async Task ProjectInputToggle_OpensAndClosesWithoutStartingTheTimer()
    {
        using var fixture = new Fixture();

        await ExecuteAsync(fixture.ViewModel.ToggleProjectInputCommand);
        Assert.True(fixture.ViewModel.IsProjectInputVisible);
        await ExecuteAsync(fixture.ViewModel.ToggleProjectInputCommand);
        Assert.False(fixture.ViewModel.IsProjectInputVisible);
        Assert.Equal(0, fixture.Timer.Starts);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CompactMode_WhenDraftOwnsAction_DoesNotPersistCommittedSettings(bool initiallyCompact)
    {
        using var fixture = new Fixture();
        fixture.ViewModel.ApplySettings(new Settings { UseCompactMode = initiallyCompact });
        int draftCalls = 0;
        fixture.ViewModel.SetCompactModeDraftToggle(() => { draftCalls++; return true; });

        await ExecuteAsync(fixture.ViewModel.ToggleCompactModeCommand);

        Assert.Equal(1, draftCalls);
        Assert.Equal(initiallyCompact, fixture.ViewModel.Settings.UseCompactMode);
        Assert.Empty(fixture.Provider.Saves);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CompactMode_WhenNoDraftOwnsAction_TogglesAndPersists(bool initiallyCompact)
    {
        using var fixture = new Fixture();
        fixture.ViewModel.ApplySettings(new Settings { UseCompactMode = initiallyCompact });
        fixture.ViewModel.SetCompactModeDraftToggle(() => false);

        await ExecuteAsync(fixture.ViewModel.ToggleCompactModeCommand);

        Assert.Equal(!initiallyCompact, fixture.ViewModel.UseCompactMode);
        Assert.Equal(!initiallyCompact, Assert.Single(fixture.Provider.Saves).UseCompactMode);
    }

    [Fact]
    public async Task CompactMode_SaveFailureIsLoggedAndTheCommandRemainsUsable()
    {
        using var fixture = new Fixture();
        fixture.Provider.FailSave = true;
        await ExecuteAsync(fixture.ViewModel.ToggleCompactModeCommand);

        Assert.True(fixture.ViewModel.UseCompactMode);
        Assert.Contains(fixture.Logger.Errors, message => message.Contains("toggling compact mode"));

        fixture.Provider.FailSave = false;
        await ExecuteAsync(fixture.ViewModel.ToggleCompactModeCommand);
        Assert.False(Assert.Single(fixture.Provider.Saves).UseCompactMode);
    }

    [Fact]
    public void AppearancePreview_IsAnIndependentSnapshotAndCanBeDiscarded()
    {
        using var fixture = new Fixture();
        var committed = new Settings { WidgetOpacity = 0.8 };
        committed.Theme.WidgetBaseOpacity = 0.7;
        fixture.ViewModel.ApplySettings(committed);
        var draft = new Settings { UseCompactMode = true, WidgetScale = 1.5, WidgetOpacity = 0.5 };
        draft.Theme.WidgetBaseOpacity = 0.37;
        draft.Theme.WindowBackground = "#123456";

        fixture.ViewModel.PreviewAppearance(draft);
        draft.Theme.WidgetBaseOpacity = 0.9;
        draft.UseCompactMode = false;

        Assert.Same(committed, fixture.ViewModel.Settings);
        Assert.True(fixture.ViewModel.UseCompactMode);
        Assert.Equal(0.37, fixture.ViewModel.EffectiveWidgetBaseOpacity);
        Assert.Equal(0.5, fixture.ViewModel.OverallOpacity);
        Assert.Equal(Orientation.Vertical, fixture.ViewModel.ButtonPanelOrientation);
        Assert.Equal(Color.Parse("#123456"), Assert.IsType<SolidColorBrush>(fixture.ViewModel.BackgroundBrush).Color);

        fixture.ViewModel.ClearAppearancePreview();
        fixture.ViewModel.ClearAppearancePreview();
        Assert.False(fixture.ViewModel.UseCompactMode);
        Assert.Equal(0.7, fixture.ViewModel.EffectiveWidgetBaseOpacity);
        Assert.Equal(0.8, fixture.ViewModel.OverallOpacity);
        Assert.Equal(Orientation.Horizontal, fixture.ViewModel.ButtonPanelOrientation);
        Assert.Empty(fixture.Provider.Saves);
    }

    [Theory]
    [InlineData(0.5, Orientation.Horizontal)]
    [InlineData(1.24, Orientation.Horizontal)]
    [InlineData(1.25, Orientation.Vertical)]
    [InlineData(2.0, Orientation.Vertical)]
    public void ScaleChange_ReflowsTheLayoutAndPreservesAccessibleButtonTargets(double scale, Orientation orientation)
    {
        using var fixture = new Fixture();

        fixture.ViewModel.Settings.WidgetScale = scale;

        Assert.Equal(orientation, fixture.ViewModel.ButtonPanelOrientation);
        Assert.Equal(DesignMetrics.BaseMainTimerFontSize * scale, fixture.ViewModel.MainTimerFontSize);
        Assert.Equal(DesignMetrics.BaseCompactTimerTextWidth * scale, fixture.ViewModel.CompactTimerTextWidth);
        Assert.True(fixture.ViewModel.ButtonSize >= DesignMetrics.MinAccessibleHitTarget);
        Assert.True(fixture.ViewModel.CompactButtonSize >= DesignMetrics.MinAccessibleHitTarget);
    }

    [Theory]
    [InlineData(-1.0, 0.0, 0.2)]
    [InlineData(0.4, 0.4, 0.4)]
    [InlineData(2.0, 1.0, 1.0)]
    public void OpacityEdits_ClampEachLayerAndApplyOverallFadeOnlyOnce(double input, double layer, double overall)
    {
        using var fixture = new Fixture();
        var vm = fixture.ViewModel;

        vm.BackgroundOpacity = input;
        vm.ClockOpacity = input;
        vm.ControlsOpacity = input;
        vm.OverallOpacity = input;

        Assert.Equal(layer, vm.Settings.Theme.BackgroundOpacity);
        Assert.Equal(layer, vm.EffectiveBackgroundOpacity);
        Assert.Equal(layer, vm.EffectiveClockOpacity);
        Assert.Equal(layer, vm.EffectiveControlsOpacity);
        Assert.Equal(overall, vm.Settings.WidgetOpacity);
    }

    [Fact]
    public void OpacityEdits_IgnoreSubToleranceChangesWithoutNotifyingBindings()
    {
        using var fixture = new Fixture();
        var vm = fixture.ViewModel;
        vm.BackgroundOpacity = vm.ClockOpacity = vm.ControlsOpacity = vm.OverallOpacity = 0.5;
        var notifications = new List<string?>();
        vm.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

        vm.BackgroundOpacity = vm.ClockOpacity = vm.ControlsOpacity = vm.OverallOpacity = 0.50001;

        Assert.Empty(notifications);
        Assert.Equal(0.5, vm.BackgroundOpacity);
        Assert.Equal(0.5, vm.ClockOpacity);
        Assert.Equal(0.5, vm.ControlsOpacity);
        Assert.Equal(0.5, vm.OverallOpacity);
    }

    [Theory]
    [InlineData(nameof(Theme.BackgroundOpacity), nameof(TimerWidgetViewModel.EffectiveBackgroundOpacity))]
    [InlineData(nameof(Theme.TimerOpacity), nameof(TimerWidgetViewModel.EffectiveClockOpacity))]
    [InlineData(nameof(Theme.ButtonOpacity), nameof(TimerWidgetViewModel.EffectiveControlsOpacity))]
    [InlineData(nameof(Theme.WindowBackground), nameof(TimerWidgetViewModel.BackgroundBrush))]
    public void ThemeEdit_NotifiesTheCorrespondingWidgetBinding(string themeProperty, string widgetProperty)
    {
        using var fixture = new Fixture();
        var notifications = new List<string?>();
        fixture.ViewModel.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

        typeof(Theme).GetProperty(themeProperty)!.SetValue(fixture.ViewModel.Settings.Theme,
            themeProperty == nameof(Theme.WindowBackground) ? "#abcdef" : (object)0.4);

        Assert.Contains(widgetProperty, notifications);
    }

    [Fact]
    public void InvalidBackgroundColor_UsesTransparentBrushWithoutThrowing()
    {
        using var fixture = new Fixture();
        fixture.ViewModel.Settings.Theme.WindowBackground = "not a color";

        Assert.Same(Brushes.Transparent, fixture.ViewModel.BackgroundBrush);
    }

    [Fact]
    public async Task ReloadFailure_PreservesCommittedSettingsAndLogsTheFailure()
    {
        using var fixture = new Fixture();
        var committed = new Settings { UseCompactMode = true };
        fixture.ViewModel.ApplySettings(committed);
        fixture.Provider.FailLoad = true;

        await fixture.ViewModel.ReloadSettingsAsync();

        Assert.Same(committed, fixture.ViewModel.Settings);
        Assert.Contains("Failed to load settings.", fixture.Logger.Errors);
    }

    [Fact]
    public void Dispose_StopsOnceWithApplicationExitAndDetachesAppearanceSubscriptions()
    {
        using var fixture = new Fixture();
        fixture.ViewModel.Dispose();
        fixture.ViewModel.Dispose();
        var notifications = new List<string?>();
        fixture.ViewModel.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

        fixture.ViewModel.Settings.WidgetScale = 1.5;
        fixture.ViewModel.Settings.Theme.BackgroundOpacity = 0.3;

        Assert.Empty(notifications);
        Assert.Equal(EndReason.ApplicationExit, Assert.Single(fixture.Timer.Stops));
    }

    private static Task ExecuteAsync(System.Windows.Input.ICommand command) =>
        ((ReactiveCommand<Unit, Unit>)command).Execute().ToTask();

    private sealed class Fixture : IDisposable
    {
        private readonly BreakReminderService _reminders;
        public Provider Provider { get; } = new();
        public RecordingLogger Logger { get; } = new();
        public RecordingTimer Timer { get; } = new();
        public TimerWidgetViewModel ViewModel { get; }
        public Fixture()
        {
            var notifications = new LinuxNotificationServiceStub();
            var tracker = new SessionTracker(new LinuxActiveWindowServiceStub(), this.Logger);
            this._reminders = new BreakReminderService(notifications, this.Provider);
            this.ViewModel = new TimerWidgetViewModel(this.Provider, this.Logger, new EmptyStore(),
                notifications, tracker, this._reminders, this.Timer, null, new EventBus());
        }
        public void Dispose() { this.ViewModel.Dispose(); this._reminders.Dispose(); }
    }

    private sealed class Provider : ISettingsProvider
    {
        public bool FailSave { get; set; }
        public bool FailLoad { get; set; }
        public List<Settings> Saves { get; } = new();
        public Task<Settings> LoadAsync() => this.FailLoad
            ? Task.FromException<Settings>(new IOException("Settings unreadable."))
            : Task.FromResult(new Settings());
        public Task SaveAsync(Settings settings)
        {
            if (this.FailSave) throw new IOException("Settings write denied.");
            this.Saves.Add(settings.Clone());
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingTimer : ITimerService
    {
        public event EventHandler<TimerState>? StateChanged { add { } remove { } }
        public event EventHandler<TimeSpan> Tick { add { } remove { } }
        public TimerState State { get; set; }
        public TimerState CurrentState => this.State;
        public TimeSpan Elapsed => TimeSpan.Zero;
        public int Starts { get; private set; }
        public int Resets { get; private set; }
        public string? LastProjectTag { get; private set; }
        public List<EndReason> Pauses { get; } = new();
        public List<EndReason> Stops { get; } = new();
        public void Start(string? projectTag = null) { this.Starts++; this.LastProjectTag = projectTag; }
        public void Pause(EndReason reason = EndReason.ManualPause) => this.Pauses.Add(reason);
        public void Stop(EndReason reason = EndReason.ManualPause) => this.Stops.Add(reason);
        public void Reset() => this.Resets++;
    }

    private sealed class RecordingLogger : IAppLogger
    {
        public List<string> Errors { get; } = new();
        public void LogError(string message, Exception? ex = null) => this.Errors.Add(message);
        public void LogCritical(string message, Exception? ex = null) { }
        public void LogDebug(string message) { }
        public void LogInformation(string message) { }
        public void LogWarning(string message) { }
    }

    private sealed class EmptyStore : IWorklogStore
    {
        public Task<WorklogOutcome> AppendAsync(IReadOnlyCollection<TimeEntry> entries, CancellationToken cancellationToken = default) =>
            Task.FromResult(WorklogOutcome.Success());
        public Task<WorklogReadResult> GetAsync(string entryId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorklogReadResult> QueryAsync(WorklogQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorklogOutcome> PatchAsync(string entryId, int expectedRevision, WorklogPatch patch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorklogOutcome> DeleteAsync(string entryId, int expectedRevision, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
