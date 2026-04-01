namespace FocusTimer.Core.Tests;

using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

public class ThemeServiceTests
{
    [Fact]
    public void BuiltInThemes_GivenServiceInstance_ContainsDefaultThemes()
    {
        var service = new ThemeService();

        Assert.NotEmpty(service.BuiltInThemes);
        Assert.NotNull(service.GetBuiltInTheme("Dark"));
        Assert.NotNull(service.GetBuiltInTheme("Light"));
    }

    [Fact]
    public void ApplyTheme_GivenTheme_StoresCloneNotOriginalReference()
    {
        var service = new ThemeService();
        var input = new Theme { ThemeName = "CustomOne", AccentPrimary = "#112233" };

        service.ApplyTheme(input);
        input.AccentPrimary = "#FFFFFF";

        Assert.Equal("#112233", service.CurrentTheme.AccentPrimary);
    }

    [Fact]
    public void GetBuiltInTheme_GivenCaseInsensitiveName_ReturnsTheme()
    {
        var service = new ThemeService();

        var theme = service.GetBuiltInTheme("dArK");

        Assert.NotNull(theme);
        Assert.Equal("Dark", theme!.ThemeName);
    }

    [Fact]
    public void GetBuiltInTheme_GivenUnknownTheme_ReturnsNull()
    {
        var service = new ThemeService();

        var theme = service.GetBuiltInTheme("DoesNotExist");

        Assert.Null(theme);
    }

    [Fact]
    public void ResetToDefault_GivenCustomTheme_RestoresDarkTheme()
    {
        var service = new ThemeService();
        service.ApplyTheme(new Theme { ThemeName = "Custom" });

        service.ResetToDefault();

        Assert.Equal("Dark", service.CurrentTheme.ThemeName);
    }

    [Theory]
    [InlineData("", "#fff", "#fff", "#fff", false)]
    [InlineData("#000", "", "#fff", "#fff", false)]
    [InlineData("#000", "#fff", "", "#fff", false)]
    [InlineData("#000", "#fff", "#fff", "", false)]
    [InlineData("#000", "#fff", "#fff", "#fff", true)]
    public void ValidateTheme_GivenRequiredFields_ReturnsExpected(
        string windowBackground,
        string primaryText,
        string buttonNormal,
        string accentPrimary,
        bool expected)
    {
        var service = new ThemeService();
        var theme = new Theme
        {
            WindowBackground = windowBackground,
            PrimaryText = primaryText,
            ButtonNormal = buttonNormal,
            AccentPrimary = accentPrimary,
        };

        Assert.Equal(expected, service.ValidateTheme(theme));
    }
}
