namespace FocusTimer.Tests;

using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

/// <summary>Deterministic source palettes shared by serialization and rendered contrast tests.</summary>
public static class ThemeFixtures
{
    /// <summary>Gets all factory presets followed by difficult, valid imported snapshots.</summary>
    public static IEnumerable<Theme> All => new ThemeService().BuiltInThemes.Concat(Custom);

    /// <summary>Gets custom palettes that exercise source preservation and derived rendering.</summary>
    public static IEnumerable<Theme> Custom
    {
        get
        {
            yield return new Theme
            {
                ThemeName = "White on white",
                SettingsBackground = "#FFFFFF",
                PrimaryText = "#FFFFFF",
                InputBackground = "#FFFFFF",
                InputText = "#FFFFFF",
                SettingsLabelText = "#FFFFFF",
                SettingsSectionHeader = "#FFFFFF",
                AccentPrimary = "#FFFFFF",
                TabText = "#FFFFFF",
                TabHoverBackground = "#FFFFFF",
                TabSelectedBackground = "#FFFFFF",
                TabSelectedText = "#FFFFFF",
            };
            yield return new Theme
            {
                ThemeName = "Pale accents",
                SettingsBackground = "#FAFAFA",
                AccentPrimary = "#FFF8DD",
                DangerColor = "#FFE0E0",
                SuccessColor = "#E0FFE0",
                WarningColor = "#FFFFCC",
                InputBorder = "#10FFFFFF",
                TabSelectedBackground = "#FFF8DD",
                TabSelectedText = "#FFFFFF",
            };
            yield return new Theme
            {
                ThemeName = "Opposing surfaces",
                SettingsBackground = "#FFFFFF",
                PrimaryText = "#000000",
                InputBackground = "#000000",
                InputText = "#FFFFFF",
                TabHoverBackground = "#000000",
                TabSelectedBackground = "#777777",
                TabSelectedText = "#777777",
            };
            yield return new Theme
            {
                ThemeName = "Translucent",
                SettingsBackground = "#FFFFFF",
                PrimaryText = "#000000",
                InputBackground = "#E6000000",
                InputText = "#FFFFFF",
                TabHoverBackground = "#80000000",
                TabSelectedBackground = "#80FFFFFF",
                TabSelectedText = "#777777",
                BackgroundOpacity = 0.37,
                ButtonOpacity = 0.63,
                TimerOpacity = 0.81,
            };
            yield return new Theme
            {
                ThemeName = "Dark",
                Author = "Imported fixture",
                SettingsBackground = "#808080",
                InputBackground = "#80000000",
                AccentPrimary = "#FFEEDD",
                TabHoverBackground = "#FFFFFF",
                WindowBackground = "#223344",
                WidgetBlurMode = WidgetBlurModes.Off,
            };
        }
    }
}
