using Avalonia;
using Avalonia.Media;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

namespace FocusTimer.App.Services;

/// <summary>
/// Manages theme application by updating Avalonia's resource dictionary at runtime.
/// </summary>
public class ThemeManager
{
    /// <summary>
    /// Applies a theme by updating all color resources in the application.
    /// </summary>
    public void ApplyTheme(Theme theme)
    {
        if (Application.Current == null)
            return;

        var resources = Application.Current.Resources;

        // Store opacity values as resources
        resources["BackgroundOpacity"] = theme.BackgroundOpacity;
        resources["TimerOpacity"] = theme.TimerOpacity;
        resources["ButtonOpacity"] = theme.ButtonOpacity;

        // Window Colors (with background opacity applied to brush, but transparent if opacity is 0)
        if (theme.BackgroundOpacity <= 0)
        {
            resources["WindowBackgroundBrush"] = Avalonia.Media.Brushes.Transparent;
        }
        else
        {
            UpdateColorResource(resources, "WindowBackgroundColor", theme.WindowBackground, theme.BackgroundOpacity);
            resources["WindowBackgroundBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(theme.WindowBackground), theme.BackgroundOpacity);
        }
        
        UpdateColorResource(resources, "WindowForegroundColor", theme.WindowForeground);
        UpdateColorResource(resources, "WindowBorderColor", theme.WindowBorder);

        // Text Colors
        UpdateColorResource(resources, "PrimaryTextColor", theme.PrimaryText);
        UpdateColorResource(resources, "SecondaryTextColor", theme.SecondaryText);
        UpdateColorResource(resources, "DisabledTextColor", theme.DisabledText);

        // Timer Display (with timer opacity applied to text brush)
        UpdateColorResource(resources, "TimerTextColor", theme.TimerText, theme.TimerOpacity);
        UpdateColorResource(resources, "TimerBackgroundColor", theme.TimerBackground);

        // Buttons & Controls (with button opacity applied to brushes)
        UpdateColorResource(resources, "ButtonNormalColor", theme.ButtonNormal, theme.ButtonOpacity);
        UpdateColorResource(resources, "ButtonHoverColor", theme.ButtonHover, theme.ButtonOpacity);
        UpdateColorResource(resources, "ButtonPressedColor", theme.ButtonPressed, theme.ButtonOpacity);
        UpdateColorResource(resources, "ButtonDisabledColor", theme.ButtonDisabled);

        // Accents
        UpdateColorResource(resources, "AccentPrimaryColor", theme.AccentPrimary);
        UpdateColorResource(resources, "AccentSecondaryColor", theme.AccentSecondary);
        UpdateColorResource(resources, "DangerColor", theme.DangerColor);
        UpdateColorResource(resources, "SuccessColor", theme.SuccessColor);
        UpdateColorResource(resources, "WarningColor", theme.WarningColor);

        // Input Controls
        UpdateColorResource(resources, "InputBackgroundColor", theme.InputBackground);
        UpdateColorResource(resources, "InputBorderColor", theme.InputBorder);
        UpdateColorResource(resources, "InputFocusBorderColor", theme.InputFocusBorder);
        UpdateColorResource(resources, "InputTextColor", theme.InputText);

        // Project Tag
        UpdateColorResource(resources, "ProjectTagBackgroundColor", theme.ProjectTagBackground);
        UpdateColorResource(resources, "ProjectTagBorderColor", theme.ProjectTagBorder);
        UpdateColorResource(resources, "ProjectTagTextColor", theme.ProjectTagText);

        // Title Bar
        UpdateColorResource(resources, "TitleBarBackgroundColor", theme.TitleBarBackground);
        UpdateColorResource(resources, "TitleBarForegroundColor", theme.TitleBarForeground);
        UpdateColorResource(resources, "TitleBarButtonHoverColor", theme.TitleBarButtonHover);
        UpdateColorResource(resources, "TitleBarButtonPressedColor", theme.TitleBarButtonPressed);

        // Settings Window
        UpdateColorResource(resources, "SettingsBackgroundColor", theme.SettingsBackground);
        UpdateColorResource(resources, "SettingsSectionHeaderColor", theme.SettingsSectionHeader);
        UpdateColorResource(resources, "SettingsLabelTextColor", theme.SettingsLabelText);

        // Tab Control
        UpdateColorResource(resources, "TabBackgroundColor", theme.TabBackground);
        UpdateColorResource(resources, "TabSelectedBackgroundColor", theme.TabSelectedBackground);
        UpdateColorResource(resources, "TabHoverBackgroundColor", theme.TabHoverBackground);
        UpdateColorResource(resources, "TabTextColor", theme.TabText);
        UpdateColorResource(resources, "TabSelectedTextColor", theme.TabSelectedText);
    }

    private void UpdateColorResource(Avalonia.Controls.IResourceDictionary resources, string key, string colorHex, double opacity = 1.0)
    {
        try
        {
            var color = Color.Parse(colorHex);
            resources[key] = color;

            // Also update the corresponding brush with opacity baked in
            var brushKey = key.Replace("Color", "Brush");
            if (resources.ContainsKey(brushKey))
            {
                resources[brushKey] = new SolidColorBrush(color, opacity);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't crash the application
            _logWriter?.LogError($"Failed to parse color '{colorHex}' for key '{key}': {ex.Message}", ex);
        }
    }

    private readonly ILogWriter? _logWriter;

    public ThemeManager(ILogWriter? logWriter = null)
    {
        _logWriter = logWriter;
    }

    /// <summary>
    /// Initializes the resource dictionary with default theme color keys.
    /// This should be called once during app startup.
    /// </summary>
    public void InitializeThemeResources()
    {
        if (Application.Current == null)
            return;

        var resources = Application.Current.Resources;
        var defaultTheme = new Theme(); // Uses default values

        // Initialize all color resources
        ApplyTheme(defaultTheme);
    }
}
