namespace FocusTimer.App.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Avalonia.Controls;
    using Avalonia.Media;
    using Avalonia.Platform.Storage;
    using FocusTimer.App.Services;
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;
    using ReactiveUI;

    /// <summary>
    /// ViewModel for the Settings window.
    /// </summary>
    public class SettingsWindowViewModel : ReactiveObject
    {
        /// <summary>
        /// The position of the Summary tab; it must match the tab order in SettingsWindow.axaml.
        /// </summary>
        public const int SummaryTabIndex = 2;

        private const double OpacityTolerance = 0.0001;
        private readonly ISettingsProvider _settingsProvider;
        private readonly IAutoStartService _autoStartService;
        private readonly IThemeService _themeService;
        private readonly ThemeManager _themeManager;
        private readonly IAppLogger _logger;
        private readonly SettingsCommitCoordinator _commitCoordinator;
        private Func<Settings, Task> _activateRuntime = _ => Task.CompletedTask;
        private Func<Task>? _recoveryCompleted;
        private Action<Settings>? _previewAppearance;
        private Settings? _attachedSettings;
        private Theme? _attachedTheme;
        private Settings _settings;
        private Settings _lastAppliedSettings = new();
        private bool _observedAutoStartEnabled;
        private string _selectedThemeName;
        private Theme _lastAppliedTheme = new();
        private bool _appearancePreviewChanged;
        private int _versionClickCount;
        private decimal? _activityPollingIntervalInput = 10;
        private int _selectedTabIndex;
        private bool _isSettingsLoaded;
        private string _loadError = string.Empty;
        private string _commitError = string.Empty;
        private bool _isCommitting;
        private bool _recoveryRequired;
        private bool _recoveryCloseWarned;
        private bool _isDisposed;
        private int _editGeneration;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsWindowViewModel"/> class.
        /// </summary>
        /// <param name="settingsProvider">The settings provider service.</param>
        /// <param name="autoStartService">The auto-start service.</param>
        /// <param name="themeService">The theme service.</param>
        /// <param name="themeManager">The theme manager.</param>
        /// <param name="logWriter">The application logger.</param>
        /// <param name="worklogSummary">The view model of the Summary tab.</param>
        public SettingsWindowViewModel(
            ISettingsProvider settingsProvider,
            IAutoStartService autoStartService,
            IThemeService themeService,
            ThemeManager themeManager,
            IAppLogger logWriter,
            WorklogSummaryViewModel worklogSummary)
        {
            this.WorklogSummary = worklogSummary;
            this._settingsProvider = settingsProvider;
            this._autoStartService = autoStartService;
            this._themeService = themeService;
            this._themeManager = themeManager;
            this._themeManager.ActualWidgetTransparencyChanged += this.OnActualWidgetTransparencyChanged;
            this._logger = logWriter;
            this._commitCoordinator = new SettingsCommitCoordinator(
                settingsProvider, autoStartService, settings => this._activateRuntime(settings));
            this._settings = new Settings();
            this._selectedThemeName = "Dark";
            this.ChangelogContent = this.LoadChangelogContent();
            this.AttachSettings(this._settings);

            // Initialize commands
            this.ApplyCommand = ReactiveCommand.CreateFromTask(this.ApplyAsync);
            this.OkCommand = ReactiveCommand.CreateFromTask(this.OkAsync);
            this.CancelCommand = ReactiveCommand.Create<Window>(this.Cancel);
            this.BrowseWorklogDirectoryCommand = ReactiveCommand.CreateFromTask<Window>(this.BrowseWorklogDirectoryAsync);
            this.ImportThemeCommand = ReactiveCommand.CreateFromTask<Window>(this.ImportThemeAsync);
            this.ExportThemeCommand = ReactiveCommand.CreateFromTask<Window>(this.ExportThemeAsync);
            this.ResetThemeCommand = ReactiveCommand.Create(this.ResetTheme);
            this.OpenRepositoryCommand = ReactiveCommand.Create(this.OpenRepository);
            this.VersionInfoClickedCommand = ReactiveCommand.Create(this.OnVersionInfoClicked);
            this.RetryLoadCommand = ReactiveCommand.CreateFromTask(this.RetryLoadAsync);
            this.RetryRecoveryCommand = ReactiveCommand.CreateFromTask(this.RetryRecoveryAsync);

            // Load settings
            _ = this.LoadSettingsAsync();
        }

        /// <summary>
        /// Gets the view model of the Summary tab.
        /// </summary>
        public WorklogSummaryViewModel WorklogSummary { get; }

        /// <summary>
        /// Gets or sets the selected tab. Selecting the Summary tab reloads its breakdown.
        /// </summary>
        public int SelectedTabIndex
        {
            get => this._selectedTabIndex;
            set
            {
                if (this._selectedTabIndex == value)
                {
                    return;
                }

                this.RaiseAndSetIfChanged(ref this._selectedTabIndex, value);
                if (value == SummaryTabIndex)
                {
                    _ = this.WorklogSummary.RefreshAsync();
                }
            }
        }

        /// <summary>
        /// Gets the command for applying settings.
        /// </summary>
        public ICommand ApplyCommand { get; }

        /// <summary>
        /// Gets the command for confirming settings changes.
        /// </summary>
        public ICommand OkCommand { get; }

        /// <summary>
        /// Gets the command for canceling settings changes.
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>Gets the command to retry loading settings after a failure.</summary>
        public ICommand RetryLoadCommand { get; }

        /// <summary>Gets the command to retry an unfinished settings recovery.</summary>
        public ICommand RetryRecoveryCommand { get; }

        /// <summary>Gets a value indicating whether a settings commit is running.</summary>
        public bool IsCommitting
        {
            get => this._isCommitting;
            private set
            {
                this.RaiseAndSetIfChanged(ref this._isCommitting, value);
                this.RaisePropertyChanged(nameof(this.CanCommit));
                this.RaisePropertyChanged(nameof(this.CanClose));
                this.RaisePropertyChanged(nameof(this.CanEdit));
                this.RaisePropertyChanged(nameof(this.IsDraftVisible));
                this.RaisePropertyChanged(nameof(this.SavingStatus));
            }
        }

        /// <summary>Gets a value indicating whether a failed commit needs recovery.</summary>
        public bool RecoveryRequired
        {
            get => this._recoveryRequired;
            private set
            {
                this.RaiseAndSetIfChanged(ref this._recoveryRequired, value);
                this.RaisePropertyChanged(nameof(this.CanCommit));
            }
        }

        /// <summary>Gets a value indicating whether Apply and OK are available.</summary>
        public bool CanCommit => this.IsSettingsLoaded && !this.IsCommitting && !this.RecoveryRequired && !this._isDisposed;

        /// <summary>Gets a value indicating whether draft fields can be edited.</summary>
        public bool CanEdit => this.IsSettingsLoaded && !this.IsCommitting && !this._isDisposed;

        /// <summary>Gets a value indicating whether the draft can be displayed with its normal visual states.</summary>
        public bool IsDraftVisible => this.IsSettingsLoaded && !this._isDisposed;

        /// <summary>Gets the reserved footer save status.</summary>
        public string SavingStatus => this.IsCommitting ? "Saving..." : string.Empty;

        /// <summary>Gets a value indicating whether Cancel can close the window.</summary>
        public bool CanClose => !this.IsCommitting;

        /// <summary>Gets a visible commit or validation failure.</summary>
        public string CommitError
        {
            get => this._commitError;
            private set
            {
                this.RaiseAndSetIfChanged(ref this._commitError, value);
                this.RaisePropertyChanged(nameof(this.HasCommitError));
            }
        }

        /// <summary>Gets a value indicating whether a commit failure should be shown.</summary>
        public bool HasCommitError => !string.IsNullOrEmpty(this.CommitError);

        /// <summary>Gets a value indicating whether the settings draft is available for editing.</summary>
        public bool IsSettingsLoaded
        {
            get => this._isSettingsLoaded;
            private set
            {
                this.RaiseAndSetIfChanged(ref this._isSettingsLoaded, value);
                this.RaisePropertyChanged(nameof(this.CanCommit));
                this.RaisePropertyChanged(nameof(this.CanEdit));
                this.RaisePropertyChanged(nameof(this.IsDraftVisible));
                this.RaisePropertyChanged(nameof(this.SavingStatus));
            }
        }

        /// <summary>Gets the visible settings load error.</summary>
        public string LoadError
        {
            get => this._loadError;
            private set
            {
                this.RaiseAndSetIfChanged(ref this._loadError, value);
                this.RaisePropertyChanged(nameof(this.HasLoadError));
            }
        }

        /// <summary>Gets a value indicating whether the load error should be shown.</summary>
        public bool HasLoadError => !string.IsNullOrEmpty(this.LoadError);

        /// <summary>Gets a value indicating whether operating-system start-on-login registration is present.</summary>
        public bool ObservedAutoStartEnabled => this._observedAutoStartEnabled;

        /// <summary>Gets the warning shown when saved start-on-login differs from Windows registration.</summary>
        public string AutoStartDriftWarning => this.IsSettingsLoaded &&
            this._lastAppliedSettings.AutoStartOnLogin != this._observedAutoStartEnabled
                ? "Saved start-on-login differs from Windows registration. Apply or OK will reconcile registration to the value shown in the toggle."
                : string.Empty;

        /// <summary>Gets a value indicating whether a registration mismatch warning should be shown.</summary>
        public bool HasAutoStartDrift => !string.IsNullOrEmpty(this.AutoStartDriftWarning);

        /// <summary>
        /// Gets the command for browsing the worklog directory.
        /// </summary>
        public ICommand BrowseWorklogDirectoryCommand { get; }

        /// <summary>
        /// Gets the command for importing a theme.
        /// </summary>
        public ICommand ImportThemeCommand { get; }

        /// <summary>
        /// Gets the command for exporting the current theme.
        /// </summary>
        public ICommand ExportThemeCommand { get; }

        /// <summary>
        /// Gets the command for resetting the theme to default.
        /// </summary>
        public ICommand ResetThemeCommand { get; }

        /// <summary>
        /// Gets the command for opening the repository URL.
        /// </summary>
        public ICommand OpenRepositoryCommand { get; }

        /// <summary>
        /// Gets the command used to unlock developer mode.
        /// </summary>
        public ICommand VersionInfoClickedCommand { get; }

        /// <summary>
        /// Gets or sets the settings being edited.
        /// </summary>
        public Settings Settings
        {
            get => this._settings;
            set
            {
                if (ReferenceEquals(this._settings, value))
                {
                    return;
                }

                this.DetachSettings(this._settings);
                this.RaiseAndSetIfChanged(ref this._settings, value);
                this.AttachSettings(value);
                this.RaisePropertyChanged(nameof(this.PlayPauseColor));
                this.ActivityPollingIntervalInput = value.ActivityPollingIntervalSeconds;
                this.RaisePropertyChanged(nameof(this.IsDeveloperModeVisible));
                this.RaisePropertyChanged(nameof(this.SelectedDeveloperLogLevel));
            }
        }

        /// <summary>
        /// Gets list of available theme names for the ComboBox.
        /// </summary>
        public List<string> AvailableThemes => [.. this._themeService.BuiltInThemes.Select(t => t.ThemeName)];

        /// <summary>Gets or sets the explicit or inherited normal Play/Pause color.</summary>
        public string PlayPauseColor
        {
            get => this.Settings.Theme.PlayPauseColor ?? this.Settings.Theme.ButtonNormal;
            set => this.SetThemeColor(nameof(Theme.PlayPauseColor), value);
        }

        /// <summary>
        /// Gets available developer log levels.
        /// </summary>
        public List<string> AvailableDeveloperLogLevels { get; } = ["Verbose", "Debug", "Information", "Warning", "Error"];

        /// <summary>Gets or sets the interval draft, validated before saving.</summary>
        public decimal? ActivityPollingIntervalInput
        {
            get => this._activityPollingIntervalInput;
            set
            {
                this.RaiseAndSetIfChanged(ref this._activityPollingIntervalInput, value);
                this.RaisePropertyChanged(nameof(this.ActivityPollingIntervalError));
                if (!this.RecoveryRequired)
                {
                    this.CommitError = string.Empty;
                }
            }
        }

        /// <summary>Gets validation feedback for the interval draft.</summary>
        public string ActivityPollingIntervalError => this.ActivityPollingIntervalInput is decimal seconds &&
            seconds >= 1 && seconds <= 60 && seconds == decimal.Truncate(seconds)
                ? string.Empty : "Enter a whole number from 1 to 60 seconds.";

        /// <summary>Gets a value indicating whether the most recent Apply/OK saved successfully.</summary>
        public bool LastApplySucceeded { get; private set; }

        /// <summary>
        /// Gets application version shown in the About tab.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "Instance property required for XAML data binding.")]
        public string AppVersion
        {
            get
            {
                var assembly = Assembly.GetEntryAssembly();
                string? informationalVersion = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                return !string.IsNullOrWhiteSpace(informationalVersion)
                    ? informationalVersion.Split('+')[0]
                    : assembly?.GetName().Version?.ToString() ?? "1.0.0";
            }
        }

        /// <summary>
        /// Gets application author shown in the About tab.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "Instance property required for XAML data binding.")]
        public string AppAuthor => Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "Mklas99";

        /// <summary>
        /// Gets a short application description shown in the About tab.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "Instance property required for XAML data binding.")]
        public string AppInformation => Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? "FocusTimer is a desktop focus timer with break reminders, work logging, and customizable appearance.";

        /// <summary>
        /// Gets repository URL shown in the About tab.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "Instance property required for XAML data binding.")]
        public string RepositoryUrl => "https://github.com/Mklas99/FocusTimer";

        /// <summary>
        /// Gets changelog content shown in the About tab.
        /// </summary>
        public string ChangelogContent { get; }

        /// <summary>
        /// Gets a value indicating whether developer options are visible.
        /// </summary>
        public bool IsDeveloperModeVisible => this.Settings.DeveloperModeEnabled;

        /// <summary>
        /// Gets or sets currently selected theme name in the ComboBox.
        /// </summary>
        public string SelectedThemeName
        {
            get => this._selectedThemeName;
            set
            {
                if (!this.CanEdit || string.IsNullOrEmpty(value) || string.Equals(this._selectedThemeName, value, StringComparison.Ordinal))
                {
                    return;
                }

                this.RaiseAndSetIfChanged(ref this._selectedThemeName, value);
                this.LoadThemeByName(value);
            }
        }

        /// <summary>
        /// Gets or sets selected developer log level.
        /// </summary>
        public string SelectedDeveloperLogLevel
        {
            get => this.Settings.DeveloperLogLevel;
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && this.Settings.DeveloperLogLevel != value)
                {
                    this.Settings.DeveloperLogLevel = value;
                    this.RaisePropertyChanged(nameof(this.SelectedDeveloperLogLevel));
                }
            }
        }

        /// <summary>
        /// Gets or sets the background material opacity in percent (0..100).
        /// </summary>
        public double BackgroundOpacityPercent
        {
            get => ToPercent(this.Settings.Theme.BackgroundOpacity);
            set
            {
                double normalized = ToNormalized(value);
                if (Math.Abs(normalized - this.Settings.Theme.BackgroundOpacity) > OpacityTolerance)
                {
                    this.Settings.Theme.BackgroundOpacity = normalized;
                    this.RaisePropertyChanged(nameof(this.BackgroundOpacityPercent));
                    this.RaisePropertyChanged(nameof(this.NormalizedBackgroundOpacity));
                    this.RaisePropertyChanged(nameof(this.OpacityDiagnosticsSummary));
                }
            }
        }

        /// <summary>
        /// Gets the available widget backdrop choices.
        /// </summary>
        public IReadOnlyList<string> AvailableBlurModes { get; } =
            [WidgetBlurModes.Off, WidgetBlurModes.Solid];

        /// <summary>Gets a value indicating whether tint opacity affects the selected backdrop.</summary>
        public bool IsBackgroundTintOpacityAvailable => this.SelectedBlurMode != WidgetBlurModes.Solid;

        /// <summary>
        /// Gets or sets the selected widget backdrop blur choice.
        /// </summary>
        public string SelectedBlurMode
        {
            get => this.Settings.Theme.WidgetBlurMode;
            set
            {
                if (WidgetBlurModes.IsValid(value) && this.Settings.Theme.WidgetBlurMode != value)
                {
                    this.Settings.Theme.WidgetBlurMode = value;
                    this.RaisePropertyChanged(nameof(this.SelectedBlurMode));
                }
            }
        }

        /// <summary>
        /// Gets or sets the clock text opacity in percent (0..100).
        /// </summary>
        public double ClockOpacityPercent
        {
            get => ToPercent(this.Settings.Theme.TimerOpacity);
            set
            {
                double normalized = ToNormalized(value);
                if (Math.Abs(normalized - this.Settings.Theme.TimerOpacity) > OpacityTolerance)
                {
                    this.Settings.Theme.TimerOpacity = normalized;
                    this.RaisePropertyChanged(nameof(this.ClockOpacityPercent));
                    this.RaisePropertyChanged(nameof(this.NormalizedClockOpacity));
                    this.RaisePropertyChanged(nameof(this.OpacityDiagnosticsSummary));
                }
            }
        }

        /// <summary>
        /// Gets or sets the controls opacity in percent (0..100).
        /// </summary>
        public double ButtonsOpacityPercent
        {
            get => ToPercent(this.Settings.Theme.ButtonOpacity);
            set
            {
                double normalized = ToNormalized(value);
                if (Math.Abs(normalized - this.Settings.Theme.ButtonOpacity) > OpacityTolerance)
                {
                    this.Settings.Theme.ButtonOpacity = normalized;
                    this.RaisePropertyChanged(nameof(this.ButtonsOpacityPercent));
                    this.RaisePropertyChanged(nameof(this.NormalizedButtonsOpacity));
                    this.RaisePropertyChanged(nameof(this.OpacityDiagnosticsSummary));
                }
            }
        }

        /// <summary>
        /// Gets or sets the overall widget fade in percent (0..100).
        /// </summary>
        public double OverallFadePercent
        {
            get => ToPercent(this.Settings.WidgetOpacity);
            set
            {
                double normalized = ToNormalized(value);
                if (Math.Abs(normalized - this.Settings.WidgetOpacity) > OpacityTolerance)
                {
                    this.Settings.WidgetOpacity = normalized;
                    this.RaisePropertyChanged(nameof(this.OverallFadePercent));
                    this.RaisePropertyChanged(nameof(this.NormalizedOverallFade));
                    this.RaisePropertyChanged(nameof(this.OpacityDiagnosticsSummary));
                }
            }
        }

        /// <summary>
        /// Gets normalized background opacity (0..1).
        /// </summary>
        public double NormalizedBackgroundOpacity => this.Settings.Theme.BackgroundOpacity;

        /// <summary>
        /// Gets normalized clock opacity (0..1).
        /// </summary>
        public double NormalizedClockOpacity => this.Settings.Theme.TimerOpacity;

        /// <summary>
        /// Gets normalized controls opacity (0..1).
        /// </summary>
        public double NormalizedButtonsOpacity => this.Settings.Theme.ButtonOpacity;

        /// <summary>
        /// Gets normalized overall fade (0..1).
        /// </summary>
        public double NormalizedOverallFade => this.Settings.WidgetOpacity;

        /// <summary>
        /// Gets transparency mode diagnostics shown in Appearance tab.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "Instance property required for XAML data binding.")]
        public string TransparencyDiagnostics =>
            $"Requested: {this.SelectedBlurMode} | Active: {this._themeManager.ActualWidgetTransparency}" +
            $" | Surface: {this.GetWidgetSurfaceDescription()}";

        /// <summary>
        /// Gets acrylic diagnostics shown in Appearance tab.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "Instance property required for XAML data binding.")]
        public string AcrylicDiagnostics => "Tint opacity applies to Off. Solid always uses a fully opaque background. Blur is unavailable for now.";

        /// <summary>
        /// Gets a single-line summary of effective normalized opacities.
        /// </summary>
        public string OpacityDiagnosticsSummary =>
            $"BG={(this.SelectedBlurMode == WidgetBlurModes.Solid ? "solid" : this.NormalizedBackgroundOpacity.ToString("F2"))} | Clock={this.NormalizedClockOpacity:F2} | Controls={this.NormalizedButtonsOpacity:F2} | Overall={this.NormalizedOverallFade:F2}";

        /// <summary>Gets the generation used to reject appearance results from before a commit.</summary>
        internal int AppearanceEditGeneration => this._editGeneration;

        /// <summary>
        /// Gets the current value of a theme color property.
        /// </summary>
        /// <param name="propertyName">The theme property name.</param>
        /// <returns>The current color value, or an empty string if the property is unknown.</returns>
        public string GetThemeColor(string propertyName)
        {
            if (propertyName == nameof(Theme.PlayPauseColor))
            {
                return this.PlayPauseColor;
            }

            PropertyInfo? property = typeof(Theme).GetProperty(propertyName);
            if (property?.PropertyType != typeof(string))
            {
                this._logger.LogWarning($"Unknown theme color property requested: {propertyName}");
                return string.Empty;
            }

            return property.GetValue(this.Settings.Theme) as string ?? string.Empty;
        }

        /// <summary>
        /// Sets a theme color property and applies it immediately.
        /// </summary>
        /// <param name="propertyName">The theme property name.</param>
        /// <param name="colorValue">The updated color value.</param>
        public void SetThemeColor(string propertyName, string colorValue)
        {
            if (!this.CanEdit)
            {
                return;
            }

            PropertyInfo? property = typeof(Theme).GetProperty(propertyName);
            if (property?.PropertyType != typeof(string))
            {
                this._logger.LogWarning($"Unknown theme color property update attempted: {propertyName}");
                return;
            }

            property.SetValue(this.Settings.Theme, colorValue);
        }

        /// <summary>
        /// Registers a click on the version text to unlock developer options.
        /// </summary>
        public void RegisterVersionInfoClick()
        {
            this.OnVersionInfoClicked();
        }

        /// <summary>
        /// Restores the last applied theme after an unapplied appearance preview.
        /// </summary>
        public void RestoreAppearancePreview()
        {
            if (!this.IsSettingsLoaded || this.IsCommitting)
            {
                return;
            }

            if (this._appearancePreviewChanged)
            {
                this._themeManager.ApplyTheme(this._lastAppliedTheme);
                this._appearancePreviewChanged = false;
            }

            Settings restoredSettings = this._lastAppliedSettings.Clone();

            // The loaded theme may have been resolved from a preset when the saved names differed.
            restoredSettings.Theme = this._lastAppliedTheme.Clone();
            this.Settings = restoredSettings;
            this._selectedThemeName = this.Settings.ActiveThemeName;
            this.RaisePropertyChanged(nameof(this.SelectedThemeName));
            this._previewAppearance?.Invoke(restoredSettings);
        }

        /// <summary>Toggles the draft's compact mode when appearance editing is available.</summary>
        public void ToggleCompactModePreview()
        {
            if (this.CanEdit && !this.RecoveryRequired)
            {
                this.Settings.UseCompactMode = !this.Settings.UseCompactMode;
            }
        }

        /// <summary>Connects the widget's temporary appearance to the settings draft.</summary>
        /// <param name="preview">Displays appearance without saving or activating nonappearance settings.</param>
        public void SetAppearancePreview(Action<Settings> preview)
        {
            this._previewAppearance = preview ?? throw new ArgumentNullException(nameof(preview));
            if (this.CanEdit)
            {
                this.ApplyThemeChanges();
            }
        }

        /// <summary>Sets the App-owned awaited runtime activation operation.</summary>
        /// <param name="activate">Activation operation.</param>
        public void SetRuntimeActivator(Func<Settings, Task> activate) =>
            this._activateRuntime = activate ?? throw new ArgumentNullException(nameof(activate));

        /// <summary>Sets the App-owned continuation after successful startup recovery.</summary>
        /// <param name="continueStartup">Startup continuation.</param>
        public void SetRecoveryCompleted(Func<Task> continueStartup) =>
            this._recoveryCompleted = continueStartup ?? throw new ArgumentNullException(nameof(continueStartup));

        /// <summary>Attempts to discard the draft and close after warning about pending recovery.</summary>
        /// <returns>True when the window can close.</returns>
        public bool TryDiscardAndClose()
        {
            if (this.IsCommitting)
            {
                return false;
            }

            if (this.RecoveryRequired && !this._recoveryCloseWarned)
            {
                this._recoveryCloseWarned = true;
                this.CommitError = "Settings recovery is unfinished. Close again to leave recovery pending for the next start.";
                return false;
            }

            if (!this.RecoveryRequired)
            {
                this.RestoreAppearancePreview();
            }

            return true;
        }

        /// <summary>
        /// Releases subscriptions held by this settings view model.
        /// </summary>
        public void Dispose()
        {
            this._isDisposed = true;
            this._editGeneration++;
            this.RaisePropertyChanged(nameof(this.IsDraftVisible));
            this._previewAppearance = null;
            this._themeManager.ActualWidgetTransparencyChanged -= this.OnActualWidgetTransparencyChanged;
            this.DetachTheme();
            if (this._attachedSettings != null)
            {
                this.DetachSettings(this._attachedSettings);
            }
        }

        /// <summary>Loads a selected theme into the editable draft if the window remains active.</summary>
        /// <param name="filePath">Selected theme file.</param>
        /// <returns>A task representing the import.</returns>
        internal async Task ImportThemeFileAsync(string filePath)
        {
            if (!this.CanEdit)
            {
                return;
            }

            int generation = this._editGeneration;
            Theme theme = await this._themeService.LoadThemeFromFileAsync(filePath);
            if (!this.CanEdit || generation != this._editGeneration)
            {
                return;
            }

            this.Settings.Theme = theme;
            this.Settings.CustomThemePath = filePath;
            this.Settings.ActiveThemeName = theme.ThemeName;
            this._selectedThemeName = "Custom";
            this.RaisePropertyChanged(nameof(this.SelectedThemeName));
            this._logger.LogInformation($"Theme '{theme.ThemeName}' imported successfully");
        }

        private static string? FindInvalidThemeColor(Theme theme)
        {
            foreach (PropertyInfo property in typeof(Theme).GetProperties())
            {
                if (property.Name == nameof(Theme.PlayPauseColor) && theme.PlayPauseColor == null)
                {
                    continue;
                }

                if (property.PropertyType == typeof(string) &&
                    property.Name is not (nameof(Theme.ThemeName) or nameof(Theme.Author) or nameof(Theme.Version) or nameof(Theme.WidgetBlurMode)) &&
                    !Color.TryParse(property.GetValue(theme) as string, out _))
                {
                    return property.Name;
                }
            }

            return null;
        }

        private static double ToPercent(double normalized)
        {
            return Math.Clamp(normalized, 0.0, 1.0) * 100.0;
        }

        private static double ToNormalized(double percent)
        {
            return Math.Clamp(percent / 100.0, 0.0, 1.0);
        }

        private void Cancel(Window window)
        {
            if (!this.TryDiscardAndClose())
            {
                return;
            }

            window?.Close();
        }

        private async Task ApplyAsync()
        {
            this.LastApplySucceeded = false;
            if (!this.CanCommit)
            {
                return;
            }

            string validationError = this.ValidateDraft();
            if (!string.IsNullOrEmpty(validationError))
            {
                this.CommitError = validationError;
                return;
            }

            this.IsCommitting = true;
            this._editGeneration++;
            this.CommitError = string.Empty;
            try
            {
                Settings candidate = this.Settings.Clone();
                candidate.ActivityPollingIntervalSeconds = (int)this.ActivityPollingIntervalInput!.Value;
                SettingsCommitStatus result = await this._commitCoordinator.CommitAsync(
                    candidate, this._lastAppliedSettings);
                if (result == SettingsCommitStatus.Success)
                {
                    this._lastAppliedSettings = candidate.Clone();
                    this._lastAppliedTheme = candidate.Theme.Clone();
                    this._appearancePreviewChanged = false;
                    this.LastApplySucceeded = true;
                    this._observedAutoStartEnabled = candidate.AutoStartOnLogin;
                    this.RaisePropertyChanged(nameof(this.ObservedAutoStartEnabled));
                    this.RaisePropertyChanged(nameof(this.AutoStartDriftWarning));
                    this.RaisePropertyChanged(nameof(this.HasAutoStartDrift));
                    this._logger.LogDebug("Settings saved successfully");
                }
                else
                {
                    this.RecoveryRequired = result == SettingsCommitStatus.RecoveryRequired;
                    string detail = this._commitCoordinator.LastError ?? "See the application log for details.";
                    this.CommitError = this.RecoveryRequired
                        ? $"Settings recovery is required: {detail} Retry recovery before applying more changes."
                        : $"Settings could not be applied: {detail} The previous values were restored; correct the problem and retry.";
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError("Failed to apply settings.", ex);
                this.CommitError = $"Settings could not be applied: {ex.Message}";
            }
            finally
            {
                this.IsCommitting = false;
                if (this.IsSettingsLoaded && !this.RecoveryRequired)
                {
                    this.ApplyThemeChanges();
                }
            }
        }

        private async Task RetryRecoveryAsync()
        {
            if (this.IsCommitting)
            {
                return;
            }

            bool recovered = await this._commitCoordinator.RecoverAsync(
                this.IsSettingsLoaded ? this._lastAppliedSettings : null);
            this.RecoveryRequired = !recovered;
            if (recovered)
            {
                this._recoveryCloseWarned = false;
            }

            this.CommitError = recovered
                ? string.Empty
                : $"Settings recovery is still required: {this._commitCoordinator.LastError} Check access, then retry.";
            if (recovered && !this.IsSettingsLoaded)
            {
                await this.LoadSettingsAsync();
                if (this.IsSettingsLoaded && this._recoveryCompleted != null)
                {
                    await this._recoveryCompleted();
                }
            }
            else if (recovered)
            {
                this.ApplyThemeChanges();
            }
        }

        private string ValidateDraft()
        {
            if (!string.IsNullOrEmpty(this.ActivityPollingIntervalError))
            {
                return this.ActivityPollingIntervalError;
            }

            if (this.Settings.BreakIntervalMinutes <= 0)
            {
                return "Break interval must be greater than zero.";
            }

            if (this.Settings.DataRetentionDays <= 0)
            {
                return "Data retention must be greater than zero days.";
            }

            if (string.IsNullOrWhiteSpace(this.Settings.LogDirectory) ||
                string.IsNullOrWhiteSpace(this.Settings.WorklogDirectory))
            {
                return "Log and worklog directories are required.";
            }

            if (!this._themeService.ValidateTheme(this.Settings.Theme))
            {
                return "The selected theme contains invalid colors or appearance values.";
            }

            string? invalidColor = FindInvalidThemeColor(this.Settings.Theme);
            if (invalidColor != null)
            {
                return $"The theme color '{invalidColor}' is invalid. Enter a valid color before saving.";
            }

            return string.Empty;
        }

        private async Task BrowseWorklogDirectoryAsync(Window window)
        {
            int generation = this._editGeneration;
            try
            {
                IStorageProvider storageProvider = window.StorageProvider;

                var options = new FolderPickerOpenOptions
                {
                    Title = "Select Worklog Directory",
                    AllowMultiple = false,
                };

                IReadOnlyList<IStorageFolder> result = await storageProvider.OpenFolderPickerAsync(options);

                if (result.Count > 0)
                {
                    if (this.CanEdit && generation == this._editGeneration)
                    {
                        this.Settings.WorklogDirectory = result[0].Path.LocalPath;
                    }
                }
            }
            catch (Exception ex)
            {
                this._logger.LogDebug($"Failed to browse folder: {ex.Message}");
            }
        }

        private async Task LoadSettingsAsync()
        {
            this.IsSettingsLoaded = false;
            this.LoadError = string.Empty;
            try
            {
                Settings savedSettings = await this._settingsProvider.LoadAsync();
                this._lastAppliedSettings = savedSettings.Clone();
                this.Settings = savedSettings.Clone();
                Theme? selectedPreset = this._themeService.GetBuiltInTheme(this.Settings.ActiveThemeName);
                if (selectedPreset != null && !string.Equals(
                        this.Settings.Theme.ThemeName,
                        selectedPreset.ThemeName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    this.Settings.Theme = selectedPreset;
                }

                this._lastAppliedSettings = this.Settings.Clone();
                this._lastAppliedTheme = this.Settings.Theme.Clone();
                this._appearancePreviewChanged = false;
                this._selectedThemeName = this.Settings.ActiveThemeName;
                this.RaisePropertyChanged(nameof(this.SelectedThemeName));
                this.RaisePropertyChanged(nameof(this.IsDeveloperModeVisible));
                this.RaisePropertyChanged(nameof(this.SelectedDeveloperLogLevel));
                this._observedAutoStartEnabled = this._autoStartService.IsAutoStartEnabled();
                this.RaisePropertyChanged(nameof(this.ObservedAutoStartEnabled));

                this.RaiseOpacityDiagnostics();
                this.IsSettingsLoaded = true;
                if (this._previewAppearance != null)
                {
                    this.ApplyThemeChanges();
                }

                this.RaisePropertyChanged(nameof(this.AutoStartDriftWarning));
                this.RaisePropertyChanged(nameof(this.HasAutoStartDrift));
            }
            catch (SettingsRecoveryRequiredException)
            {
                this.RecoveryRequired = true;
                this.LoadError = "An unfinished Settings commit needs recovery before the file can be loaded.";
                this.CommitError = "Settings recovery is required. Retry recovery before applying more changes.";
            }
            catch (Exception ex)
            {
                this._logger.LogError("Failed to load settings.", ex);
                this.LoadError = "Settings could not be loaded. The existing file was left unchanged. Retry after fixing the file or access problem.";
            }
        }

        private async Task RetryLoadAsync()
        {
            await this.LoadSettingsAsync();
            if (this.IsSettingsLoaded && this._recoveryCompleted != null)
            {
                await this._recoveryCompleted();
            }
        }

        private async Task OkAsync()
        {
            await this.ApplyAsync();
            this._logger.LogInformation("OK command executed - settings applied and window will be closed.");

            // Window will be closed by the calling code
        }

        private async Task ImportThemeAsync(Window window)
        {
            int generation = this._editGeneration;
            try
            {
                IStorageProvider storageProvider = window.StorageProvider;
                var options = new FilePickerOpenOptions
                {
                    Title = "Import Theme",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("FocusTimer Theme")
                        {
                            Patterns = new[] { "*.fttheme" },
                        },
                        FilePickerFileTypes.All,
                    },
                };
                IReadOnlyList<IStorageFile> result = await storageProvider.OpenFilePickerAsync(options);
                if (result.Count > 0 && this.CanEdit && generation == this._editGeneration)
                {
                    string filePath = result[0].Path.LocalPath;
                    await this.ImportThemeFileAsync(filePath);
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError($"Failed to import theme: {ex.Message}", ex);
                this.CommitError = $"Theme import failed: {ex.Message}";
            }
        }

        private async Task ExportThemeAsync(Window window)
        {
            try
            {
                IStorageProvider storageProvider = window.StorageProvider;

                var options = new FilePickerSaveOptions
                {
                    Title = "Export Theme",
                    DefaultExtension = "fttheme",
                    SuggestedFileName = $"{this.Settings.Theme.ThemeName}.fttheme",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("FocusTimer Theme")
                        {
                            Patterns = new[] { "*.fttheme" },
                        },
                    },
                };

                IStorageFile? result = await storageProvider.SaveFilePickerAsync(options);

                if (result != null)
                {
                    string filePath = result.Path.LocalPath;
                    await this._themeService.SaveThemeToFileAsync(this.Settings.Theme, filePath);
                    this._logger.LogInformation($"Theme exported to: {filePath}");
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError("Failed to export theme.", ex);

                // TODO: Show error dialog
            }
        }

        private void ResetTheme()
        {
            if (!this.CanEdit)
            {
                return;
            }

            this.Settings.Theme = this._themeService.GetBuiltInTheme("Dark")
                ?? throw new InvalidOperationException("The built-in Dark theme is unavailable.");
            this.Settings.ActiveThemeName = "Dark";
            this._selectedThemeName = "Dark";
            this.RaisePropertyChanged(nameof(this.SelectedThemeName));
            this._logger.LogInformation("Theme reset to default.");
        }

        private void LoadThemeByName(string themeName)
        {
            Theme? theme = this._themeService.GetBuiltInTheme(themeName);
            if (theme != null)
            {
                this.Settings.Theme = theme.Clone();
                this.Settings.ActiveThemeName = themeName;
            }
        }

        private void AttachSettings(Settings settings)
        {
            this._attachedSettings = settings;
            this._attachedSettings.PropertyChanged += this.OnSettingsPropertyChanged;
            this.AttachTheme(settings.Theme);
        }

        private void DetachSettings(Settings settings)
        {
            settings.PropertyChanged -= this.OnSettingsPropertyChanged;
            if (ReferenceEquals(this._attachedSettings, settings))
            {
                this._attachedSettings = null;
            }

            this.DetachTheme();
        }

        private void AttachTheme(Theme theme)
        {
            this.DetachTheme();
            this._attachedTheme = theme;
            this._attachedTheme.PropertyChanged += this.OnThemePropertyChanged;
        }

        private void DetachTheme()
        {
            if (this._attachedTheme != null)
            {
                this._attachedTheme.PropertyChanged -= this.OnThemePropertyChanged;
                this._attachedTheme = null;
            }
        }

        private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!this.RecoveryRequired)
            {
                this.CommitError = string.Empty;
            }

            if (e.PropertyName == nameof(this.Settings.Theme))
            {
                this.RaisePropertyChanged(nameof(this.PlayPauseColor));
                this.AttachTheme(this.Settings.Theme);
                this._appearancePreviewChanged = true;
                this.ApplyThemeChanges();
                this.RaiseOpacityDiagnostics();
                return;
            }

            if (e.PropertyName == nameof(this.Settings.WidgetOpacity))
            {
                this.RaisePropertyChanged(nameof(this.OverallFadePercent));
                this.RaisePropertyChanged(nameof(this.NormalizedOverallFade));
                this.RaisePropertyChanged(nameof(this.OpacityDiagnosticsSummary));
            }

            if (e.PropertyName is nameof(this.Settings.WidgetOpacity) or nameof(this.Settings.WidgetScale) or nameof(this.Settings.UseCompactMode))
            {
                this.ApplyThemeChanges();
            }
        }

        private void OnThemePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(Theme.ButtonNormal) or nameof(Theme.PlayPauseColor))
            {
                this.RaisePropertyChanged(nameof(this.PlayPauseColor));
            }

            if (!this.RecoveryRequired)
            {
                this.CommitError = string.Empty;
            }

            this._appearancePreviewChanged = true;
            this.ApplyThemeChanges();
            if (e.PropertyName == nameof(Theme.WidgetBlurMode))
            {
                this.RaisePropertyChanged(nameof(this.SelectedBlurMode));
                this.RaisePropertyChanged(nameof(this.IsBackgroundTintOpacityAvailable));
                this.RaisePropertyChanged(nameof(this.TransparencyDiagnostics));
                this.RaisePropertyChanged(nameof(this.OpacityDiagnosticsSummary));
            }

            if (e.PropertyName == nameof(Theme.BackgroundOpacity))
            {
                this.RaisePropertyChanged(nameof(this.BackgroundOpacityPercent));
                this.RaisePropertyChanged(nameof(this.NormalizedBackgroundOpacity));
            }
            else if (e.PropertyName == nameof(Theme.TimerOpacity))
            {
                this.RaisePropertyChanged(nameof(this.ClockOpacityPercent));
                this.RaisePropertyChanged(nameof(this.NormalizedClockOpacity));
            }
            else if (e.PropertyName == nameof(Theme.ButtonOpacity))
            {
                this.RaisePropertyChanged(nameof(this.ButtonsOpacityPercent));
                this.RaisePropertyChanged(nameof(this.NormalizedButtonsOpacity));
            }

            this.RaisePropertyChanged(nameof(this.OpacityDiagnosticsSummary));
        }

        private void OnActualWidgetTransparencyChanged(WindowTransparencyLevel level) =>
            this.RaisePropertyChanged(nameof(this.TransparencyDiagnostics));

        private string GetWidgetSurfaceDescription()
        {
            if (this._themeManager.IsWidgetShellFallbackActive)
            {
                return this.SelectedBlurMode == WidgetBlurModes.Solid ? "solid fill" : "solid fallback";
            }

            if (this._themeManager.ActualWidgetTransparency == WindowTransparencyLevel.Transparent)
            {
                return "transparent backdrop";
            }

            return "platform blur";
        }

        private void ApplyThemeChanges()
        {
            if (!this.IsSettingsLoaded || this.RecoveryRequired || this._isDisposed)
            {
                return;
            }

            if (FindInvalidThemeColor(this.Settings.Theme) != null)
            {
                return;
            }

            this._appearancePreviewChanged = true;
            this._themeManager.ApplyTheme(this.Settings.Theme);
            this._previewAppearance?.Invoke(this.Settings);
            this._logger.LogInformation($"Applied theme: {this.Settings.Theme.ThemeName}");
        }

        private void OpenRepository()
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = this.RepositoryUrl,
                    UseShellExecute = true,
                };

                Process.Start(processInfo);
            }
            catch (Exception ex)
            {
                this._logger.LogError("Failed to open repository URL.", ex);
            }
        }

        private void OnVersionInfoClicked()
        {
            this._versionClickCount++;
            if (this._versionClickCount < 7)
            {
                return;
            }

            this._versionClickCount = 0;
            if (this.Settings.DeveloperModeEnabled)
            {
                return;
            }

            this.Settings.DeveloperModeEnabled = true;
            this.RaisePropertyChanged(nameof(this.IsDeveloperModeVisible));
        }

        private string LoadChangelogContent()
        {
            try
            {
                var current = new DirectoryInfo(AppContext.BaseDirectory);
                for (int i = 0; i < 8 && current != null; i++)
                {
                    string candidate = Path.Combine(current.FullName, "docs", "CHANGELOG.md");
                    if (File.Exists(candidate))
                    {
                        return File.ReadAllText(candidate);
                    }

                    current = current.Parent;
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError("Failed to load changelog content.", ex);
            }

            return "No changelog file found. Add docs/CHANGELOG.md to populate this section.";
        }

        private void RaiseOpacityDiagnostics()
        {
            this.RaisePropertyChanged(nameof(this.BackgroundOpacityPercent));
            this.RaisePropertyChanged(nameof(this.SelectedBlurMode));
            this.RaisePropertyChanged(nameof(this.IsBackgroundTintOpacityAvailable));
            this.RaisePropertyChanged(nameof(this.ClockOpacityPercent));
            this.RaisePropertyChanged(nameof(this.ButtonsOpacityPercent));
            this.RaisePropertyChanged(nameof(this.OverallFadePercent));

            this.RaisePropertyChanged(nameof(this.NormalizedBackgroundOpacity));
            this.RaisePropertyChanged(nameof(this.NormalizedClockOpacity));
            this.RaisePropertyChanged(nameof(this.NormalizedButtonsOpacity));

            this.RaisePropertyChanged(nameof(this.NormalizedOverallFade));
            this.RaisePropertyChanged(nameof(this.TransparencyDiagnostics));
            this.RaisePropertyChanged(nameof(this.AcrylicDiagnostics));
            this.RaisePropertyChanged(nameof(this.OpacityDiagnosticsSummary));
        }
    }
}
