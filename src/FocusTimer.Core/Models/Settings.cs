using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace FocusTimer.Core.Models;

/// <summary>
/// Application settings/preferences.
/// </summary>
public class Settings : INotifyPropertyChanged
{
    private bool _autoStartOnLogin;
    private bool _startMinimized;
    private bool _alwaysOnTop = true;
    private int _breakIntervalMinutes = 50;
    private bool _breakRemindersEnabled = true;
    private string _logDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "FocusTimer",
        "logs");
    private int _dataRetentionDays = 90;
    private double _widgetScale = 1.0;
    private double _widgetOpacity = 1.0;
    private bool _useCompactMode;
    private string? _hotkeyShowHide;
    private string? _hotkeyToggleTimer;
    private Theme _theme = new Theme();
    private string _activeThemeName = "Dark";
    private string? _customThemePath;

    public event PropertyChangedEventHandler? PropertyChanged;

    // General
    public bool AutoStartOnLogin
    {
        get => _autoStartOnLogin;
        set => SetField(ref _autoStartOnLogin, value);
    }

    public bool StartMinimized
    {
        get => _startMinimized;
        set => SetField(ref _startMinimized, value);
    }

    public bool AlwaysOnTop
    {
        get => _alwaysOnTop;
        set => SetField(ref _alwaysOnTop, value);
    }

    // Break reminders
    public int BreakIntervalMinutes
    {
        get => _breakIntervalMinutes;
        set => SetField(ref _breakIntervalMinutes, value);
    }

    public bool BreakRemindersEnabled
    {
        get => _breakRemindersEnabled;
        set => SetField(ref _breakRemindersEnabled, value);
    }

    // Logging
    public string LogDirectory
    {
        get => _logDirectory;
        set => SetField(ref _logDirectory, value);
    }

    public int DataRetentionDays
    {
        get => _dataRetentionDays;
        set => SetField(ref _dataRetentionDays, value);
    }

    // Widget appearance - with validation
    public double WidgetScale
    {
        get => _widgetScale;
        set
        {
            var clamped = Math.Clamp(value, 0.5, 3.0);
            SetField(ref _widgetScale, clamped);
        }
    }

    /// <summary>
    /// Gets or sets the opacity of the widget (0.0 to 1.0).
    /// </summary>
    public double WidgetOpacity
    {
        get => _widgetOpacity;
        set
        {
            var clamped = Math.Clamp(value, 0.0, 1.0);
            SetField(ref _widgetOpacity, clamped);
        }
    }

    public bool UseCompactMode
    {
        get => _useCompactMode;
        set => SetField(ref _useCompactMode, value);
    }

    // Future: Hotkey settings (placeholders)
    public string? HotkeyShowHide
    {
        get => _hotkeyShowHide;
        set => SetField(ref _hotkeyShowHide, value);
    }

    public string? HotkeyToggleTimer
    {
        get => _hotkeyToggleTimer;
        set => SetField(ref _hotkeyToggleTimer, value);
    }

    // Theme Management
    public Theme Theme
    {
        get => _theme;
        set => SetField(ref _theme, value);
    }

    public string ActiveThemeName
    {
        get => _activeThemeName;
        set => SetField(ref _activeThemeName, value);
    }

    public string? CustomThemePath
    {
        get => _customThemePath;
        set => SetField(ref _customThemePath, value);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
