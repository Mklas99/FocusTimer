using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace FocusTimer.Core.Models;

/// <summary>
/// Represents a complete theme with all customizable colors for the FocusTimer application.
/// </summary>
public class Theme : INotifyPropertyChanged
{
    // Metadata
    private string _themeName = "Custom";
    private string _author = "User";
    private string _version = "1.0";

    // Core Window Colors
    private string _windowBackground = "#1E1E1E";
    private string _windowForeground = "#FFFFFF";
    private string _windowBorder = "#3F3F46";

    // Text Colors
    private string _primaryText = "#F5F5F5";
    private string _secondaryText = "#CCCCCC";
    private string _disabledText = "#666666";

    // Timer Display
    private string _timerText = "#FFFFFF";
    private string _timerBackground = "#00000000"; // Transparent

    // Buttons & Controls
    private string _buttonNormal = "#4CDEFFBD";
    private string _buttonHover = "#5DEFFCE7";
    private string _buttonPressed = "#3BBD99AC";
    private string _buttonDisabled = "#40FFFFFF";

    // Opacity Controls
    private double _backgroundOpacity = 1.0;
    private double _timerOpacity = 1.0;
    private double _buttonOpacity = 1.0;

    // Accents
    private string _accentPrimary = "#0078D7";
    private string _accentSecondary = "#005A9E";
    private string _dangerColor = "#D9534F";
    private string _successColor = "#4CAF50";
    private string _warningColor = "#FFA726";

    // Input Controls
    private string _inputBackground = "#1AFFFFFF";
    private string _inputBorder = "#35FFFFFF";
    private string _inputFocusBorder = "#0078D7";
    private string _inputText = "#FFFFFF";

    // Project Tag
    private string _projectTagBackground = "#1AFFFFFF";
    private string _projectTagBorder = "#35FFFFFF";
    private string _projectTagText = "#FFFFFF";

    // Title Bar (Custom Window)
    private string _titleBarBackground = "#00000000";
    private string _titleBarForeground = "#CCCCCC";
    private string _titleBarButtonHover = "#1A000000";
    private string _titleBarButtonPressed = "#33000000";

    // Settings Window
    private string _settingsBackground = "#2D2D30";
    private string _settingsSectionHeader = "#F5F5F5";
    private string _settingsLabelText = "#CCCCCC";

    // Tab Control
    private string _tabBackground = "#2D2D30";
    private string _tabSelectedBackground = "#007ACC";
    private string _tabHoverBackground = "#1E1E1E";
    private string _tabText = "#CCCCCC";
    private string _tabSelectedText = "#FFFFFF";

    public event PropertyChangedEventHandler? PropertyChanged;

    // Metadata Properties
    [JsonPropertyName("themeName")]
    public string ThemeName
    {
        get => _themeName;
        set => SetField(ref _themeName, value);
    }

    [JsonPropertyName("author")]
    public string Author
    {
        get => _author;
        set => SetField(ref _author, value);
    }

    [JsonPropertyName("version")]
    public string Version
    {
        get => _version;
        set => SetField(ref _version, value);
    }

    // Window Colors
    [JsonPropertyName("windowBackground")]
    public string WindowBackground
    {
        get => _windowBackground;
        set => SetField(ref _windowBackground, value);
    }

    [JsonPropertyName("windowForeground")]
    public string WindowForeground
    {
        get => _windowForeground;
        set => SetField(ref _windowForeground, value);
    }

    [JsonPropertyName("windowBorder")]
    public string WindowBorder
    {
        get => _windowBorder;
        set => SetField(ref _windowBorder, value);
    }

    // Text Colors
    [JsonPropertyName("primaryText")]
    public string PrimaryText
    {
        get => _primaryText;
        set => SetField(ref _primaryText, value);
    }

    [JsonPropertyName("secondaryText")]
    public string SecondaryText
    {
        get => _secondaryText;
        set => SetField(ref _secondaryText, value);
    }

    [JsonPropertyName("disabledText")]
    public string DisabledText
    {
        get => _disabledText;
        set => SetField(ref _disabledText, value);
    }

    // Timer Display
    [JsonPropertyName("timerText")]
    public string TimerText
    {
        get => _timerText;
        set => SetField(ref _timerText, value);
    }

    [JsonPropertyName("timerBackground")]
    public string TimerBackground
    {
        get => _timerBackground;
        set => SetField(ref _timerBackground, value);
    }

    // Buttons & Controls
    [JsonPropertyName("buttonNormal")]
    public string ButtonNormal
    {
        get => _buttonNormal;
        set => SetField(ref _buttonNormal, value);
    }

    [JsonPropertyName("buttonHover")]
    public string ButtonHover
    {
        get => _buttonHover;
        set => SetField(ref _buttonHover, value);
    }

    [JsonPropertyName("buttonPressed")]
    public string ButtonPressed
    {
        get => _buttonPressed;
        set => SetField(ref _buttonPressed, value);
    }

    [JsonPropertyName("buttonDisabled")]
    public string ButtonDisabled
    {
        get => _buttonDisabled;
        set => SetField(ref _buttonDisabled, value);
    }

    // Accents
    [JsonPropertyName("accentPrimary")]
    public string AccentPrimary
    {
        get => _accentPrimary;
        set => SetField(ref _accentPrimary, value);
    }

    [JsonPropertyName("accentSecondary")]
    public string AccentSecondary
    {
        get => _accentSecondary;
        set => SetField(ref _accentSecondary, value);
    }

    [JsonPropertyName("dangerColor")]
    public string DangerColor
    {
        get => _dangerColor;
        set => SetField(ref _dangerColor, value);
    }

    [JsonPropertyName("successColor")]
    public string SuccessColor
    {
        get => _successColor;
        set => SetField(ref _successColor, value);
    }

    [JsonPropertyName("warningColor")]
    public string WarningColor
    {
        get => _warningColor;
        set => SetField(ref _warningColor, value);
    }

    // Input Controls
    [JsonPropertyName("inputBackground")]
    public string InputBackground
    {
        get => _inputBackground;
        set => SetField(ref _inputBackground, value);
    }

    [JsonPropertyName("inputBorder")]
    public string InputBorder
    {
        get => _inputBorder;
        set => SetField(ref _inputBorder, value);
    }

    [JsonPropertyName("inputFocusBorder")]
    public string InputFocusBorder
    {
        get => _inputFocusBorder;
        set => SetField(ref _inputFocusBorder, value);
    }

    [JsonPropertyName("inputText")]
    public string InputText
    {
        get => _inputText;
        set => SetField(ref _inputText, value);
    }

    // Project Tag
    [JsonPropertyName("projectTagBackground")]
    public string ProjectTagBackground
    {
        get => _projectTagBackground;
        set => SetField(ref _projectTagBackground, value);
    }

    [JsonPropertyName("projectTagBorder")]
    public string ProjectTagBorder
    {
        get => _projectTagBorder;
        set => SetField(ref _projectTagBorder, value);
    }

    [JsonPropertyName("projectTagText")]
    public string ProjectTagText
    {
        get => _projectTagText;
        set => SetField(ref _projectTagText, value);
    }

    // Title Bar
    [JsonPropertyName("titleBarBackground")]
    public string TitleBarBackground
    {
        get => _titleBarBackground;
        set => SetField(ref _titleBarBackground, value);
    }

    [JsonPropertyName("titleBarForeground")]
    public string TitleBarForeground
    {
        get => _titleBarForeground;
        set => SetField(ref _titleBarForeground, value);
    }

    [JsonPropertyName("titleBarButtonHover")]
    public string TitleBarButtonHover
    {
        get => _titleBarButtonHover;
        set => SetField(ref _titleBarButtonHover, value);
    }

    [JsonPropertyName("titleBarButtonPressed")]
    public string TitleBarButtonPressed
    {
        get => _titleBarButtonPressed;
        set => SetField(ref _titleBarButtonPressed, value);
    }

    // Settings Window
    [JsonPropertyName("settingsBackground")]
    public string SettingsBackground
    {
        get => _settingsBackground;
        set => SetField(ref _settingsBackground, value);
    }

    [JsonPropertyName("settingsSectionHeader")]
    public string SettingsSectionHeader
    {
        get => _settingsSectionHeader;
        set => SetField(ref _settingsSectionHeader, value);
    }

    [JsonPropertyName("settingsLabelText")]
    public string SettingsLabelText
    {
        get => _settingsLabelText;
        set => SetField(ref _settingsLabelText, value);
    }

    // Tab Control
    [JsonPropertyName("tabBackground")]
    public string TabBackground
    {
        get => _tabBackground;
        set => SetField(ref _tabBackground, value);
    }

    [JsonPropertyName("tabSelectedBackground")]
    public string TabSelectedBackground
    {
        get => _tabSelectedBackground;
        set => SetField(ref _tabSelectedBackground, value);
    }

    [JsonPropertyName("tabHoverBackground")]
    public string TabHoverBackground
    {
        get => _tabHoverBackground;
        set => SetField(ref _tabHoverBackground, value);
    }

    [JsonPropertyName("tabText")]
    public string TabText
    {
        get => _tabText;
        set => SetField(ref _tabText, value);
    }

    [JsonPropertyName("tabSelectedText")]
    public string TabSelectedText
    {
        get => _tabSelectedText;
        set => SetField(ref _tabSelectedText, value);
    }

    // Opacity Controls
    [JsonPropertyName("backgroundOpacity")]
    public double BackgroundOpacity
    {
        get => _backgroundOpacity;
        set
        {
            var clamped = Math.Clamp(value, 0.0, 1.0);
            SetField(ref _backgroundOpacity, clamped);
        }
    }

    [JsonPropertyName("timerOpacity")]
    public double TimerOpacity
    {
        get => _timerOpacity;
        set
        {
            var clamped = Math.Clamp(value, 0.0, 1.0);
            SetField(ref _timerOpacity, clamped);
        }
    }

    [JsonPropertyName("buttonOpacity")]
    public double ButtonOpacity
    {
        get => _buttonOpacity;
        set
        {
            var clamped = Math.Clamp(value, 0.0, 1.0);
            SetField(ref _buttonOpacity, clamped);
        }
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

    /// <summary>
    /// Creates a deep copy of this theme.
    /// </summary>
    public Theme Clone()
    {
        return new Theme
        {
            ThemeName = ThemeName,
            Author = Author,
            Version = Version,
            WindowBackground = WindowBackground,
            WindowForeground = WindowForeground,
            WindowBorder = WindowBorder,
            PrimaryText = PrimaryText,
            SecondaryText = SecondaryText,
            DisabledText = DisabledText,
            TimerText = TimerText,
            TimerBackground = TimerBackground,
            ButtonNormal = ButtonNormal,
            ButtonHover = ButtonHover,
            ButtonPressed = ButtonPressed,
            ButtonDisabled = ButtonDisabled,
            AccentPrimary = AccentPrimary,
            AccentSecondary = AccentSecondary,
            DangerColor = DangerColor,
            SuccessColor = SuccessColor,
            WarningColor = WarningColor,
            InputBackground = InputBackground,
            InputBorder = InputBorder,
            InputFocusBorder = InputFocusBorder,
            InputText = InputText,
            ProjectTagBackground = ProjectTagBackground,
            ProjectTagBorder = ProjectTagBorder,
            ProjectTagText = ProjectTagText,
            TitleBarBackground = TitleBarBackground,
            TitleBarForeground = TitleBarForeground,
            TitleBarButtonHover = TitleBarButtonHover,
            TitleBarButtonPressed = TitleBarButtonPressed,
            SettingsBackground = SettingsBackground,
            SettingsSectionHeader = SettingsSectionHeader,
            SettingsLabelText = SettingsLabelText,
            TabBackground = TabBackground,
            TabSelectedBackground = TabSelectedBackground,
            TabHoverBackground = TabHoverBackground,
            TabText = TabText,
            TabSelectedText = TabSelectedText,
            BackgroundOpacity = BackgroundOpacity,
            TimerOpacity = TimerOpacity,
            ButtonOpacity = ButtonOpacity
        };
    }
}
