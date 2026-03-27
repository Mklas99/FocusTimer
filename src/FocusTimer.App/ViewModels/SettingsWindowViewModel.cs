namespace FocusTimer.App.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
        private Settings _settings;
        private string _selectedThemeName;

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

            // Initialize commands
            this.ApplyCommand = ReactiveCommand.CreateFromTask(this.ApplyAsync);
            this.OkCommand = ReactiveCommand.CreateFromTask(this.OkAsync);
            this.CancelCommand = ReactiveCommand.Create<Window>(this.Cancel);
            this.BrowseWorklogDirectoryCommand = ReactiveCommand.CreateFromTask<Window>(this.BrowseWorklogDirectoryAsync);
            this.ImportThemeCommand = ReactiveCommand.CreateFromTask<Window>(this.ImportThemeAsync);
            this.ExportThemeCommand = ReactiveCommand.CreateFromTask<Window>(this.ExportThemeAsync);
            this.ResetThemeCommand = ReactiveCommand.Create(this.ResetTheme);

            // Load settings
            _ = this.LoadSettingsAsync();

            // Subscribe to theme property changes to apply them immediately
            this._settings.Theme.PropertyChanged += (s, e) => this.ApplyThemeChanges();
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
        /// Gets or sets the settings being edited.
        /// </summary>
        public Settings Settings
        {
            get => this._settings;
            set => this.RaiseAndSetIfChanged(ref this._settings, value);
        }

        /// <summary>
        /// Gets list of available theme names for the ComboBox.
        /// </summary>
        public List<string> AvailableThemes => [.. this._themeService.BuiltInThemes.Select(t => t.ThemeName)];

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
                    this.ApplyThemeChanges();
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

            this.ApplyThemeChanges();
            this._logger.LogInformation("Theme reset to default.");
        }

        private void LoadThemeByName(string themeName)
        {
            var theme = this._themeService.GetBuiltInTheme(themeName);
            if (theme != null)
            {
                this.Settings.Theme = theme.Clone();
                this.Settings.ActiveThemeName = themeName;
                this.ApplyThemeChanges();
            }
        }

        private void ApplyThemeChanges()
        {
            this._themeManager.ApplyTheme(this.Settings.Theme);
            this._logger.LogInformation($"Applied theme: {this.Settings.Theme.ThemeName}");
        }
    }
}
