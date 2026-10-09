namespace FocusTimer.App.Tests;

using Avalonia.Controls;
using Avalonia.Media;
using System.Globalization;
using FocusTimer.App.Services;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

public class DesktopPaletteContrastTests
{
    private static readonly System.Text.Json.JsonSerializerOptions EvidenceJsonOptions = new() { WriteIndented = true };
    private static readonly string[] FrostTextBrushes = ["DesktopTabTextBrush", "TabSelectedLabelBrush"];

    [Fact]
    public void TranslucentSelection_UsesTheActualFieldAndKeepsSelectionDistinct()
    {
        Theme theme = FocusTimer.Tests.ThemeFixtures.Custom.Single(t => t.ThemeName == "Translucent");
        var resources = new ResourceDictionary();
        new ThemeManager().ApplyTheme(theme, resources);
        Color field = Assert.IsType<SolidColorBrush>(resources["DesktopFieldBrush"]).Color;
        Color fill = Assert.IsType<SolidColorBrush>(resources["DesktopSelectionBrush"]).Color;
        Color text = Assert.IsType<SolidColorBrush>(resources["DesktopSelectedTextBrush"]).Color;
        Assert.True(ThemeContrast.Ratio(text, fill) >= 4.5, $"selection text {text} on field-composited fill {fill}");
        Assert.True(ThemeContrast.Ratio(fill, field) >= 3.0);
    }

    [Fact]
    public async Task DifficultImports_RoundTripTheirSourcePalettes()
    {
        var service = new ThemeService();
        string path = Path.Combine(Path.GetTempPath(), $"theme-fixtures-{Guid.NewGuid():N}.json");
        try
        {
            foreach (Theme theme in FocusTimer.Tests.ThemeFixtures.Custom)
            {
                await service.SaveThemeToFileAsync(theme, path);
                Theme imported = await service.LoadThemeFromFileAsync(path);
                Assert.Equal(System.Text.Json.JsonSerializer.Serialize(theme), System.Text.Json.JsonSerializer.Serialize(imported));
                Assert.True(service.ValidateTheme(imported));
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void EveryBuiltInTheme_DesktopTextAndStatusColorsAreReadableOnEveryControlState()
    {
        var rows = new List<string> { "theme\tforeground\tbackground\tratio\tthreshold" };
        foreach (Theme theme in FocusTimer.Tests.ThemeFixtures.All)
        {
            Check(theme, rows);
        }

        string? folder = Environment.GetEnvironmentVariable("FOCUSTIMER_THEME_EVIDENCE_DIR");
        if (folder != null)
        {
            Directory.CreateDirectory(folder);
            File.WriteAllLines(Path.Combine(folder, "derived-contrast.tsv"), rows);
            File.WriteAllText(Path.Combine(folder, "source-palettes.json"), System.Text.Json.JsonSerializer.Serialize(
                new ThemeService().BuiltInThemes, EvidenceJsonOptions));
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

    private static void Check(Theme theme, List<string>? evidence = null)
    {
        var resources = new ResourceDictionary();
        new ThemeManager().ApplyTheme(theme, resources);
        Color Brush(string key) => Assert.IsType<SolidColorBrush>(resources[key]).Color;
        void Measure(string foreground, string background, double threshold)
        {
            double ratio = ThemeContrast.Ratio(Brush(foreground), Brush(background));
            Assert.True(ratio >= threshold, $"{theme.ThemeName}: {foreground} on {background} = {ratio:F3}:1");
            evidence?.Add($"{theme.ThemeName}\t{foreground}\t{background}\t{ratio.ToString("F4", CultureInfo.InvariantCulture)}\t{threshold.ToString(CultureInfo.InvariantCulture)}");
        }

        string[] backgrounds = ["DesktopShellBrush", "DesktopCardBrush", "DesktopFieldBrush", "DesktopControlHoverBrush", "DesktopControlPressedBrush"];
        string[] foregrounds = ["DesktopTextBrush", "DesktopSecondaryTextBrush", "DesktopLabelBrush", "DesktopHeadingBrush", "DesktopInputTextBrush", "DesktopWarningBrush", "DesktopDangerBrush", "DesktopSuccessBrush"];
        foreach (string foreground in foregrounds)
        {
            foreach (string background in backgrounds)
            {
                Measure(foreground, background, 4.5);
            }
        }

        var frost = Assert.IsType<SolidColorBrush>(resources["DesktopShellFrostBrush"]);
        Assert.Equal(ThemeManager.DesktopShellFrostOpacity, frost.Opacity);
        foreach (Color desktop in new[] { Colors.Black, Colors.White })
        {
            byte Channel(byte foreground, byte background) => (byte)Math.Round(foreground * frost.Opacity + background * (1 - frost.Opacity));
            Color renderedShell = Color.FromRgb(Channel(frost.Color.R, desktop.R), Channel(frost.Color.G, desktop.G), Channel(frost.Color.B, desktop.B));
            foreach (string foreground in foregrounds.Concat(FrostTextBrushes))
            {
                double ratio = ThemeContrast.Ratio(Brush(foreground), renderedShell);
                Assert.True(ratio >= 4.5, $"{theme.ThemeName}: {foreground} on frost over {desktop} = {ratio:F3}:1");
                evidence?.Add($"{theme.ThemeName}\t{foreground}\tfrost over {desktop}\t{ratio.ToString("F4", CultureInfo.InvariantCulture)}\t4.5");
            }

            foreach (string indicator in new[] { "DesktopBorderBrush", "DesktopFocusBrush", "DesktopActionFocusBrush", "DesktopTabSelectedBrush", "DesktopSelectionBrush" })
            {
                Assert.True(ThemeContrast.Ratio(Brush(indicator), renderedShell) >= 3, $"{theme.ThemeName}: {indicator} on frost over {desktop}");
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
            Color shell = Assert.IsType<SolidColorBrush>(resources["DesktopShellBrush"]).Color;
            Assert.True(ThemeContrast.Ratio(indicator, shell) >= 3.0, $"{theme.ThemeName}: {key}");
            Measure(key, "DesktopShellBrush", 3);
        }

        Measure("TabSelectedLabelBrush", "DesktopShellBrush", 4.5);
        Measure("DesktopTabTextBrush", "DesktopTabHoverBrush", 4.5);
        Measure("DesktopTabFocusBrush", "DesktopTabHoverBrush", 3);
        Measure("DesktopTabFocusBrush", "DesktopShellBrush", 3);
        Measure("DesktopSelectedTextBrush", "DesktopSelectionBrush", 4.5);
        Measure("DesktopSelectionBrush", "DesktopFieldBrush", 3);
        Measure("DesktopSelectionBrush", "DesktopShellBrush", 3);
        Measure("DesktopNotificationBodyBrush", "DesktopShellBrush", 4.5);
        foreach (string action in new[] { "DesktopPrimaryAction", "DesktopDestructiveAction" })
        {
            foreach (string state in new[] { "", "Hover", "Pressed" })
            {
                Measure(action + "TextBrush", action + state + "Brush", 4.5);
                Measure("DesktopActionFocusBrush", action + state + "Brush", 3);
            }
        }
    }
}
