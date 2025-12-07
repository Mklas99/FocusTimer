using System.Text.Json;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

namespace FocusTimer.Core.Services;

/// <summary>
/// Service for managing application themes.
/// </summary>
public class ThemeService : IThemeService
{
    private Theme _currentTheme;
    private readonly List<Theme> _builtInThemes;

    public Theme CurrentTheme => _currentTheme;
    public IReadOnlyList<Theme> BuiltInThemes => _builtInThemes.AsReadOnly();

    public ThemeService()
    {
        _builtInThemes = CreateBuiltInThemes();
        _currentTheme = _builtInThemes[0].Clone(); // Default to Dark theme
    }

    public void ApplyTheme(Theme theme)
    {
        _currentTheme = theme.Clone();
    }

    public async Task<Theme> LoadThemeFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Theme file not found: {filePath}");

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var theme = JsonSerializer.Deserialize<Theme>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            });

            if (theme == null)
                throw new InvalidOperationException("Failed to deserialize theme file.");

            if (!ValidateTheme(theme))
                throw new InvalidOperationException("Theme file is missing required properties.");

            return theme;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid theme file format: {ex.Message}", ex);
        }
    }

    public async Task SaveThemeToFileAsync(Theme theme, string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(theme, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await File.WriteAllTextAsync(filePath, json);
    }

    public Theme? GetBuiltInTheme(string themeName)
    {
        return _builtInThemes.FirstOrDefault(t => 
            t.ThemeName.Equals(themeName, StringComparison.OrdinalIgnoreCase))?.Clone();
    }

    public void ResetToDefault()
    {
        _currentTheme = _builtInThemes[0].Clone();
    }

    public bool ValidateTheme(Theme theme)
    {
        // Check that all required color properties are not null or empty
        return !string.IsNullOrWhiteSpace(theme.WindowBackground) &&
               !string.IsNullOrWhiteSpace(theme.PrimaryText) &&
               !string.IsNullOrWhiteSpace(theme.ButtonNormal) &&
               !string.IsNullOrWhiteSpace(theme.AccentPrimary);
    }

    private static List<Theme> CreateBuiltInThemes()
    {
        var themes = new List<Theme>();

        // Dark Theme (Default)
        themes.Add(new Theme
        {
            ThemeName = "Dark",
            Author = "FocusTimer",
            Version = "1.0",
            WindowBackground = "#1E1E1E",
            WindowForeground = "#FFFFFF",
            WindowBorder = "#3F3F46",
            PrimaryText = "#F5F5F5",
            SecondaryText = "#CCCCCC",
            DisabledText = "#666666",
            TimerText = "#FFFFFF",
            TimerBackground = "#00000000",
            ButtonNormal = "#4CDEFFBD",
            ButtonHover = "#5DEFFCE7",
            ButtonPressed = "#3BBD99AC",
            ButtonDisabled = "#40FFFFFF",
            AccentPrimary = "#0078D7",
            AccentSecondary = "#005A9E",
            DangerColor = "#D9534F",
            SuccessColor = "#4CAF50",
            WarningColor = "#FFA726",
            InputBackground = "#1AFFFFFF",
            InputBorder = "#35FFFFFF",
            InputFocusBorder = "#0078D7",
            InputText = "#FFFFFF",
            ProjectTagBackground = "#1AFFFFFF",
            ProjectTagBorder = "#35FFFFFF",
            ProjectTagText = "#FFFFFF",
            TitleBarBackground = "#00000000",
            TitleBarForeground = "#CCCCCC",
            TitleBarButtonHover = "#1A000000",
            TitleBarButtonPressed = "#33000000",
            SettingsBackground = "#2D2D30",
            SettingsSectionHeader = "#F5F5F5",
            SettingsLabelText = "#CCCCCC",
            TabBackground = "#2D2D30",
            TabSelectedBackground = "#007ACC",
            TabHoverBackground = "#1E1E1E",
            TabText = "#CCCCCC",
            TabSelectedText = "#FFFFFF",
            BackgroundOpacity = 0.95,
            TimerOpacity = 1.0,
            ButtonOpacity = 0.9
        });

        // Light Theme
        themes.Add(new Theme
        {
            ThemeName = "Light",
            Author = "FocusTimer",
            Version = "1.0",
            WindowBackground = "#F5F5F5",
            WindowForeground = "#000000",
            WindowBorder = "#CCCCCC",
            PrimaryText = "#1E1E1E",
            SecondaryText = "#5A5A5A",
            DisabledText = "#AAAAAA",
            TimerText = "#1E1E1E",
            TimerBackground = "#00000000",
            ButtonNormal = "#0078D7",
            ButtonHover = "#1084DD",
            ButtonPressed = "#006CBE",
            ButtonDisabled = "#E0E0E0",
            AccentPrimary = "#0078D7",
            AccentSecondary = "#005A9E",
            DangerColor = "#D32F2F",
            SuccessColor = "#388E3C",
            WarningColor = "#F57C00",
            InputBackground = "#FFFFFF",
            InputBorder = "#CCCCCC",
            InputFocusBorder = "#0078D7",
            InputText = "#1E1E1E",
            ProjectTagBackground = "#E8E8E8",
            ProjectTagBorder = "#CCCCCC",
            ProjectTagText = "#1E1E1E",
            TitleBarBackground = "#00000000",
            TitleBarForeground = "#5A5A5A",
            TitleBarButtonHover = "#1A000000",
            TitleBarButtonPressed = "#33000000",
            SettingsBackground = "#FFFFFF",
            SettingsSectionHeader = "#1E1E1E",
            SettingsLabelText = "#5A5A5A",
            TabBackground = "#F5F5F5",
            TabSelectedBackground = "#0078D7",
            TabHoverBackground = "#E8E8E8",
            TabText = "#5A5A5A",
            TabSelectedText = "#FFFFFF",
            BackgroundOpacity = 0.98,
            TimerOpacity = 1.0,
            ButtonOpacity = 0.95
        });

        // Monokai Theme
        themes.Add(new Theme
        {
            ThemeName = "Monokai",
            Author = "FocusTimer",
            Version = "1.0",
            WindowBackground = "#272822",
            WindowForeground = "#F8F8F2",
            WindowBorder = "#3E3D32",
            PrimaryText = "#F8F8F2",
            SecondaryText = "#CFCFC2",
            DisabledText = "#75715E",
            TimerText = "#66D9EF",
            TimerBackground = "#00000000",
            ButtonNormal = "#A6E22E",
            ButtonHover = "#B7F33F",
            ButtonPressed = "#8FBF1D",
            ButtonDisabled = "#75715E",
            AccentPrimary = "#FD971F",
            AccentSecondary = "#E6831E",
            DangerColor = "#F92672",
            SuccessColor = "#A6E22E",
            WarningColor = "#E6DB74",
            InputBackground = "#3E3D32",
            InputBorder = "#75715E",
            InputFocusBorder = "#FD971F",
            InputText = "#F8F8F2",
            ProjectTagBackground = "#3E3D32",
            ProjectTagBorder = "#75715E",
            ProjectTagText = "#F8F8F2",
            TitleBarBackground = "#00000000",
            TitleBarForeground = "#CFCFC2",
            TitleBarButtonHover = "#1AFFFFFF",
            TitleBarButtonPressed = "#33FFFFFF",
            SettingsBackground = "#1E1F1C",
            SettingsSectionHeader = "#F8F8F2",
            SettingsLabelText = "#CFCFC2",
            TabBackground = "#272822",
            TabSelectedBackground = "#FD971F",
            TabHoverBackground = "#3E3D32",
            TabText = "#CFCFC2",
            TabSelectedText = "#272822",
            BackgroundOpacity = 0.92,
            TimerOpacity = 1.0,
            ButtonOpacity = 0.88
        });

        // Solarized Dark Theme
        themes.Add(new Theme
        {
            ThemeName = "Solarized Dark",
            Author = "FocusTimer",
            Version = "1.0",
            WindowBackground = "#002B36",
            WindowForeground = "#839496",
            WindowBorder = "#073642",
            PrimaryText = "#93A1A1",
            SecondaryText = "#657B83",
            DisabledText = "#586E75",
            TimerText = "#2AA198",
            TimerBackground = "#00000000",
            ButtonNormal = "#859900",
            ButtonHover = "#9FB300",
            ButtonPressed = "#6B7A00",
            ButtonDisabled = "#586E75",
            AccentPrimary = "#268BD2",
            AccentSecondary = "#2075C7",
            DangerColor = "#DC322F",
            SuccessColor = "#859900",
            WarningColor = "#B58900",
            InputBackground = "#073642",
            InputBorder = "#586E75",
            InputFocusBorder = "#268BD2",
            InputText = "#93A1A1",
            ProjectTagBackground = "#073642",
            ProjectTagBorder = "#586E75",
            ProjectTagText = "#93A1A1",
            TitleBarBackground = "#00000000",
            TitleBarForeground = "#657B83",
            TitleBarButtonHover = "#1AFFFFFF",
            TitleBarButtonPressed = "#33FFFFFF",
            SettingsBackground = "#073642",
            SettingsSectionHeader = "#93A1A1",
            SettingsLabelText = "#657B83",
            TabBackground = "#002B36",
            TabSelectedBackground = "#268BD2",
            TabHoverBackground = "#073642",
            TabText = "#657B83",
            TabSelectedText = "#FDF6E3",
            BackgroundOpacity = 0.94,
            TimerOpacity = 1.0,
            ButtonOpacity = 0.9
        });

        // Nord Theme
        themes.Add(new Theme
        {
            ThemeName = "Nord",
            Author = "FocusTimer",
            Version = "1.0",
            WindowBackground = "#2E3440",
            WindowForeground = "#ECEFF4",
            WindowBorder = "#3B4252",
            PrimaryText = "#ECEFF4",
            SecondaryText = "#D8DEE9",
            DisabledText = "#4C566A",
            TimerText = "#88C0D0",
            TimerBackground = "#00000000",
            ButtonNormal = "#A3BE8C",
            ButtonHover = "#B1CC9D",
            ButtonPressed = "#8FA876",
            ButtonDisabled = "#4C566A",
            AccentPrimary = "#5E81AC",
            AccentSecondary = "#4C6A9C",
            DangerColor = "#BF616A",
            SuccessColor = "#A3BE8C",
            WarningColor = "#EBCB8B",
            InputBackground = "#3B4252",
            InputBorder = "#4C566A",
            InputFocusBorder = "#5E81AC",
            InputText = "#ECEFF4",
            ProjectTagBackground = "#3B4252",
            ProjectTagBorder = "#4C566A",
            ProjectTagText = "#ECEFF4",
            TitleBarBackground = "#00000000",
            TitleBarForeground = "#D8DEE9",
            TitleBarButtonHover = "#1AFFFFFF",
            TitleBarButtonPressed = "#33FFFFFF",
            SettingsBackground = "#3B4252",
            SettingsSectionHeader = "#ECEFF4",
            SettingsLabelText = "#D8DEE9",
            TabBackground = "#2E3440",
            TabSelectedBackground = "#5E81AC",
            TabHoverBackground = "#3B4252",
            TabText = "#D8DEE9",
            TabSelectedText = "#ECEFF4",
            BackgroundOpacity = 0.93,
            TimerOpacity = 1.0,
            ButtonOpacity = 0.89
        });

        // Dracula Theme
        themes.Add(new Theme
        {
            ThemeName = "Dracula",
            Author = "FocusTimer",
            Version = "1.0",
            WindowBackground = "#282A36",
            WindowForeground = "#F8F8F2",
            WindowBorder = "#44475A",
            PrimaryText = "#F8F8F2",
            SecondaryText = "#BFBFBF",
            DisabledText = "#6272A4",
            TimerText = "#8BE9FD",
            TimerBackground = "#00000000",
            ButtonNormal = "#50FA7B",
            ButtonHover = "#6BFB8C",
            ButtonPressed = "#3FE96A",
            ButtonDisabled = "#6272A4",
            AccentPrimary = "#BD93F9",
            AccentSecondary = "#A77FE8",
            DangerColor = "#FF5555",
            SuccessColor = "#50FA7B",
            WarningColor = "#F1FA8C",
            InputBackground = "#44475A",
            InputBorder = "#6272A4",
            InputFocusBorder = "#BD93F9",
            InputText = "#F8F8F2",
            ProjectTagBackground = "#44475A",
            ProjectTagBorder = "#6272A4",
            ProjectTagText = "#F8F8F2",
            TitleBarBackground = "#00000000",
            TitleBarForeground = "#BFBFBF",
            TitleBarButtonHover = "#1AFFFFFF",
            TitleBarButtonPressed = "#33FFFFFF",
            SettingsBackground = "#44475A",
            SettingsSectionHeader = "#F8F8F2",
            SettingsLabelText = "#BFBFBF",
            TabBackground = "#282A36",
            TabSelectedBackground = "#BD93F9",
            TabHoverBackground = "#44475A",
            TabText = "#BFBFBF",
            TabSelectedText = "#F8F8F2",
            BackgroundOpacity = 0.96,
            TimerOpacity = 1.0,
            ButtonOpacity = 0.92
        });

        // High Contrast Theme
        themes.Add(new Theme
        {
            ThemeName = "High Contrast",
            Author = "FocusTimer",
            Version = "1.0",
            WindowBackground = "#000000",
            WindowForeground = "#FFFFFF",
            WindowBorder = "#FFFFFF",
            PrimaryText = "#FFFFFF",
            SecondaryText = "#FFFF00",
            DisabledText = "#808080",
            TimerText = "#00FFFF",
            TimerBackground = "#00000000",
            ButtonNormal = "#00FF00",
            ButtonHover = "#00FF00",
            ButtonPressed = "#008000",
            ButtonDisabled = "#808080",
            AccentPrimary = "#00FFFF",
            AccentSecondary = "#0080FF",
            DangerColor = "#FF0000",
            SuccessColor = "#00FF00",
            WarningColor = "#FFFF00",
            InputBackground = "#000000",
            InputBorder = "#FFFFFF",
            InputFocusBorder = "#00FFFF",
            InputText = "#FFFFFF",
            ProjectTagBackground = "#000000",
            ProjectTagBorder = "#FFFFFF",
            ProjectTagText = "#FFFFFF",
            TitleBarBackground = "#00000000",
            TitleBarForeground = "#FFFF00",
            TitleBarButtonHover = "#40FFFFFF",
            TitleBarButtonPressed = "#80FFFFFF",
            SettingsBackground = "#000000",
            SettingsSectionHeader = "#FFFFFF",
            SettingsLabelText = "#FFFF00",
            TabBackground = "#000000",
            TabSelectedBackground = "#00FFFF",
            TabHoverBackground = "#1A1A1A",
            TabText = "#FFFF00",
            TabSelectedText = "#000000",
            BackgroundOpacity = 1.0,
            TimerOpacity = 1.0,
            ButtonOpacity = 1.0
        });

        return themes;
    }
}
