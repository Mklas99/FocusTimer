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
        private readonly ISettingsProvider _settingsProvider;
        private readonly IAutoStartService _autoStartService;
        private readonly IThemeService _themeService;
        private readonly ThemeManager _themeManager;
        private readonly IAppLogger _logger;
        private readonly string _changelogContent;
        private Settings? _attachedSettings;
        private Theme? _attachedTheme;
        private Settings _settings;
        private string _selectedThemeName;
        private int _versionClickCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsWindowViewModel"/> class.
        /// </summary>
        /// <param name="settingsProvider">The settings provider service.</param>
        /// <param name="autoStartService">The auto-start service.</param>
        /// <param name="themeService">The theme service.</param>
        /// <param name="themeManager">The theme manager.</param>
        /// <param name="logWriter">The application logger.</param>
        public SettingsWindowViewModel(
            ISettingsProvider settingsProvider,
            IAutoStartService autoStartService,
            IThemeService themeService,
            ThemeManager themeManager,
            IAppLogger logWriter)
        {
            this._settingsProvider = settingsProvider;
            this._autoStartService = autoStartService;
            this._themeService = themeService;
            this._themeManager = themeManager;
            this._logger = logWriter;
            this._settings = new Settings();
            this._selectedThemeName = "Dark";
            this._changelogContent = this.LoadChangelogContent();
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

            // Load settings
            _ = this.LoadSettingsAsync();
        }

        /// <summary>
        /// Raised when settings have been applied/saved.
        /// </summary>
        public event EventHandler? SettingsApplied;

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
                this.RaisePropertyChanged(nameof(this.IsDeveloperModeVisible));
                this.RaisePropertyChanged(nameof(this.SelectedDeveloperLogLevel));
            }
        }

        /// <summary>
        /// Gets list of available theme names for the ComboBox.
        /// </summary>
        public List<string> AvailableThemes => [.. this._themeService.BuiltInThemes.Select(t => t.ThemeName)];

        /// <summary>
        /// Gets available developer log levels.
        /// </summary>
        public List<string> AvailableDeveloperLogLevels { get; } = new() { "Verbose", "Debug", "Information", "Warning", "Error" };

        /// <summary>
        /// Gets application version shown in the About tab.
        /// </summary>
        public string AppVersion
        {
            get
            {
                var assembly = Assembly.GetEntryAssembly();
                var informationalVersion = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                if (!string.IsNullOrWhiteSpace(informationalVersion))
                {
                    return informationalVersion.Split('+')[0];
                }

                return assembly?.GetName().Version?.ToString() ?? "1.0.0";
            }
        }

        /// <summary>
        /// Gets application author shown in the About tab.
        /// </summary>
        public string AppAuthor => Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "Mklas99";

        /// <summary>
        /// Gets a short application description shown in the About tab.
        /// </summary>
        public string AppInformation => Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? "FocusTimer is a desktop focus timer with break reminders, work logging, and customizable appearance.";

        /// <summary>
        /// Gets repository URL shown in the About tab.
        /// </summary>
        public string RepositoryUrl => "https://github.com/Mklas99/FocusTimer";

        /// <summary>
        /// Gets changelog content shown in the About tab.
        /// </summary>
        public string ChangelogContent => this._changelogContent;

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
                this.RaiseAndSetIfChanged(ref this._selectedThemeName, value);
                if (!string.IsNullOrEmpty(value))
                {
                    this.LoadThemeByName(value);
                }
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
        /// Gets the current value of a theme color property.
        /// </summary>
        /// <param name="propertyName">The theme property name.</param>
        /// <returns>The current color value, or an empty string if the property is unknown.</returns>
        public string GetThemeColor(string propertyName)
        {
            var property = typeof(Theme).GetProperty(propertyName);
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
            var property = typeof(Theme).GetProperty(propertyName);
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

        private async Task ApplyAsync()
        {
            try
            {
                // Apply auto-start setting to registry before saving
                this._autoStartService.SetAutoStart(this._settings.AutoStartOnLogin);

                await this._settingsProvider.SaveAsync(this._settings);
                this.SettingsApplied?.Invoke(this, EventArgs.Empty);
                this._logger.LogDebug("Settings saved successfully");
            }
            catch (Exception ex)
            {
                this._logger.LogDebug($"Failed to save settings: {ex.Message}");

                // TODO: Show error dialog to user
            }
        }

        private void Cancel(Window window)
        {
            window?.Close();
        }

        private async Task BrowseWorklogDirectoryAsync(Window window)
        {
            try
            {
                var storageProvider = window.StorageProvider;

                var options = new FolderPickerOpenOptions
                {
                    Title = "Select Worklog Directory",
                    AllowMultiple = false,
                };

                var result = await storageProvider.OpenFolderPickerAsync(options);

                if (result.Count > 0)
                {
                    this.Settings.WorklogDirectory = result[0].Path.LocalPath;
                }
            }
            catch (Exception ex)
            {
                this._logger.LogDebug($"Failed to browse folder: {ex.Message}");
            }
        }

        private async Task LoadSettingsAsync()
        {
            try
            {
                this.Settings = await this._settingsProvider.LoadAsync();
                this._selectedThemeName = this.Settings.ActiveThemeName;
                this.RaisePropertyChanged(nameof(this.SelectedThemeName));
                this.RaisePropertyChanged(nameof(this.IsDeveloperModeVisible));
                this.RaisePropertyChanged(nameof(this.SelectedDeveloperLogLevel));
                var isAutoStartEnabled = this._autoStartService.IsAutoStartEnabled();
                if (this.Settings.AutoStartOnLogin != isAutoStartEnabled)
                {
                    this.Settings.AutoStartOnLogin = isAutoStartEnabled;
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError("Failed to load settings.", ex);

                // Keep default settings
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
            try
            {
                var storageProvider = window.StorageProvider;
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
                var result = await storageProvider.OpenFilePickerAsync(options);
                if (result.Count > 0)
                {
                    var filePath = result[0].Path.LocalPath;
                    var theme = await this._themeService.LoadThemeFromFileAsync(filePath);
                    this.Settings.Theme = theme;
                    this.Settings.CustomThemePath = filePath;
                    this.Settings.ActiveThemeName = theme.ThemeName;
                    this._selectedThemeName = "Custom";
                    this.RaisePropertyChanged(nameof(this.SelectedThemeName));
                    this._logger.LogInformation($"Theme '{theme.ThemeName}' imported successfully");
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError($"Failed to import theme: {ex.Message}", ex);

                // TODO: Show error dialog
            }
        }

        private async Task ExportThemeAsync(Window window)
        {
            try
            {
                var storageProvider = window.StorageProvider;

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

                var result = await storageProvider.SaveFilePickerAsync(options);

                if (result != null)
                {
                    var filePath = result.Path.LocalPath;
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
            this._themeService.ResetToDefault();
            this.Settings.Theme = this._themeService.CurrentTheme.Clone();
            this.Settings.ActiveThemeName = "Dark";
            this._selectedThemeName = "Dark";
            this.RaisePropertyChanged(nameof(this.SelectedThemeName));
            this._logger.LogInformation("Theme reset to default.");
        }

        private void LoadThemeByName(string themeName)
        {
            var theme = this._themeService.GetBuiltInTheme(themeName);
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
            if (e.PropertyName != nameof(this.Settings.Theme))
            {
                return;
            }

            this.AttachTheme(this.Settings.Theme);
            this.ApplyThemeChanges();
        }

        private void OnThemePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            this.ApplyThemeChanges();
        }

        private void ApplyThemeChanges()
        {
            this._themeManager.ApplyTheme(this.Settings.Theme);
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
                for (var i = 0; i < 8 && current != null; i++)
                {
                    var candidate = Path.Combine(current.FullName, "docs", "CHANGELOG.md");
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
    }
}
