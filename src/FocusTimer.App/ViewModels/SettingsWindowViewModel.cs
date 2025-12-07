using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.App.Services;
using ReactiveUI;

namespace FocusTimer.App.ViewModels;

/// <summary>
/// ViewModel for the Settings window.
/// </summary>
public class SettingsWindowViewModel : ReactiveObject
{
    private readonly ISettingsProvider _settingsProvider;
    private readonly IAutoStartService _autoStartService;
    private readonly IThemeService _themeService;
    private readonly ThemeManager _themeManager;
    private Settings _settings;
    private string _selectedThemeName;
    
    public SettingsWindowViewModel(
        ISettingsProvider settingsProvider, 
        IAutoStartService autoStartService,
        IThemeService themeService,
        ThemeManager themeManager)
    {
        _settingsProvider = settingsProvider;
        _autoStartService = autoStartService;
        _themeService = themeService;
        _themeManager = themeManager;
        _settings = new Settings();
        _selectedThemeName = "Dark";
        
        // Initialize commands
        ApplyCommand = ReactiveCommand.CreateFromTask(ApplyAsync);
        OkCommand = ReactiveCommand.CreateFromTask(OkAsync);
        CancelCommand = ReactiveCommand.Create<Window>(Cancel);
        BrowseLogDirectoryCommand = ReactiveCommand.CreateFromTask<Window>(BrowseLogDirectoryAsync);
        ImportThemeCommand = ReactiveCommand.CreateFromTask<Window>(ImportThemeAsync);
        ExportThemeCommand = ReactiveCommand.CreateFromTask<Window>(ExportThemeAsync);
        ResetThemeCommand = ReactiveCommand.Create(ResetTheme);
        
        // Load settings
        _ = LoadSettingsAsync();
        
        // Subscribe to theme property changes to apply them immediately
        _settings.Theme.PropertyChanged += (s, e) => ApplyThemeChanges();
    }

    #region Properties

    /// <summary>
    /// The settings being edited.
    /// </summary>
    public Settings Settings
    {
        get => _settings;
        set => this.RaiseAndSetIfChanged(ref _settings, value);
    }

    /// <summary>
    /// List of available theme names for the ComboBox.
    /// </summary>
    public List<string> AvailableThemes => _themeService.BuiltInThemes
        .Select(t => t.ThemeName)
        .ToList();

    /// <summary>
    /// Currently selected theme name in the ComboBox.
    /// </summary>
    public string SelectedThemeName
    {
        get => _selectedThemeName;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedThemeName, value);
            if (!string.IsNullOrEmpty(value))
            {
                LoadThemeByName(value);
            }
        }
    }

    #endregion

    #region Commands

    public ICommand ApplyCommand { get; }
    public ICommand OkCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand BrowseLogDirectoryCommand { get; }
    public ICommand ImportThemeCommand { get; }
    public ICommand ExportThemeCommand { get; }
    public ICommand ResetThemeCommand { get; }

    #endregion

    #region Events

    /// <summary>
    /// Raised when settings have been applied/saved.
    /// </summary>
    public event EventHandler? SettingsApplied;

    #endregion

    #region Command Implementations

    private async Task LoadSettingsAsync()
    {
        try
        {
            Settings = await _settingsProvider.LoadAsync();
            
            // Sync auto-start setting with actual registry state
            var isAutoStartEnabled = _autoStartService.IsAutoStartEnabled();
            if (Settings.AutoStartOnLogin != isAutoStartEnabled)
            {
                Settings.AutoStartOnLogin = isAutoStartEnabled;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
            // Keep default settings
        }
    }

    private async Task ApplyAsync()
    {
        try
        {
            // Apply auto-start setting to registry before saving
            _autoStartService.SetAutoStart(_settings.AutoStartOnLogin);
            
            await _settingsProvider.SaveAsync(_settings);
            SettingsApplied?.Invoke(this, EventArgs.Empty);
            System.Diagnostics.Debug.WriteLine("Settings saved successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            // TODO: Show error dialog to user
        }
    }

    private async Task OkAsync()
    {
        await ApplyAsync();
        // Window will be closed by the calling code
    }

    private void Cancel(Window window)
    {
        window?.Close();
    }

    private async Task BrowseLogDirectoryAsync(Window window)
    {
        try
        {
            var storageProvider = window.StorageProvider;
            
            var options = new FolderPickerOpenOptions
            {
                Title = "Select Log Directory",
                AllowMultiple = false
            };

            var result = await storageProvider.OpenFolderPickerAsync(options);
            
            if (result.Count > 0)
            {
                Settings.LogDirectory = result[0].Path.LocalPath;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to browse folder: {ex.Message}");
        }
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
                        Patterns = new[] { "*.fttheme" }
                    },
                    FilePickerFileTypes.All
                }
            };

            var result = await storageProvider.OpenFilePickerAsync(options);
            
            if (result.Count > 0)
            {
                var filePath = result[0].Path.LocalPath;
                var theme = await _themeService.LoadThemeFromFileAsync(filePath);
                
                Settings.Theme = theme;
                Settings.CustomThemePath = filePath;
                Settings.ActiveThemeName = theme.ThemeName;
                _selectedThemeName = "Custom";
                this.RaisePropertyChanged(nameof(SelectedThemeName));
                
                ApplyThemeChanges();
                
                System.Diagnostics.Debug.WriteLine($"Theme '{theme.ThemeName}' imported successfully");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to import theme: {ex.Message}");
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
                SuggestedFileName = $"{Settings.Theme.ThemeName}.fttheme",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("FocusTimer Theme")
                    {
                        Patterns = new[] { "*.fttheme" }
                    }
                }
            };

            var result = await storageProvider.SaveFilePickerAsync(options);
            
            if (result != null)
            {
                var filePath = result.Path.LocalPath;
                await _themeService.SaveThemeToFileAsync(Settings.Theme, filePath);
                System.Diagnostics.Debug.WriteLine($"Theme exported to: {filePath}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to export theme: {ex.Message}");
            // TODO: Show error dialog
        }
    }

    private void ResetTheme()
    {
        _themeService.ResetToDefault();
        Settings.Theme = _themeService.CurrentTheme.Clone();
        Settings.ActiveThemeName = "Dark";
        _selectedThemeName = "Dark";
        this.RaisePropertyChanged(nameof(SelectedThemeName));
        
        ApplyThemeChanges();
    }

    private void LoadThemeByName(string themeName)
    {
        var theme = _themeService.GetBuiltInTheme(themeName);
        if (theme != null)
        {
            Settings.Theme = theme.Clone();
            Settings.ActiveThemeName = themeName;
            ApplyThemeChanges();
        }
    }

    private void ApplyThemeChanges()
    {
        _themeManager.ApplyTheme(Settings.Theme);
    }

    #endregion
}
