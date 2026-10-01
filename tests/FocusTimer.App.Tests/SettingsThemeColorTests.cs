namespace FocusTimer.App.Tests
{
    using System;
    using System.Collections.Generic;
    using Avalonia.Controls;
    using Avalonia.Media;
    using FocusTimer.App.Services;
    using FocusTimer.Core.Models;
    using FocusTimer.Core.Services;
    using Xunit;

    public class SettingsThemeColorTests
    {
        [Fact]
        public void ApplyTheme_UpdatesSettingsTextInputAndTabResourcesIndependently()
        {
            var manager = new ThemeManager();
            var resources = new ResourceDictionary();
            var theme = new Theme
            {
                PrimaryText = "#111111",
                SettingsSectionHeader = "#222222",
                SettingsLabelText = "#333333",
                DisabledText = "#444444",
                InputBackground = "#555555",
                InputBorder = "#666666",
                InputText = "#777777",
                TabBackground = "#888888",
                TabHoverBackground = "#999999",
                TabText = "#AAAAAA",
                TabSelectedBackground = "#BBBBBB",
                TabSelectedText = "#CCCCCC",
                ButtonNormal = "#DDDDDD",
            };

            manager.ApplyTheme(theme, resources);
            AssertSettingsResourcesMatch(theme, resources);
            Color widgetButtonColor = Assert.IsType<SolidColorBrush>(resources["ButtonNormalBrush"]).Color;

            theme.PrimaryText = "#EEEEEE";
            theme.InputText = "#121212";
            theme.TabSelectedText = "#343434";
            manager.ApplyTheme(theme, resources);

            AssertSettingsResourcesMatch(theme, resources);
            Assert.Equal(widgetButtonColor, Assert.IsType<SolidColorBrush>(resources["ButtonNormalBrush"]).Color);
        }

        [Fact]
        public void BuiltInThemes_KeepSettingsTextSelectionAndFocusReadable()
        {
            var service = new ThemeService();
            string[] themeNames = ["Dark", "Light", "Monokai", "Solarized Dark", "Nord", "Dracula", "High Contrast"];
            var failures = new List<string>();

            foreach (string name in themeNames)
            {
                Theme theme = service.GetBuiltInTheme(name)!;
                Color settingsSurface = Color.Parse(theme.SettingsBackground);
                Color inputSurface = Composite(Color.Parse(theme.InputBackground), settingsSurface);
                Color tabSurface = Color.Parse(theme.TabBackground);
                Color selectedSurface = Color.Parse(theme.TabSelectedBackground);

                Check(name, "body text", theme.PrimaryText, settingsSurface, 4.5, failures);
                Check(name, "section heading", theme.SettingsSectionHeader, settingsSurface, 4.5, failures);
                Check(name, "label text", theme.SettingsLabelText, settingsSurface, 4.5, failures);
                Check(name, "field text", theme.InputText, inputSurface, 4.5, failures);
                Check(name, "tab text", theme.TabText, tabSurface, 4.5, failures);
                Check(name, "hovered tab text", theme.TabText, Color.Parse(theme.TabHoverBackground), 4.5, failures);
                Check(name, "selected tab text", theme.TabSelectedText, selectedSurface, 4.5, failures);
                Check(name, "selected tab", theme.TabSelectedBackground, tabSurface, 3.0, failures);
                Check(name, "field focus", theme.InputFocusBorder, inputSurface, 3.0, failures);
                Check(name, "unselected tab focus", theme.InputFocusBorder, tabSurface, 3.0, failures);
            }

            Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
        }

        private static void AssertSettingsResourcesMatch(Theme theme, ResourceDictionary resources)
        {
            var colors = new Dictionary<string, string>
            {
                ["PrimaryTextColor"] = theme.PrimaryText,
                ["SettingsSectionHeaderColor"] = theme.SettingsSectionHeader,
                ["SettingsLabelTextColor"] = theme.SettingsLabelText,
                ["DisabledTextColor"] = theme.DisabledText,
                ["InputBackgroundColor"] = theme.InputBackground,
                ["InputBorderColor"] = theme.InputBorder,
                ["InputTextColor"] = theme.InputText,
                ["TabBackgroundColor"] = theme.TabBackground,
                ["TabHoverBackgroundColor"] = theme.TabHoverBackground,
                ["TabTextColor"] = theme.TabText,
                ["TabSelectedBackgroundColor"] = theme.TabSelectedBackground,
                ["TabSelectedTextColor"] = theme.TabSelectedText,
            };

            foreach ((string key, string hex) in colors)
            {
                Color expected = Color.Parse(hex);
                Assert.Equal(expected, resources[key]);
                Assert.Equal(expected, Assert.IsType<SolidColorBrush>(resources[key.Replace("Color", "Brush")]).Color);
            }
        }

        private static void Check(string themeName, string role, string foreground, Color background, double minimum, List<string> failures)
        {
            Check(themeName, role, Color.Parse(foreground), background, minimum, failures);
        }

        private static void Check(string themeName, string role, Color foreground, Color background, double minimum, List<string> failures)
        {
            double contrast = Contrast(foreground, background);
            if (contrast < minimum)
            {
                failures.Add($"{themeName} {role}: {contrast:F2}:1 (needs {minimum:F1}:1)");
            }
        }

        private static Color Composite(Color foreground, Color background)
        {
            double alpha = foreground.A / 255.0;
            byte Blend(byte front, byte back) => (byte)Math.Round((alpha * front) + ((1 - alpha) * back));
            return Color.FromRgb(
                Blend(foreground.R, background.R),
                Blend(foreground.G, background.G),
                Blend(foreground.B, background.B));
        }

        private static double Contrast(Color foreground, Color background)
        {
            double first = Luminance(foreground);
            double second = Luminance(background);
            return (Math.Max(first, second) + 0.05) / (Math.Min(first, second) + 0.05);
        }

        private static double Luminance(Color color) =>
            (0.2126 * Linearize(color.R)) +
            (0.7152 * Linearize(color.G)) +
            (0.0722 * Linearize(color.B));

        private static double Linearize(byte channel)
        {
            double value = channel / 255.0;
            return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }
    }
}
