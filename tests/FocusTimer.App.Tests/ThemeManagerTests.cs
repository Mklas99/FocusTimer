namespace FocusTimer.App.Tests
{
    using Avalonia.Controls;
    using Avalonia.Media;
    using FocusTimer.App.Services;
    using FocusTimer.Core.Models;
    using Xunit;

    public class ThemeManagerTests
    {
        [Fact]
        public void ApplyTheme_GivenThemeAndResourceDictionary_PopulatesAllExpectedResources()
        {
            var manager = new ThemeManager();
            var dict = new ResourceDictionary();
            var theme = new Theme
            {
                WindowBackground = "#1E1E1E",
                PrimaryText = "#F5F5F5",
                AccentPrimary = "#0078D7",
                ButtonNormal = "#00FF00",
                BackgroundOpacity = 0.9,
                TimerOpacity = 0.8,
                ButtonOpacity = 0.7,
                WidgetBaseOpacity = 0.6,
            };

            manager.ApplyTheme(theme, dict);

            Assert.True(dict.ContainsKey("WindowBackgroundBrush"));
            Assert.True(dict.ContainsKey("PrimaryTextBrush"));
            Assert.True(dict.ContainsKey("AccentPrimaryBrush"));
            Assert.True(dict.ContainsKey("ButtonNormalBrush"));
            Assert.True(dict.ContainsKey("FocusRingBrush"));
            Assert.True(dict.ContainsKey("BorderSubtleBrush"));
            Assert.True(dict.ContainsKey("SurfaceSubtleBrush"));
            Assert.True(dict.ContainsKey("ActionPrimaryBrush"));
            Assert.True(dict.ContainsKey("WidgetBaseLayerBrush"));

            var bgBrush = Assert.IsType<SolidColorBrush>(dict["WindowBackgroundBrush"]);
            Assert.Equal(0.9, bgBrush.Opacity);

            var focusRingBrush = Assert.IsType<SolidColorBrush>(dict["FocusRingBrush"]);
            Assert.Equal(Color.Parse("#0078D7"), focusRingBrush.Color);
        }

        [Fact]
        public void ApplyTheme_GivenZeroBackgroundOpacity_SetsTransparentWindowBackgroundBrush()
        {
            var manager = new ThemeManager();
            var dict = new ResourceDictionary();
            var theme = new Theme
            {
                WindowBackground = "#1E1E1E",
                BackgroundOpacity = 0.0,
            };

            manager.ApplyTheme(theme, dict);

            var bgBrush = Assert.IsAssignableFrom<IBrush>(dict["WindowBackgroundBrush"]);
            Assert.Equal(Brushes.Transparent, bgBrush);
        }

        [Fact]
        public void ApplyTheme_GivenHighContrastTheme_AppliesHighContrastBrushes()
        {
            var manager = new ThemeManager();
            var dict = new ResourceDictionary();
            var themeService = new Core.Services.ThemeService();
            var highContrast = themeService.GetBuiltInTheme("High Contrast");

            Assert.NotNull(highContrast);
            manager.ApplyTheme(highContrast!, dict);

            var bgBrush = Assert.IsType<SolidColorBrush>(dict["WindowBackgroundBrush"]);
            Assert.Equal(Color.Parse("#000000"), bgBrush.Color);
            Assert.Equal(1.0, bgBrush.Opacity);

            var focusRingBrush = Assert.IsType<SolidColorBrush>(dict["FocusRingBrush"]);
            Assert.Equal(Color.Parse("#00FFFF"), focusRingBrush.Color);

            var textBrush = Assert.IsType<SolidColorBrush>(dict["PrimaryTextBrush"]);
            Assert.Equal(Color.Parse("#FFFFFF"), textBrush.Color);
        }

        [Fact]
        public void ApplyTheme_GivenFullOpacity_ProducesSolidFallbackBrush()
        {
            var manager = new ThemeManager();
            var dict = new ResourceDictionary();
            var theme = new Theme
            {
                WindowBackground = "#1E1E1E",
                BackgroundOpacity = 1.0,
            };

            manager.ApplyTheme(theme, dict);

            var bgBrush = Assert.IsType<SolidColorBrush>(dict["WindowBackgroundBrush"]);
            Assert.Equal(1.0, bgBrush.Opacity);
            Assert.Equal(Color.Parse("#1E1E1E"), bgBrush.Color);
        }

        [Fact]
        public void ApplyTheme_GivenAllSevenBuiltInThemes_SuccessfullyAppliesWithoutException()
        {
            var manager = new ThemeManager();
            var service = new Core.Services.ThemeService();

            var expectedThemes = new[] { "Dark", "Light", "Monokai", "Solarized Dark", "Nord", "Dracula", "High Contrast" };

            foreach (var name in expectedThemes)
            {
                var theme = service.GetBuiltInTheme(name);
                Assert.NotNull(theme);

                var dict = new ResourceDictionary();
                manager.ApplyTheme(theme!, dict);

                Assert.True(dict.ContainsKey("WindowBackgroundBrush"));
                Assert.True(dict.ContainsKey("PrimaryTextBrush"));
                Assert.True(dict.ContainsKey("AccentPrimaryBrush"));
                Assert.True(dict.ContainsKey("FocusRingBrush"));
                Assert.True(dict.ContainsKey("ActionPrimaryBrush"));
            }
        }

        [Fact]
        public async Task ApplyTheme_GivenExportedAndReimportedTheme_AppliesEquivalentResources()
        {
            var manager = new ThemeManager();
            var service = new Core.Services.ThemeService();
            var tempFile = Path.Combine(Path.GetTempPath(), $"theme_test_{Guid.NewGuid():N}.fttheme");

            try
            {
                var originalTheme = service.GetBuiltInTheme("Dracula")!;
                await service.SaveThemeToFileAsync(originalTheme, tempFile);

                var loadedTheme = await service.LoadThemeFromFileAsync(tempFile);
                Assert.Equal(originalTheme.ThemeName, loadedTheme.ThemeName);
                Assert.Equal(originalTheme.AccentPrimary, loadedTheme.AccentPrimary);

                var dict = new ResourceDictionary();
                manager.ApplyTheme(loadedTheme, dict);

                var accentBrush = Assert.IsType<SolidColorBrush>(dict["AccentPrimaryBrush"]);
                Assert.Equal(Color.Parse(originalTheme.AccentPrimary), accentBrush.Color);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }
    }
}
