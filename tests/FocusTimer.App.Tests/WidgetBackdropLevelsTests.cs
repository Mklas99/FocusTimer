namespace FocusTimer.App.Tests;

using Avalonia.Controls;
using FocusTimer.App.Services;
using FocusTimer.Core.Models;

public sealed class WidgetBackdropLevelsTests
{
    [Fact]
    public void ForTheme_UsesSelectedLevelAndLowerFallbacks()
    {
        Assert.Equal(
            [WindowTransparencyLevel.Transparent],
            WidgetBackdropLevels.ForTheme(new Theme { WidgetBlurMode = WidgetBlurModes.Off }));
        Assert.Equal(
            [WindowTransparencyLevel.Transparent],
            WidgetBackdropLevels.ForTheme(new Theme { WidgetBlurMode = WidgetBlurModes.Solid }));
    }

    [Fact]
    public void ForTheme_HighContrastNeverRequestsBlur()
    {
        var theme = new Theme { ThemeName = "High Contrast", WidgetBlurMode = WidgetBlurModes.Solid };

        Assert.Equal([WindowTransparencyLevel.Transparent], WidgetBackdropLevels.ForTheme(theme));
    }
}
