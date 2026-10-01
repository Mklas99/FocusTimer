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
        public void ApplyTheme_BackgroundOpacityDoesNotChangeForegroundBrushes()
        {
            var manager = new ThemeManager();
            var dict = new ResourceDictionary();
            var theme = new Theme { BackgroundOpacity = 0.8 };
            manager.ApplyTheme(theme, dict);
            var timerBrush = Assert.IsType<SolidColorBrush>(dict["TimerTextBrush"]);
            var buttonBrush = Assert.IsType<SolidColorBrush>(dict["ButtonNormalBrush"]);
            var projectBrush = Assert.IsType<SolidColorBrush>(dict["ProjectTagBackgroundBrush"]);

            theme.BackgroundOpacity = 0.2;
            manager.ApplyTheme(theme, dict);

            Assert.Equal(0.2, Assert.IsType<SolidColorBrush>(dict["WidgetShellTintBrush"]).Opacity);
            var updatedTimer = Assert.IsType<SolidColorBrush>(dict["TimerTextBrush"]);
            var updatedButton = Assert.IsType<SolidColorBrush>(dict["ButtonNormalBrush"]);
            var updatedProject = Assert.IsType<SolidColorBrush>(dict["ProjectTagBackgroundBrush"]);
            Assert.Equal((timerBrush.Color, timerBrush.Opacity), (updatedTimer.Color, updatedTimer.Opacity));
            Assert.Equal((buttonBrush.Color, buttonBrush.Opacity), (updatedButton.Color, updatedButton.Opacity));
            Assert.Equal((projectBrush.Color, projectBrush.Opacity), (updatedProject.Color, updatedProject.Opacity));
        }

        [Fact]
        public void WidgetShell_OffKeepsTintOpacityOnTransparentDesktop()
        {
            var manager = new ThemeManager();
            var dict = new ResourceDictionary();
            manager.ApplyTheme(new Theme { WidgetBlurMode = WidgetBlurModes.Off, BackgroundOpacity = 0 }, dict);

            manager.ReportActualWidgetTransparency(WindowTransparencyLevel.Transparent);
            Assert.False(manager.IsWidgetShellFallbackActive);
            Assert.Same(dict["WidgetShellTintBrush"], dict["WidgetShellActiveBrush"]);

            manager.ApplyTheme(new Theme { WidgetBlurMode = WidgetBlurModes.Off, BackgroundOpacity = 0 }, dict);
            Assert.False(manager.IsWidgetShellFallbackActive);
            Assert.Same(dict["WidgetShellTintBrush"], dict["WidgetShellActiveBrush"]);
        }

        [Fact]
        public void WidgetShell_HighContrastRemainsSolidOnTransparentDesktop()
        {
            var manager = new ThemeManager();
            var dict = new ResourceDictionary();
            var theme = new Core.Services.ThemeService().GetBuiltInTheme("High Contrast")!;
            theme.BackgroundOpacity = 0;
            theme.WidgetBlurMode = WidgetBlurModes.Off;
            manager.ApplyTheme(theme, dict);
            manager.ReportActualWidgetTransparency(WindowTransparencyLevel.Transparent);

            Assert.True(manager.IsWidgetShellFallbackActive);
            Assert.Same(dict["WidgetShellFallbackBrush"], dict["WidgetShellActiveBrush"]);
        }

        [Fact]
        public void WidgetShell_SolidIgnoresSavedTintOpacityWithoutDimmingForeground()
        {
            var manager = new ThemeManager();
            var dict = new ResourceDictionary();
            manager.ApplyTheme(new Theme { WidgetBlurMode = WidgetBlurModes.Solid, BackgroundOpacity = 0 }, dict);
            manager.ReportActualWidgetTransparency(WindowTransparencyLevel.Transparent);

            Assert.True(manager.IsWidgetShellFallbackActive);
            Assert.Same(dict["WidgetShellFallbackBrush"], dict["WidgetShellActiveBrush"]);
            Assert.Equal(1, Assert.IsType<SolidColorBrush>(dict["WidgetShellActiveBrush"]).Opacity);
            Assert.Equal(0, manager.ActiveTheme!.BackgroundOpacity);
        }

        [Theory]
        [InlineData(WidgetBlurModes.Off, "Transparent")]
        public void WidgetShell_ZeroTintStaysClearAtEverySupportedTransparencyLevel(
            string mode,
            string actualLevelName)
        {
            var manager = new ThemeManager();
            var dict = new ResourceDictionary();
            manager.ApplyTheme(new Theme { WidgetBlurMode = mode, BackgroundOpacity = 0 }, dict);

            WindowTransparencyLevel actualLevel = actualLevelName switch
            {
                "Blur" => WindowTransparencyLevel.Blur,
                "AcrylicBlur" => WindowTransparencyLevel.AcrylicBlur,
                _ => WindowTransparencyLevel.Transparent,
            };
            manager.ReportActualWidgetTransparency(actualLevel);

            Assert.False(manager.IsWidgetShellFallbackActive);
            Assert.Same(dict["WidgetShellTintBrush"], dict["WidgetShellActiveBrush"]);
            Assert.Equal(0, Assert.IsType<SolidColorBrush>(dict["WidgetShellActiveBrush"]).Opacity);
        }


        [Fact]
        public void ApplyTheme_PublishesAppearanceSnapshot()
        {
            var manager = new ThemeManager();
            Theme? published = null;
            manager.ThemeApplied += theme => published = theme;
            var theme = new Theme { WidgetBlurMode = WidgetBlurModes.Solid };

            manager.ApplyTheme(theme, new ResourceDictionary());
            theme.WidgetBlurMode = WidgetBlurModes.Off;

            Assert.Equal(WidgetBlurModes.Solid, published!.WidgetBlurMode);
            Assert.Equal(WidgetBlurModes.Solid, manager.ActiveTheme!.WidgetBlurMode);
        }

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
            Assert.True(dict.ContainsKey("WidgetShellTintBrush"));
            Assert.True(dict.ContainsKey("WidgetShellFallbackBrush"));

            var bgBrush = Assert.IsType<SolidColorBrush>(dict["WindowBackgroundBrush"]);
            Assert.Equal(0.9, bgBrush.Opacity);

            var shellTint = Assert.IsType<SolidColorBrush>(dict["WidgetShellTintBrush"]);
            var shellFallback = Assert.IsType<SolidColorBrush>(dict["WidgetShellFallbackBrush"]);
            Assert.Equal(0.9, shellTint.Opacity);
            Assert.Equal(1.0, shellFallback.Opacity);

            var focusRingBrush = Assert.IsType<SolidColorBrush>(dict["FocusRingBrush"]);
            Assert.Equal(Color.Parse("#0078D7"), focusRingBrush.Color);
        }

        [Fact]
        public void ApplyTheme_UsesIndependentInputFocusAndSelectedTabColorsAcrossUpdates()
        {
            var manager = new ThemeManager();
            var resources = new ResourceDictionary();
            var theme = new Theme
            {
                AccentPrimary = "#112233",
                InputFocusBorder = "#445566",
                TabSelectedBackground = "#778899",
            };

            manager.ApplyTheme(theme, resources);
            Assert.Equal(Color.Parse(theme.InputFocusBorder), resources["InputFocusBorderColor"]);
            Assert.Equal(Color.Parse(theme.InputFocusBorder),
                Assert.IsType<SolidColorBrush>(resources["InputFocusBorderBrush"]).Color);
            Assert.Equal(Color.Parse(theme.TabSelectedBackground), resources["TabSelectedBackgroundColor"]);
            Assert.Equal(Color.Parse(theme.TabSelectedBackground),
                Assert.IsType<SolidColorBrush>(resources["TabSelectedBackgroundBrush"]).Color);

            theme.InputFocusBorder = "#AABBCC";
            theme.TabSelectedBackground = "#DDEEFF";
            manager.ApplyTheme(theme, resources);

            Assert.Equal(Color.Parse(theme.InputFocusBorder), resources["InputFocusBorderColor"]);
            Assert.Equal(Color.Parse(theme.InputFocusBorder),
                Assert.IsType<SolidColorBrush>(resources["InputFocusBorderBrush"]).Color);
            Assert.Equal(Color.Parse(theme.TabSelectedBackground), resources["TabSelectedBackgroundColor"]);
            Assert.Equal(Color.Parse(theme.TabSelectedBackground),
                Assert.IsType<SolidColorBrush>(resources["TabSelectedBackgroundBrush"]).Color);
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
            Assert.Equal(0.0, Assert.IsType<SolidColorBrush>(dict["WidgetShellTintBrush"]).Opacity);
            Assert.Equal(1.0, Assert.IsType<SolidColorBrush>(dict["WidgetShellFallbackBrush"]).Opacity);
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
            Assert.Equal(1.0, Assert.IsType<SolidColorBrush>(dict["WidgetShellTintBrush"]).Opacity);

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
