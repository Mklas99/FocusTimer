using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using ReactiveUI;

namespace FocusTimer.App.ViewModels;

/// <summary>
/// ViewModel for the Settings window.
/// </summary>
public class SettingsWindowViewModel : ReactiveObject
{
    private readonly ISettingsProvider _settingsProvider;
    private readonly IAutoStartService _autoStartService;
    private Settings _settings;
    
    public SettingsWindowViewModel(ISettingsProvider settingsProvider, IAutoStartService autoStartService)
    {
        _settingsProvider = settingsProvider;
        _autoStartService = autoStartService;
        _settings = new Settings();
        
        // Initialize commands
        ApplyCommand = ReactiveCommand.CreateFromTask(ApplyAsync);
        OkCommand = ReactiveCommand.CreateFromTask(OkAsync);
        CancelCommand = ReactiveCommand.Create<Window>(Cancel);
        BrowseLogDirectoryCommand = ReactiveCommand.CreateFromTask<Window>(BrowseLogDirectoryAsync);
        
        // Load settings
        _ = LoadSettingsAsync();
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

    #endregion

    #region Commands

    public ICommand ApplyCommand { get; }
    public ICommand OkCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand BrowseLogDirectoryCommand { get; }

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

    #endregion
}
