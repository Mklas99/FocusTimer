namespace FocusTimer.App.Tests;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FocusTimer.App.Controls;
using FocusTimer.App.Services;
using FocusTimer.Core.Models;

/// <summary>
/// The dense-frost shell falls back to solid whenever the platform, the theme, or the user's accessibility
/// preferences say so, and the material never depends on the widget's own opacity values.
/// </summary>
public class DesktopMaterialTests
{
    public static TheoryData<string, bool, bool, bool, DesktopMaterialState> Cases => new()
    {
        { nameof(WindowTransparencyLevel.AcrylicBlur), false, false, false, DesktopMaterialState.DenseFrost },
        { nameof(WindowTransparencyLevel.Blur), false, false, false, DesktopMaterialState.DenseFrost },
        { nameof(WindowTransparencyLevel.None), false, false, false, DesktopMaterialState.SolidFallback },
        { nameof(WindowTransparencyLevel.Transparent), false, false, false, DesktopMaterialState.SolidFallback },
        { nameof(WindowTransparencyLevel.Mica), false, false, false, DesktopMaterialState.SolidFallback },
        { nameof(WindowTransparencyLevel.AcrylicBlur), true, false, false, DesktopMaterialState.SolidFallback },
        { nameof(WindowTransparencyLevel.AcrylicBlur), false, true, false, DesktopMaterialState.SolidFallback },
        { nameof(WindowTransparencyLevel.AcrylicBlur), false, false, true, DesktopMaterialState.SolidFallback },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Resolve_RequiresAReportedBlurAndNoAccessibilityOverride(
        string actual, bool highContrastTheme, bool systemHighContrast, bool transparencyOff, DesktopMaterialState expected)
    {
        WindowTransparencyLevel level = actual switch
        {
            nameof(WindowTransparencyLevel.AcrylicBlur) => WindowTransparencyLevel.AcrylicBlur,
            nameof(WindowTransparencyLevel.Blur) => WindowTransparencyLevel.Blur,
            nameof(WindowTransparencyLevel.None) => WindowTransparencyLevel.None,
            nameof(WindowTransparencyLevel.Transparent) => WindowTransparencyLevel.Transparent,
            nameof(WindowTransparencyLevel.Mica) => WindowTransparencyLevel.Mica,
            _ => throw new ArgumentOutOfRangeException(nameof(actual), actual, "Unknown transparency level."),
        };
        var inputs = new DesktopMaterialInputs(level, highContrastTheme, systemHighContrast, transparencyOff);

        Assert.Equal(expected, DesktopMaterialPolicy.Resolve(inputs));
    }

    [Fact]
    public void Resolve_FollowsEveryTransitionBetweenFrostAndFallback()
    {
        var blurred = new DesktopMaterialInputs(WindowTransparencyLevel.AcrylicBlur, false, false, false);

        Assert.Equal(DesktopMaterialState.DenseFrost, DesktopMaterialPolicy.Resolve(blurred));
        Assert.Equal(DesktopMaterialState.SolidFallback, DesktopMaterialPolicy.Resolve(blurred with { TransparencyDisabled = true }));
        Assert.Equal(DesktopMaterialState.DenseFrost, DesktopMaterialPolicy.Resolve(blurred with { TransparencyDisabled = false }));
        Assert.Equal(DesktopMaterialState.SolidFallback, DesktopMaterialPolicy.Resolve(blurred with { SystemHighContrast = true }));
        Assert.Equal(DesktopMaterialState.SolidFallback, DesktopMaterialPolicy.Resolve(blurred with { Actual = WindowTransparencyLevel.None }));
        Assert.Equal(DesktopMaterialState.DenseFrost, DesktopMaterialPolicy.Resolve(blurred));
    }

    [Fact]
    public void RequestedLevels_AskForBlurFirstAndAlwaysEndWithTheSolidLevel()
    {
        Assert.Equal(
            [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.None],
            DesktopMaterialPolicy.RequestedLevels);
    }

    [Fact]
    public void Describe_ReportsRequestedAndActualLevelsAndWhyFrostIsOff()
    {
        var inputs = new DesktopMaterialInputs(WindowTransparencyLevel.AcrylicBlur, false, false, true);

        string text = DesktopMaterialPolicy.Describe(inputs, DesktopMaterialPolicy.Resolve(inputs));

        Assert.Contains("solid", text, StringComparison.Ordinal);
        Assert.Contains("requested AcrylicBlur/None", text, StringComparison.Ordinal);
        Assert.Contains("actual AcrylicBlur", text, StringComparison.Ordinal);
        Assert.Contains("transparency effects off", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyTheme_DerivesShellBrushesFromSettingsRolesOnly()
    {
        var manager = new ThemeManager();
        var resources = new ResourceDictionary();
        var theme = new Theme { SettingsBackground = "#102030", PrimaryText = "#F0F0F0", BackgroundOpacity = 0.2, WidgetBaseOpacity = 0.3 };
        manager.ApplyTheme(theme, resources);
        var frost = Assert.IsType<SolidColorBrush>(resources["DesktopShellFrostBrush"]);
        var card = Assert.IsType<SolidColorBrush>(resources["DesktopCardBrush"]);

        theme.BackgroundOpacity = 0.9;
        theme.TimerOpacity = 0.1;
        theme.ButtonOpacity = 0.4;
        theme.WidgetBaseOpacity = 0.8;
        manager.ApplyTheme(theme, resources);

        Assert.Equal(Color.Parse("#102030"), frost.Color);
        Assert.Equal(ThemeManager.DesktopShellFrostOpacity, frost.Opacity);
        var frostAfter = Assert.IsType<SolidColorBrush>(resources["DesktopShellFrostBrush"]);
        var cardAfter = Assert.IsType<SolidColorBrush>(resources["DesktopCardBrush"]);
        Assert.Equal(frost.Color, frostAfter.Color);
        Assert.Equal(frost.Opacity, frostAfter.Opacity);
        Assert.Equal(card.Color, cardAfter.Color);
        Assert.Equal(1.0, cardAfter.Opacity);
    }

    [Theory]
    [InlineData("High Contrast", true)]
    [InlineData("Dark", false)]
    public void ApplyTheme_ForcesSolidMaterialOnlyForHighContrast(string name, bool expected)
    {
        var resources = new ResourceDictionary();

        new ThemeManager().ApplyTheme(new Theme { ThemeName = name }, resources);

        Assert.Equal(expected, resources[DesktopWindowMaterial.ForceSolidResourceKey]);
    }

    [Fact]
    public void ResponsiveCardsPanel_SitsSideBySideOnlyWhenTheyFit()
    {
        var panel = new ResponsiveCardsPanel { TwoColumnMinWidth = 560, Spacing = 12 };
        var first = new Border { Height = 80 };
        var second = new Border { Height = 120 };
        panel.Children.Add(first);
        panel.Children.Add(second);

        panel.Measure(new Size(600, double.PositiveInfinity));
        panel.Arrange(new Rect(0, 0, 600, panel.DesiredSize.Height));

        Assert.Equal(120, panel.DesiredSize.Height);
        Assert.Equal(0, first.Bounds.X);
        Assert.Equal(300 + 6, second.Bounds.X);
        Assert.Equal(294, first.Bounds.Width);
        Assert.Equal(first.Bounds.Width, second.Bounds.Width);

        panel.Measure(new Size(440, double.PositiveInfinity));
        panel.Arrange(new Rect(0, 0, 440, panel.DesiredSize.Height));

        Assert.Equal(80 + 12 + 120, panel.DesiredSize.Height);
        Assert.Equal(0, second.Bounds.X);
        Assert.Equal(92, second.Bounds.Y);
        Assert.Equal(440, first.Bounds.Width);
        Assert.Equal(440, second.Bounds.Width);
    }

    [Fact]
    public void ResponsiveCardsPanel_IgnoresHiddenCardsAndNeverUsesColumnsForASingleCard()
    {
        var panel = new ResponsiveCardsPanel { TwoColumnMinWidth = 560, Spacing = 12 };
        var only = new Border { Height = 40 };
        var hidden = new Border { Height = 40, IsVisible = false };
        panel.Children.Add(only);
        panel.Children.Add(hidden);

        panel.Measure(new Size(900, double.PositiveInfinity));
        panel.Arrange(new Rect(0, 0, 900, panel.DesiredSize.Height));

        Assert.Equal(900, only.Bounds.Width);
        Assert.Equal(40, panel.DesiredSize.Height);
    }
}
