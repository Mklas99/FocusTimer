using FocusTimer.App.Services;
using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;
using ReactiveUI;
using System.Reactive.Concurrency;

namespace FocusTimer.App.ViewModels;

/// <summary>
/// ViewModel for the timer widget window.
/// Handles timer logic, state management, and commands.
/// </summary>
public class TimerWidgetViewModel : ReactiveObject, IDisposable
{
    
    private System.ComponentModel.INotifyPropertyChanged? _themeNotifier;
    private System.ComponentModel.INotifyPropertyChanged? _settingsNotifier;

    /// <summary>
    /// Brush used for the widget background. Always fully opaque; visual transparency
    /// is controlled via EffectiveBackgroundOpacity on the background layer.
    /// </summary>
    public Avalonia.Media.IBrush BackgroundBrush
    {
        get
        {
            var colorStr = Settings?.Theme?.WindowBackground ?? "#FF000000";
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
    /// Base background opacity (0..1) proxied to the theme.
    /// </summary>
    public double BackgroundOpacity
    {
        get => Settings?.Theme?.BackgroundOpacity ?? 1.0;
        set
        {
            if (Settings?.Theme is null)
                return;

            var clamped = Math.Clamp(value, 0.0, 1.0);
            if (!clamped.Equals(Settings.Theme.BackgroundOpacity))
            {
                Settings.Theme.BackgroundOpacity = clamped;
                this.RaisePropertyChanged(nameof(BackgroundOpacity));
                this.RaisePropertyChanged(nameof(EffectiveBackgroundOpacity));
                this.RaisePropertyChanged(nameof(BackgroundBrush));
            }
        }
    }

    /// <summary>
    /// Base clock/text opacity (0..1) proxied to the theme.
    /// </summary>
    public double ClockOpacity
    {
        get => Settings?.Theme?.TimerOpacity ?? 1.0;
        set
        {
            if (Settings?.Theme is null)
                return;

            var clamped = Math.Clamp(value, 0.0, 1.0);
            if (!clamped.Equals(Settings.Theme.TimerOpacity))
            {
                Settings.Theme.TimerOpacity = clamped;
                this.RaisePropertyChanged(nameof(ClockOpacity));
                this.RaisePropertyChanged(nameof(EffectiveClockOpacity));
            }
        }
    }

    /// <summary>
    /// Base controls/buttons opacity (0..1) proxied to the theme.
    /// </summary>
    public double ControlsOpacity
    {
        get => Settings?.Theme?.ButtonOpacity ?? 1.0;
        set
        {
            if (Settings?.Theme is null)
                return;

            var clamped = Math.Clamp(value, 0.0, 1.0);
            if (!clamped.Equals(Settings.Theme.ButtonOpacity))
            {
                Settings.Theme.ButtonOpacity = clamped;
                this.RaisePropertyChanged(nameof(ControlsOpacity));
                this.RaisePropertyChanged(nameof(EffectiveControlsOpacity));
            }
        }
    }

    /// <summary>
    /// Overall opacity multiplier (0.2..1) applied to background, clock and controls.
    /// Backed by Settings.WidgetOpacity so it is persisted.
    /// </summary>
    public double OverallOpacity
    {
        get => Settings?.WidgetOpacity ?? 1.0;
        set
        {
            if (Settings is null)
                return;

            var clamped = Math.Clamp(value, 0.2, 1.0);
            if (!clamped.Equals(Settings.WidgetOpacity))
            {
                Settings.WidgetOpacity = clamped;
                this.RaisePropertyChanged(nameof(OverallOpacity));
                this.RaisePropertyChanged(nameof(EffectiveBackgroundOpacity));
                this.RaisePropertyChanged(nameof(EffectiveClockOpacity));
                this.RaisePropertyChanged(nameof(EffectiveControlsOpacity));
            }
        }
    }

    /// <summary>
    /// Effective opacities with the overall multiplier applied.
    /// </summary>
    public double EffectiveBackgroundOpacity => BackgroundOpacity * OverallOpacity;
    public double EffectiveClockOpacity => ClockOpacity * OverallOpacity;
    public double EffectiveControlsOpacity => ControlsOpacity * OverallOpacity;

private readonly ISettingsProvider _settingsProvider;
    private readonly ILogWriter _logWriter;
    private readonly SessionTracker _sessionTracker;
    private readonly BreakReminderService _breakReminderService;
    private readonly DispatcherTimer _timer;
    
    // Timer state using DateTime-based measurement to avoid drift
    private DateTime? _startTime;
    private TimeSpan _accumulatedElapsed;
    private TimeSpan _elapsed;
    private string _elapsedFormatted = "00:00:00";
    
    private bool _isRunning;
    private string? _projectTag;
    private Settings _settings;
    private bool _isProjectInputVisible = false;

    public TimerWidgetViewModel(
        ISettingsProvider settingsProvider,
        ILogWriter logWriter,
        SessionTracker sessionTracker,
        BreakReminderService breakReminderService)
    {
        // Defensive: Ensure ViewModel is constructed on the UI thread
        if (!Dispatcher.UIThread.CheckAccess())
            throw new InvalidOperationException("TimerWidgetViewModel must be constructed on the Avalonia UI thread.");

        _settingsProvider = settingsProvider;
        _logWriter = logWriter;
        _sessionTracker = sessionTracker;
        _breakReminderService = breakReminderService;
        _settings = new Settings(); // Safe defaults with validation

        SubscribeToThemeChanges(_settings.Theme);
        SubscribeToSettingsChanges(_settings);
        
        // Initialize timer (ticks every second)
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += OnTimerTick;

        // Initialize commands with explicit UI thread scheduler
        ToggleCommand = ReactiveCommand.Create(Toggle, outputScheduler: RxApp.MainThreadScheduler);
        ResetCommand = ReactiveCommand.Create(Reset, outputScheduler: RxApp.MainThreadScheduler);
        ToggleProjectInputCommand = ReactiveCommand.Create(ToggleProjectInput, outputScheduler: RxApp.MainThreadScheduler);

        // Toggle full/compact mode. We persist the setting asynchronously to avoid blocking the UI.
        ToggleCompactModeCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            // Flip the compact mode flag
            Settings.UseCompactMode = !Settings.UseCompactMode;

            try
            {
                await _settingsProvider.SaveAsync(Settings);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings after toggling compact mode: {ex.Message}");
            }
        }, outputScheduler: RxApp.MainThreadScheduler);
    }

    #region Properties

    /// <summary>
    /// Current elapsed time.
    /// </summary>
    public TimeSpan Elapsed
    {
        get => _elapsed;
        private set
        {
            var oldValue = _elapsed;
            this.RaiseAndSetIfChanged(ref _elapsed, value);
            
            // Update formatted string when elapsed changes
            if (oldValue != _elapsed)
            {
                ElapsedFormatted = $"{(int)_elapsed.TotalHours:D2}:{_elapsed.Minutes:D2}:{_elapsed.Seconds:D2}";
            }
        }
    }

    /// <summary>
    /// Formatted elapsed time string (HH:MM:SS).
    /// Cached and only updated when Elapsed changes.
    /// </summary>
    public string ElapsedFormatted
    {
        get => _elapsedFormatted;
        private set => this.RaiseAndSetIfChanged(ref _elapsedFormatted, value);
    }

    /// <summary>
    /// Whether the timer is currently running.
    /// </summary>
    public bool IsRunning
    {
        get => _isRunning;
        private set => this.RaiseAndSetIfChanged(ref _isRunning, value);
    }

    /// <summary>
    /// Optional project/task tag for this session.
    /// </summary>
    public string? ProjectTag
    {
        get => _projectTag;
        set
        {
            if (_projectTag != value)
            {
                _projectTag = value;
                this.RaisePropertyChanged(nameof(ProjectTag));
                OnProjectTagChanged();
            }
        }
    }

    /// <summary>
    /// Current application settings.
    /// </summary>
    public Settings Settings
    {
        get => _settings;
        private set
        {
            if (_settings != value)
            {
                this.RaiseAndSetIfChanged(ref _settings, value);
                SubscribeToThemeChanges(_settings.Theme);
                SubscribeToSettingsChanges(_settings);
                this.RaisePropertyChanged(nameof(BackgroundBrush));
                this.RaisePropertyChanged(nameof(BackgroundOpacity));
                this.RaisePropertyChanged(nameof(ClockOpacity));
                this.RaisePropertyChanged(nameof(ControlsOpacity));
                this.RaisePropertyChanged(nameof(OverallOpacity));
                this.RaisePropertyChanged(nameof(EffectiveBackgroundOpacity));
                this.RaisePropertyChanged(nameof(EffectiveClockOpacity));
                this.RaisePropertyChanged(nameof(EffectiveControlsOpacity));
            }
        }
    }

    /// <summary>
    /// Controls visibility of the project input section.
    /// </summary>
    public bool IsProjectInputVisible
    {
        get => _isProjectInputVisible;
        set => this.RaiseAndSetIfChanged(ref _isProjectInputVisible, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Toggle between play and pause states.
    /// </summary>
    public ICommand ToggleCommand { get; }

    /// <summary>
    /// Reset the timer to zero and stop it.
    /// </summary>
    public ICommand ResetCommand { get; }

    /// <summary>
    /// Toggle the visibility of the project input section.
    /// </summary>
    public ICommand ToggleProjectInputCommand { get; }

    /// <summary>
    /// Toggle between full and compact modes.
    /// When invoked, this command flips the UseCompactMode flag on the settings and
    /// asynchronously persists the updated settings. Any UI bound to Settings.UseCompactMode
    /// will reactively update when the flag changes.
    /// </summary>
    public ICommand ToggleCompactModeCommand { get; }

    #endregion

    #region Command Implementations

    private void Toggle()
    {
        // Use proper async handling instead of fire-and-forget
        if (IsRunning)
        {
            PauseAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    System.Diagnostics.Debug.WriteLine($"Error pausing timer: {t.Exception?.GetBaseException().Message}");
                }
            });
        }
        else
        {
            StartAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    System.Diagnostics.Debug.WriteLine($"Error starting timer: {t.Exception?.GetBaseException().Message}");
                }
            });
        }
    }

    private async Task StartAsync()
    {
        if (!IsRunning)
        {
            IsRunning = true;
            _startTime ??= DateTime.Now; // Only set if not already set (resume case)
            
            try
            {
                // Start session tracking
                await _sessionTracker.StartAsync(_projectTag);
                
                // Start break reminder tracking
                _breakReminderService.OnTimerStarted();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start session tracking: {ex.Message}");
                // Continue anyway - timer still works even if tracking fails
            }
            
            _timer.Start();
        }
    }

    private async Task PauseAsync()
    {
        if (IsRunning)
        {
            IsRunning = false;
            _timer.Stop();

            // Accumulate elapsed time up to now
            if (_startTime.HasValue)
            {
                _accumulatedElapsed += DateTime.Now - _startTime.Value;
                _startTime = null;
            }

            // Update display with final accumulated time
            Elapsed = _accumulatedElapsed;

            // Cancel break reminder
            _breakReminderService.OnTimerPaused();

            // Collect and log entries
            await FlushEntriesAsync();
        }
    }

    private void Reset()
    {
        ResetAsync().ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                System.Diagnostics.Debug.WriteLine($"Error resetting timer: {t.Exception?.GetBaseException().Message}");
            }
        });
    }

    private async Task ResetAsync()
    {
        await PauseAsync(); // Ensure timer is stopped and entries are flushed
        _accumulatedElapsed = TimeSpan.Zero;
        Elapsed = TimeSpan.Zero;
    }

    private void ToggleProjectInput()
    {
        IsProjectInputVisible = !IsProjectInputVisible;
    }

    #endregion

    #region Timer Logic

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (IsRunning && _startTime.HasValue)
        {
            // Calculate actual elapsed time from DateTime measurement
            Elapsed = _accumulatedElapsed + (DateTime.Now - _startTime.Value);
            
            // Check for active window changes (fire and forget, but log errors)
            _sessionTracker.OnTimerTickAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    System.Diagnostics.Debug.WriteLine($"Error tracking window: {t.Exception?.GetBaseException().Message}");
                }
            }, TaskScheduler.Default);
        }
    }

    #endregion

    #region Session Tracking & Logging

    /// <summary>
    /// Flushes completed time entries to CSV log.
    /// Reloads settings to ensure we use the latest log directory.
    /// </summary>
    private async Task FlushEntriesAsync()
    {
        try
        {
            // Collect entries first
            var entries = _sessionTracker.CollectAndResetSegments();
            
            if (entries.Count > 0)
            {
                // Reload settings to ensure we have the latest configuration
                Settings currentSettings;
                try
                {
                    currentSettings = await _settingsProvider.LoadAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to reload settings, using cached: {ex.Message}");
                    currentSettings = _settings; // Fall back to cached settings
                }

                // Write entries
                await _logWriter.WriteEntriesAsync(entries, currentSettings);
                System.Diagnostics.Debug.WriteLine($"Successfully logged {entries.Count} time entries to {currentSettings.LogDirectory}");
                // Notify AppController for tray update
                var appController = Program.Services.GetService(typeof(AppController)) as AppController;
                appController?.OnEntriesLogged(entries);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No time entries to log");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to flush entries: {ex.Message}");
            // TODO: Consider showing a user notification about log write failure
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
            _sessionTracker.UpdateProjectTag(_projectTag);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating project tag: {ex.Message}");
        }
    }

    #endregion

    #region Settings Management

    /// <summary>
    /// Initialize settings asynchronously. Should be called from Window.Loaded or similar event.
    /// </summary>
    public async Task InitializeSettingsAsync()
    {
        try
        {
            var loadedSettings = await _settingsProvider.LoadAsync();
            
            // Ensure UI updates happen on the UI thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Settings = loadedSettings;
            });
        }
        catch (Exception ex)
        {
            // Log error but continue with defaults
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex}");
            // TODO: Consider showing a notification to the user about failed settings load
        }
    }

    /// <summary>
    /// Reapply settings to the widget (used when settings change at runtime).
    /// </summary>
    public async Task ReloadSettingsAsync()
    {
        await InitializeSettingsAsync();
        // Settings property change will trigger UI bindings
    }

    #endregion

    #region Cleanup
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_themeNotifier != null)
        {
            _themeNotifier.PropertyChanged -= Theme_PropertyChanged;
            _themeNotifier = null;
        }

        if (_settingsNotifier != null)
        {
            _settingsNotifier.PropertyChanged -= Settings_PropertyChanged;
            _settingsNotifier = null;
        }

        try
        {
            // Stop timer
            _timer.Stop();
            _timer.Tick -= OnTimerTick;

            // Stop break reminder
            _breakReminderService.OnTimerPaused();

            // If timer was running, try to flush entries
            if (IsRunning)
            {
                try
                {
                    // Synchronously collect and attempt to save entries
                    var entries = _sessionTracker.CollectAndResetSegments();
                    if (entries.Count > 0)
                    {
                        // We can't await here, so we'll do our best effort
                        _logWriter.WriteEntriesAsync(entries, _settings).Wait(TimeSpan.FromSeconds(2));
                        System.Diagnostics.Debug.WriteLine($"Flushed {entries.Count} entries on dispose");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to flush entries on dispose: {ex.Message}");
                }
            }
        }
        finally
        {
            _disposed = true;
        }
    }

    private void SubscribeToThemeChanges(object? theme)
    {
        if (_themeNotifier != null)
        {
            _themeNotifier.PropertyChanged -= Theme_PropertyChanged;
            _themeNotifier = null;
        }
        if (theme is System.ComponentModel.INotifyPropertyChanged notifier)
        {
            _themeNotifier = notifier;
            _themeNotifier.PropertyChanged += Theme_PropertyChanged;
        }
    }

    private void SubscribeToSettingsChanges(Settings? newSettings)
    {
        if (_settingsNotifier != null)
        {
            _settingsNotifier.PropertyChanged -= Settings_PropertyChanged;
            _settingsNotifier = null;
        }

        if (newSettings is System.ComponentModel.INotifyPropertyChanged notifier)
        {
            _settingsNotifier = notifier;
            _settingsNotifier.PropertyChanged += Settings_PropertyChanged;
        }
    }

    private void Settings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.WidgetOpacity))
        {
            this.RaisePropertyChanged(nameof(OverallOpacity));
            this.RaisePropertyChanged(nameof(EffectiveBackgroundOpacity));
            this.RaisePropertyChanged(nameof(EffectiveClockOpacity));
            this.RaisePropertyChanged(nameof(EffectiveControlsOpacity));
        }
    }

    private void Theme_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[Theme_PropertyChanged] Property changed: {e.PropertyName}");

        if (e.PropertyName == nameof(FocusTimer.Core.Models.Theme.BackgroundOpacity))
        {
            this.RaisePropertyChanged(nameof(BackgroundOpacity));
            this.RaisePropertyChanged(nameof(EffectiveBackgroundOpacity));
            this.RaisePropertyChanged(nameof(BackgroundBrush));
        }
        else if (e.PropertyName == nameof(FocusTimer.Core.Models.Theme.WindowBackground))
        {
            this.RaisePropertyChanged(nameof(BackgroundBrush));
        }
        else if (e.PropertyName == nameof(FocusTimer.Core.Models.Theme.TimerOpacity))
        {
            this.RaisePropertyChanged(nameof(ClockOpacity));
            this.RaisePropertyChanged(nameof(EffectiveClockOpacity));
        }
        else if (e.PropertyName == nameof(FocusTimer.Core.Models.Theme.ButtonOpacity))
        {
            this.RaisePropertyChanged(nameof(ControlsOpacity));
            this.RaisePropertyChanged(nameof(EffectiveControlsOpacity));
        }
    }

    #endregion
}
