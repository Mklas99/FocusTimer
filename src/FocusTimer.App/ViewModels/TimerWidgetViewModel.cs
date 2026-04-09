namespace FocusTimer.App.ViewModels
{
    using System;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Avalonia.Layout;
    using Avalonia.ReactiveUI;
    using Avalonia.Threading;
    using FocusTimer.App.Services;
    using FocusTimer.Core;
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;
    using FocusTimer.Core.Services;
    using ReactiveUI;

    /// <summary>
    /// ViewModel for the timer widget window.
    /// Handles timer logic, state management, and commands.
    /// </summary>
    public class TimerWidgetViewModel : ReactiveObject, IDisposable
    {
        private readonly ISettingsProvider _settingsProvider;
        private readonly IAppLogger _logWriter;
        private readonly ISessionRepository _sessionRepository;
        private readonly SessionTracker _sessionTracker;
        private readonly BreakReminderService _breakReminderService;
        private readonly ITimerService _timerService;
        private readonly SemaphoreSlim _persistenceSemaphore = new(1, 1);
        private readonly Core.Interfaces.IEventBus? _eventBus;
        private System.ComponentModel.INotifyPropertyChanged? _themeNotifier;
        private System.ComponentModel.INotifyPropertyChanged? _settingsNotifier;

        private TimeSpan _elapsed;
        private string _elapsedFormatted = "00:00:00";
        private bool _disposed;
        private bool _isRunning;
        private string? _projectTag;
        private Settings _settings;
        private bool _isProjectInputVisible = false;

        // Responsive layout fields
        private Orientation _buttonPanelOrientation = Orientation.Horizontal;
        private double _mainTimerFontSize = 36;
        private double _compactTimerFontSize = 25;
        private double _mainTimerTextWidth = 182;
        private double _compactTimerTextWidth = 126;
        private double _projectFontSize = 14;
        private double _buttonSize = 20;
        private double _iconSize = 16;
        private double _compactButtonSize = 16;
        private double _compactIconSize = 13;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerWidgetViewModel"/> class.
        /// </summary>
        /// <param name="settingsProvider">The settings provider for loading and managing application settings.</param>
        /// <param name="logWriter">The logger for writing diagnostic and error messages.</param>
        /// <param name="sessionRepository">The repository for persisting and retrieving session data.</param>
        /// <param name="sessionTracker">The service for tracking the current session state.</param>
        /// <param name="breakReminderService">The service for managing break reminders.</param>
        /// <param name="timerService">The timer service for managing timer events and state.</param>
        /// <param name="hotkeyService">Optional global hotkey service used to bind window handles.</param>
        /// <param name="eventBus">Event bus for publishing application-level events.</param>
        public TimerWidgetViewModel(
            ISettingsProvider settingsProvider,
            IAppLogger logWriter,
            ISessionRepository sessionRepository,
            SessionTracker sessionTracker,
            BreakReminderService breakReminderService,
            ITimerService timerService,
            Core.Interfaces.IGlobalHotkeyService? hotkeyService,
            Core.Interfaces.IEventBus eventBus)
        {
            // Defensive: Ensure ViewModel is constructed on the UI thread
            this._settingsProvider = settingsProvider;
            this._logWriter = logWriter;
            this._sessionRepository = sessionRepository;
            this._sessionTracker = sessionTracker;
            this._breakReminderService = breakReminderService;
            this._timerService = timerService;
            this._settings = new Settings(); // Safe defaults with validation
            this.HotkeyService = hotkeyService;
            this._eventBus = eventBus;

            this.SubscribeToThemeChanges(this._settings.Theme);
            this.SubscribeToSettingsChanges(this._settings);

            // Initialize timer events
            this._timerService.Tick += (s, elapsed) =>
            {
                Dispatcher.UIThread.Post(() => this.OnTimerTick(elapsed));
            };

            this._timerService.StateChanged += (s, state) =>
            {
                Dispatcher.UIThread.Post(() => this.OnTimerStateChanged(state));
            };

            // Initialize commands with explicit UI thread scheduler
            this.ToggleCommand = ReactiveCommand.Create(this.Toggle, outputScheduler: RxApp.MainThreadScheduler);
            this.ResetCommand = ReactiveCommand.Create(this.Reset, outputScheduler: RxApp.MainThreadScheduler);
            this.ToggleProjectInputCommand = ReactiveCommand.Create(this.ToggleProjectInput, outputScheduler: RxApp.MainThreadScheduler);

            // Toggle full/compact mode. We persist the setting asynchronously to avoid blocking the UI.
            this.ToggleCompactModeCommand = ReactiveCommand.CreateFromTask(
                async () =>
                {
                    // Flip the compact mode flag
                    this.Settings.UseCompactMode = !this.Settings.UseCompactMode;

                    try
                    {
                        await this._settingsProvider.SaveAsync(this.Settings);
                    }
                    catch (Exception ex)
                    {
                        this._logWriter.LogError("Failed to save settings after toggling compact mode.", ex);
                    }
                },
                outputScheduler: RxApp.MainThreadScheduler);
        }

        /// <summary>
        /// Gets or sets the orientation of the button panel for responsive layout.
        /// </summary>
        public Orientation ButtonPanelOrientation
        {
            get => this._buttonPanelOrientation;
            set => this.RaiseAndSetIfChanged(ref this._buttonPanelOrientation, value);
        }

        /// <summary>
        /// Gets or sets the main timer font size for responsive layout.
        /// </summary>
        public double MainTimerFontSize
        {
            get => this._mainTimerFontSize;
            set => this.RaiseAndSetIfChanged(ref this._mainTimerFontSize, value);
        }

        /// <summary>
        /// Gets or sets the compact timer font size for responsive layout.
        /// </summary>
        public double CompactTimerFontSize
        {
            get => this._compactTimerFontSize;
            set => this.RaiseAndSetIfChanged(ref this._compactTimerFontSize, value);
        }

        /// <summary>
        /// Gets or sets a fixed width for the main timer text to avoid window jitter while ticking.
        /// </summary>
        public double MainTimerTextWidth
        {
            get => this._mainTimerTextWidth;
            set => this.RaiseAndSetIfChanged(ref this._mainTimerTextWidth, value);
        }

        /// <summary>
        /// Gets or sets a fixed width for the compact timer text to avoid window jitter while ticking.
        /// </summary>
        public double CompactTimerTextWidth
        {
            get => this._compactTimerTextWidth;
            set => this.RaiseAndSetIfChanged(ref this._compactTimerTextWidth, value);
        }

        /// <summary>
        /// Gets or sets the button size for responsive layout.
        /// </summary>
        public double ButtonSize
        {
            get => this._buttonSize;
            set => this.RaiseAndSetIfChanged(ref this._buttonSize, value);
        }

        /// <summary>
        /// Gets or sets the icon size for responsive layout.
        /// </summary>
        public double IconSize
        {
            get => this._iconSize;
            set => this.RaiseAndSetIfChanged(ref this._iconSize, value);
        }

        /// <summary>
        /// Gets or sets the compact-mode button size for responsive layout.
        /// </summary>
        public double CompactButtonSize
        {
            get => this._compactButtonSize;
            set => this.RaiseAndSetIfChanged(ref this._compactButtonSize, value);
        }

        /// <summary>
        /// Gets or sets the compact-mode icon size for responsive layout.
        /// </summary>
        public double CompactIconSize
        {
            get => this._compactIconSize;
            set => this.RaiseAndSetIfChanged(ref this._compactIconSize, value);
        }

        /// <summary>
        /// Gets or sets the secondary font size used by smaller labels.
        /// </summary>
        public double ProjectFontSize
        {
            get => this._projectFontSize;
            set => this.RaiseAndSetIfChanged(ref this._projectFontSize, value);
        }

        /// <summary>
        /// Gets brush used for the widget background. Always fully opaque; visual transparency
        /// is controlled via EffectiveBackgroundOpacity on the background layer.
        /// </summary>
        public Avalonia.Media.IBrush BackgroundBrush
        {
            get
            {
                var colorStr = this.Settings?.Theme?.WindowBackground ?? "#FF000000";
                try
                {
                    var color = Avalonia.Media.Color.Parse(colorStr);
                    return new Avalonia.Media.SolidColorBrush(color, 1.0);
                }
                catch
                {
                    return Avalonia.Media.Brushes.Transparent;
                }
            }
        }

        /// <summary>
        /// Gets the logger for UI components that need it (e.g., windows).
        /// </summary>
        public IAppLogger? Logger => this._logWriter;

        /// <summary>
        /// Gets the hotkey service used for window handle wiring.
        /// </summary>
        public Core.Interfaces.IGlobalHotkeyService? HotkeyService { get; }

        /// <summary>
        /// Gets or sets base background opacity (0..1) proxied to the theme.
        /// </summary>
        public double BackgroundOpacity
        {
            get => this.Settings?.Theme?.BackgroundOpacity ?? 1.0;
            set
            {
                if (this.Settings?.Theme is null)
                {
                    return;
                }

                var clamped = Math.Clamp(value, 0.0, 1.0);
                if (!clamped.Equals(this.Settings.Theme.BackgroundOpacity))
                {
                    this.Settings.Theme.BackgroundOpacity = clamped;
                    this.RaisePropertyChanged(nameof(this.BackgroundOpacity));
                    this.RaisePropertyChanged(nameof(this.EffectiveBackgroundOpacity));
                    this.RaisePropertyChanged(nameof(this.BackgroundBrush));
                }
            }
        }

        /// <summary>
        /// Gets or sets base clock/text opacity (0..1) proxied to the theme.
        /// </summary>
        public double ClockOpacity
        {
            get => this.Settings?.Theme?.TimerOpacity ?? 1.0;
            set
            {
                if (this.Settings?.Theme is null)
                {
                    return;
                }

                var clamped = Math.Clamp(value, 0.0, 1.0);
                if (!clamped.Equals(this.Settings.Theme.TimerOpacity))
                {
                    this.Settings.Theme.TimerOpacity = clamped;
                    this.RaisePropertyChanged(nameof(this.ClockOpacity));
                    this.RaisePropertyChanged(nameof(this.EffectiveClockOpacity));
                }
            }
        }

        /// <summary>
        /// Gets or sets base controls/buttons opacity (0..1) proxied to the theme.
        /// </summary>
        public double ControlsOpacity
        {
            get => this.Settings?.Theme?.ButtonOpacity ?? 1.0;
            set
            {
                if (this.Settings?.Theme is null)
                {
                    return;
                }

                var clamped = Math.Clamp(value, 0.0, 1.0);
                if (!clamped.Equals(this.Settings.Theme.ButtonOpacity))
                {
                    this.Settings.Theme.ButtonOpacity = clamped;
                    this.RaisePropertyChanged(nameof(this.ControlsOpacity));
                    this.RaisePropertyChanged(nameof(this.EffectiveControlsOpacity));
                }
            }
        }

        /// <summary>
        /// Gets or sets overall opacity multiplier (0.2..1) applied to background, clock and controls.
        /// Backed by Settings.WidgetOpacity so it is persisted.
        /// </summary>
        public double OverallOpacity
        {
            get => this.Settings?.WidgetOpacity ?? 1.0;
            set
            {
                if (this.Settings is null)
                {
                    return;
                }

                var clamped = Math.Clamp(value, 0.2, 1.0);
                if (!clamped.Equals(this.Settings.WidgetOpacity))
                {
                    this.Settings.WidgetOpacity = clamped;
                    this.RaisePropertyChanged(nameof(this.OverallOpacity));
                    this.RaisePropertyChanged(nameof(this.EffectiveBackgroundOpacity));
                    this.RaisePropertyChanged(nameof(this.EffectiveClockOpacity));
                    this.RaisePropertyChanged(nameof(this.EffectiveControlsOpacity));
                }
            }
        }

        /// <summary>
        /// Gets effective opacities with the overall multiplier applied.
        /// </summary>
        public double EffectiveBackgroundOpacity => this.BackgroundOpacity * this.OverallOpacity;

        /// <summary>
        /// Gets the effective opacity for the rear widget shell (base layer) with the overall multiplier applied.
        /// </summary>
        public double EffectiveWidgetBaseOpacity => (this.Settings?.Theme?.WidgetBaseOpacity ?? 1.0) * this.OverallOpacity;

        /// <summary>
        /// Gets a value indicating whether the controls layer accepts pointer input.
        /// </summary>
        public bool AreControlsInteractable => true;

        /// <summary>
        /// Gets the effective opacity for the clock with the overall multiplier applied.
        /// </summary>
        public double EffectiveClockOpacity => this.ClockOpacity * this.OverallOpacity;

        /// <summary>
        /// Gets the effective opacity for controls with the overall multiplier applied.
        /// </summary>
        public double EffectiveControlsOpacity => this.ControlsOpacity * this.OverallOpacity;

        /// <summary>
        /// Gets current elapsed time.
        /// </summary>
        public TimeSpan Elapsed
        {
            get => this._elapsed;
            private set
            {
                var oldValue = this._elapsed;
                this.RaiseAndSetIfChanged(ref this._elapsed, value);

                // Update formatted string when elapsed changes
                if (oldValue != this._elapsed)
                {
                    this.ElapsedFormatted = $"{(int)this._elapsed.TotalHours:D2}:{this._elapsed.Minutes:D2}:{this._elapsed.Seconds:D2}";
                }
            }
        }

        /// <summary>
        /// Gets formatted elapsed time string (HH:MM:SS).
        /// Cached and only updated when Elapsed changes.
        /// </summary>
        public string ElapsedFormatted
        {
            get => this._elapsedFormatted;
            private set => this.RaiseAndSetIfChanged(ref this._elapsedFormatted, value);
        }

        /// <summary>
        /// Gets a value indicating whether whether the timer is currently running.
        /// </summary>
        public bool IsRunning
        {
            get => this._isRunning;
            private set => this.RaiseAndSetIfChanged(ref this._isRunning, value);
        }

        /// <summary>
        /// Gets or sets optional project/task tag for this session.
        /// </summary>
        public string? ProjectTag
        {
            get => this._projectTag;
            set
            {
                if (this._projectTag != value)
                {
                    this._projectTag = value;
                    this.RaisePropertyChanged(nameof(this.ProjectTag));
                    this.OnProjectTagChanged();
                }
            }
        }

        /// <summary>
        /// Gets current application settings.
        /// </summary>
        public Settings Settings
        {
            get => this._settings;
            private set
            {
                if (this._settings != value)
                {
                    this.RaiseAndSetIfChanged(ref this._settings, value);
                    this.SubscribeToThemeChanges(this._settings.Theme);
                    this.SubscribeToSettingsChanges(this._settings);
                    this.RaisePropertyChanged(nameof(this.BackgroundBrush));
                    this.RaisePropertyChanged(nameof(this.BackgroundOpacity));
                    this.RaisePropertyChanged(nameof(this.ClockOpacity));
                    this.RaisePropertyChanged(nameof(this.ControlsOpacity));
                    this.RaisePropertyChanged(nameof(this.OverallOpacity));
                    this.RaisePropertyChanged(nameof(this.EffectiveBackgroundOpacity));
                    this.RaisePropertyChanged(nameof(this.EffectiveClockOpacity));
                    this.RaisePropertyChanged(nameof(this.EffectiveControlsOpacity));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether controls visibility of the project input section.
        /// </summary>
        public bool IsProjectInputVisible
        {
            get => this._isProjectInputVisible;
            set => this.RaiseAndSetIfChanged(ref this._isProjectInputVisible, value);
        }

        /// <summary>
        /// Gets toggle between play and pause states.
        /// </summary>
        public ICommand ToggleCommand { get; }

        /// <summary>
        /// Gets reset the timer to zero and stop it.
        /// </summary>
        public ICommand ResetCommand { get; }

        /// <summary>
        /// Gets toggle the visibility of the project input section.
        /// </summary>
        public ICommand ToggleProjectInputCommand { get; }

        /// <summary>
        /// Gets toggle between full and compact modes.
        /// When invoked, this command flips the UseCompactMode flag on the settings and
        /// asynchronously persists the updated settings. Any UI bound to Settings.UseCompactMode
        /// will reactively update when the flag changes.
        /// </summary>
        public ICommand ToggleCompactModeCommand { get; }

        /// <summary>
        /// Initialize settings asynchronously. Should be called from Window.Loaded or similar event.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task InitializeSettingsAsync()
        {
            try
            {
                var loadedSettings = await this._settingsProvider.LoadAsync();

                // Ensure UI updates happen on the UI thread
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.Settings = loadedSettings;
                });

                this._sessionTracker.SetTrackingEnabled(this.Settings.WorkLoggingEnabled);
                if (this.IsRunning && this.Settings.WorkLoggingEnabled)
                {
                    await this._sessionTracker.StartAsync(this.ProjectTag);
                }
            }
            catch (Exception ex)
            {
                // Log error but continue with defaults
                this._logWriter.LogError("Failed to load settings.", ex);

                // TODO: Consider showing a notification to the user about failed settings load
            }
        }

        /// <summary>
        /// Reapply settings to the widget (used when settings change at runtime).
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task ReloadSettingsAsync()
        {
            await this.InitializeSettingsAsync();

            // Settings property change will trigger UI bindings
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this._disposed)
            {
                return;
            }

            if (this._themeNotifier != null)
            {
                this._themeNotifier.PropertyChanged -= this.Theme_PropertyChanged;
                this._themeNotifier = null;
            }

            if (this._settingsNotifier != null)
            {
                this._settingsNotifier.PropertyChanged -= this.Settings_PropertyChanged;
                this._settingsNotifier = null;
            }

            try
            {
                // Stop timer
                this._timerService.Stop();

                // No need to unsubscribe from Tick here, as we use lambda subscriptions in the constructor

                // Stop break reminder
                this._breakReminderService.OnTimerPaused();

                // If timer was running, try to flush entries
                if (this.IsRunning)
                {
                    try
                    {
                        // Synchronously collect and attempt to save entries
                        var entries = this._sessionTracker.CollectAndResetSegments();
                        if (entries.Count > 0)
                        {
                            // We can't await here, so we'll do our best effort
                            this._sessionRepository.SaveSessionAsync(entries).Wait(TimeSpan.FromSeconds(2));
                            this._logWriter.LogInformation($"Flushed {entries.Count} entries on dispose");
                        }
                    }
                    catch (Exception ex)
                    {
                        this._logWriter.LogError("Failed to flush entries on dispose.", ex);
                    }
                }
            }
            finally
            {
                this._disposed = true;
            }
        }

        // Call this method whenever WidgetScale changes
        private void UpdateResponsiveLayout()
        {
            var scale = this.Settings?.WidgetScale ?? 1.0;
            this.MainTimerFontSize = 36 * scale;
            this.CompactTimerFontSize = 25 * scale;
            this.MainTimerTextWidth = 175 * scale;
            this.CompactTimerTextWidth = 115 * scale;
            this.ProjectFontSize = 14 * scale;
            this.ButtonSize = 25 * (scale / 2);
            this.IconSize = 28 * (scale / 2);
            this.CompactButtonSize = this.ButtonSize * 0.7;
            this.CompactIconSize = this.IconSize * 0.7;
            this.ButtonPanelOrientation = scale >= 1.25 ? Orientation.Vertical : Orientation.Horizontal;
        }

        private void Toggle()
        {
            if (this._timerService.CurrentState == TimerState.Running)
            {
                this._timerService.Pause();
            }
            else
            {
                this._timerService.Start(this.ProjectTag);
            }
        }

        private void Reset()
        {
            this._timerService.Reset();
        }

        private void ToggleProjectInput()
        {
            this.IsProjectInputVisible = !this.IsProjectInputVisible;
        }

        private void OnTimerTick(TimeSpan elapsed)
        {
            this.Elapsed = elapsed;

            if (this._sessionTracker.HasCompletedEntries)
            {
                _ = this.PersistCompletedSegmentsAsync();
            }
        }

        private void OnTimerStateChanged(TimerState state)
        {
            this.IsRunning = state == TimerState.Running;
            this._logWriter.LogDebug($"Timer state changed to {state}");

            if (state == TimerState.Running)
            {
                this._breakReminderService.OnTimerStarted();
            }
            else
            {
                this._breakReminderService.OnTimerPaused();

                // Flush entries when paused or stopped
                this.FlushEntriesAsync($"state change to {state}").ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        this._logWriter.LogError("Error flushing entries.", t.Exception);
                    }
                });
            }
        }

        /// <summary>
        /// Flushes completed time entries to CSV log.
        /// Reloads settings to ensure we use the latest log directory.
        /// </summary>
        private async Task FlushEntriesAsync(string reason)
        {
            await this._persistenceSemaphore.WaitAsync();
            try
            {
                this._logWriter.LogDebug($"Attempting full session flush due to {reason}.");

                var entries = this._sessionTracker.CollectAndResetSegments();
                await this.PersistEntriesAsync(entries, reason);
            }
            catch (Exception ex)
            {
                this._logWriter.LogError("Failed to flush entries.", ex);

                // TODO: Consider showing a user notification about log write failure
            }
            finally
            {
                this._persistenceSemaphore.Release();
            }
        }

        private async Task PersistCompletedSegmentsAsync()
        {
            if (!await this._persistenceSemaphore.WaitAsync(0))
            {
                return;
            }

            try
            {
                var entries = this._sessionTracker.DrainCompletedSegments();
                await this.PersistEntriesAsync(entries, "completed window segment");
            }
            catch (Exception ex)
            {
                this._logWriter.LogError("Failed to persist completed segments.", ex);
            }
            finally
            {
                this._persistenceSemaphore.Release();
            }
        }

        private async Task PersistEntriesAsync(IReadOnlyList<TimeEntry> entries, string reason)
        {
            if (entries.Count == 0)
            {
                this._logWriter.LogDebug($"No time entries available for persistence during {reason}.");
                return;
            }

            Settings currentSettings;
            try
            {
                currentSettings = await this._settingsProvider.LoadAsync();
            }
            catch (Exception ex)
            {
                this._logWriter.LogError("Failed to reload settings, using cached.", ex);
                currentSettings = this._settings;
            }

            if (!currentSettings.WorkLoggingEnabled)
            {
                this._logWriter.LogDebug($"Skipping persistence because work logging is disabled during {reason}.");
                return;
            }

            this._logWriter.LogInformation($"Persisting {entries.Count} time entries due to {reason}.");
            await this._sessionRepository.SaveSessionAsync(entries);
            this._logWriter.LogInformation($"Successfully logged {entries.Count} time entries to {currentSettings.WorklogDirectory}");

            // Publish an EntriesLoggedEvent so the AppController (or other listeners) can react.
            try
            {
                this._eventBus?.Publish(new FocusTimer.Core.Models.EntriesLoggedEvent { Entries = entries });
            }
            catch
            {
                // best-effort
            }
        }

        /// <summary>
        /// Updates the project tag and notifies the session tracker.
        /// Safe to call even if tracking isn't active.
        /// </summary>
        private void OnProjectTagChanged()
        {
            try
            {
                this._sessionTracker.UpdateProjectTag(this._projectTag);
            }
            catch (Exception ex)
            {
                this._logWriter.LogError("Error updating project tag.", ex);
            }
        }

        private void SubscribeToThemeChanges(object? theme)
        {
            if (this._themeNotifier != null)
            {
                this._themeNotifier.PropertyChanged -= this.Theme_PropertyChanged;
                this._themeNotifier = null;
            }

            if (theme is System.ComponentModel.INotifyPropertyChanged notifier)
            {
                this._themeNotifier = notifier;
                this._themeNotifier.PropertyChanged += this.Theme_PropertyChanged;
            }
        }

        private void SubscribeToSettingsChanges(Settings? newSettings)
        {
            if (this._settingsNotifier != null)
            {
                this._settingsNotifier.PropertyChanged -= this.Settings_PropertyChanged;
                this._settingsNotifier = null;
            }

            if (newSettings is System.ComponentModel.INotifyPropertyChanged notifier)
            {
                this._settingsNotifier = notifier;
                this._settingsNotifier.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(this.Settings.WidgetScale))
                    {
                        this.UpdateResponsiveLayout();
                    }

                    this.Settings_PropertyChanged(s, e);
                };
            }

            // Initial call
            this.UpdateResponsiveLayout();
        }

        private void Settings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.Settings.WidgetOpacity))
            {
                this.RaisePropertyChanged(nameof(this.OverallOpacity));
                this.RaisePropertyChanged(nameof(this.EffectiveBackgroundOpacity));
                this.RaisePropertyChanged(nameof(this.EffectiveClockOpacity));
                this.RaisePropertyChanged(nameof(this.EffectiveControlsOpacity));
            }
            else if (e.PropertyName == nameof(this.Settings.WorkLoggingEnabled))
            {
                this._sessionTracker.SetTrackingEnabled(this.Settings.WorkLoggingEnabled);

                if (this.Settings.WorkLoggingEnabled && this.IsRunning)
                {
                    _ = this._sessionTracker.StartAsync(this.ProjectTag);
                }
            }
        }

        private void Theme_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this._logWriter.LogDebug($"[Theme_PropertyChanged] Property changed: {e.PropertyName}");

            if (e.PropertyName == nameof(FocusTimer.Core.Models.Theme.BackgroundOpacity))
            {
                this.RaisePropertyChanged(nameof(this.BackgroundOpacity));
                this.RaisePropertyChanged(nameof(this.EffectiveBackgroundOpacity));
                this.RaisePropertyChanged(nameof(this.BackgroundBrush));
            }
            else if (e.PropertyName == nameof(FocusTimer.Core.Models.Theme.WindowBackground))
            {
                this.RaisePropertyChanged(nameof(this.BackgroundBrush));
            }
            else if (e.PropertyName == nameof(FocusTimer.Core.Models.Theme.TimerOpacity))
            {
                this.RaisePropertyChanged(nameof(this.ClockOpacity));
                this.RaisePropertyChanged(nameof(this.EffectiveClockOpacity));
            }
            else if (e.PropertyName == nameof(FocusTimer.Core.Models.Theme.ButtonOpacity))
            {
                this.RaisePropertyChanged(nameof(this.ControlsOpacity));
                this.RaisePropertyChanged(nameof(this.EffectiveControlsOpacity));
            }
        }
    }
}
