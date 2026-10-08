namespace FocusTimer.App.Tests;

using Avalonia.Controls;
using Avalonia.Media;
using FocusTimer.App.Services;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

public class DesktopPaletteContrastTests
{
    [Fact]
    public void EveryBuiltInTheme_DesktopTextAndStatusColorsAreReadableOnEveryControlState()
    {
        foreach (Theme theme in new ThemeService().BuiltInThemes)
        {
            Check(theme);
        }
    }

    [Fact]
    public void ImportedLowContrastTheme_IsReadableWithoutChangingItsStoredValuesOrWidgetColors()
    {
        var theme = new Theme
        {
            SettingsBackground = "#FFFFFF",
            PrimaryText = "#FFFFFF",
            SettingsLabelText = "#FFFFFF",
            SettingsSectionHeader = "#FFFFFF",
            InputBackground = "#FFFFFF",
            InputText = "#FFFFFF",
            WarningColor = "#FFFF00",
            DangerColor = "#FFAAAA",
            SuccessColor = "#AAFFAA",
        };
        string before = System.Text.Json.JsonSerializer.Serialize(theme);
        Check(theme);
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(theme));
    }

    private static void Check(Theme theme)
    {
        var resources = new ResourceDictionary();
        new ThemeManager().ApplyTheme(theme, resources);
        string[] backgrounds = ["SettingsBackgroundBrush", "DesktopCardBrush", "DesktopFieldBrush", "DesktopControlHoverBrush", "DesktopControlPressedBrush"];
        string[] foregrounds = ["DesktopTextBrush", "DesktopSecondaryTextBrush", "DesktopLabelBrush", "DesktopHeadingBrush", "DesktopInputTextBrush", "DesktopWarningBrush", "DesktopDangerBrush", "DesktopSuccessBrush"];
        foreach (string foreground in foregrounds)
        {
            foreach (string background in backgrounds)
            {
                Color front = Assert.IsType<SolidColorBrush>(resources[foreground]).Color;
                Color back = Assert.IsType<SolidColorBrush>(resources[background]).Color;
                Assert.True(ThemeContrast.Ratio(front, back) >= 4.5, $"{theme.ThemeName}: {foreground} on {background}");
            }
        }

        Assert.Equal(Color.Parse(theme.ButtonNormal), Assert.IsType<SolidColorBrush>(resources["ButtonNormalBrush"]).Color);
        Color actionText = Assert.IsType<SolidColorBrush>(resources["DesktopPrimaryActionTextBrush"]).Color;
        Color actionFocus = Assert.IsType<SolidColorBrush>(resources["DesktopActionFocusBrush"]).Color;
        foreach (string background in new[] { "DesktopPrimaryActionBrush", "DesktopPrimaryActionHoverBrush", "DesktopPrimaryActionPressedBrush" })
        {
            Color fill = Assert.IsType<SolidColorBrush>(resources[background]).Color;
            Assert.True(ThemeContrast.Ratio(actionText, fill) >= 4.5, $"{theme.ThemeName}: action text on {background}");
            Assert.True(ThemeContrast.Ratio(actionFocus, fill) >= 3.0, $"{theme.ThemeName}: action focus on {background}");
        }

        foreach (string background in backgrounds)
        {
            Color fill = Assert.IsType<SolidColorBrush>(resources[background]).Color;
            Assert.True(ThemeContrast.Ratio(actionFocus, fill) >= 3.0, $"{theme.ThemeName}: action focus on {background}");
        }

        Color dangerText = Assert.IsType<SolidColorBrush>(resources["DesktopDestructiveActionTextBrush"]).Color;
        foreach (string background in new[] { "DesktopDestructiveActionBrush", "DesktopDestructiveActionHoverBrush", "DesktopDestructiveActionPressedBrush" })
        {
            Color fill = Assert.IsType<SolidColorBrush>(resources[background]).Color;
            Assert.True(ThemeContrast.Ratio(dangerText, fill) >= 4.5, $"{theme.ThemeName}: delete text on {background}");
            Assert.True(ThemeContrast.Ratio(actionFocus, fill) >= 3.0, $"{theme.ThemeName}: delete focus on {background}");
        }

        foreach (string key in new[] { "DesktopBorderBrush", "DesktopFocusBrush", "DesktopTabSelectedBrush" })
        {
            Color indicator = Assert.IsType<SolidColorBrush>(resources[key]).Color;
            Color shell = Color.Parse(theme.SettingsBackground);
            Assert.True(ThemeContrast.Ratio(indicator, shell) >= 3.0, $"{theme.ThemeName}: {key}");
        }
    }
}
