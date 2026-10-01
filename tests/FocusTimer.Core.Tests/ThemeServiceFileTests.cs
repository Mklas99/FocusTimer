namespace FocusTimer.Core.Tests;

using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

public sealed class ThemeServiceFileTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "FocusTimerThemeTests_" + Guid.NewGuid().ToString("N"));

    public ThemeServiceFileTests() => Directory.CreateDirectory(this._dir);

    public void Dispose()
    {
        if (Directory.Exists(this._dir))
        {
            Directory.Delete(this._dir, recursive: true);
        }
    }

    [Fact]
    public async Task SaveThenLoad_GivenTheme_RoundTripsColors()
    {
        var service = new ThemeService();
        var theme = service.GetBuiltInTheme("Light")!;
        theme.AccentPrimary = "#123456";
        theme.WidgetBlurMode = WidgetBlurModes.Solid;
        theme.BackgroundOpacity = 0.43;
        var path = Path.Combine(this._dir, "nested", "theme.json");

        await service.SaveThemeToFileAsync(theme, path);
        var loaded = await service.LoadThemeFromFileAsync(path);

        Assert.Equal("#123456", loaded.AccentPrimary);
        Assert.Equal(theme.ThemeName, loaded.ThemeName);
        Assert.Equal(theme.WindowBackground, loaded.WindowBackground);
        Assert.Equal(WidgetBlurModes.Solid, loaded.WidgetBlurMode);
        Assert.Equal(0.43, loaded.BackgroundOpacity);
    }

    [Fact]
    public async Task LoadThemeFromFileAsync_GivenMissingFile_ThrowsFileNotFound()
    {
        var service = new ThemeService();

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => service.LoadThemeFromFileAsync(Path.Combine(this._dir, "missing.json")));
    }

    [Fact]
    public async Task LoadThemeFromFileAsync_GivenInvalidJson_ThrowsInvalidOperation()
    {
        var path = Path.Combine(this._dir, "bad.json");
        await File.WriteAllTextAsync(path, "{ not json");

        await Assert.ThrowsAsync<InvalidOperationException>(() => new ThemeService().LoadThemeFromFileAsync(path));
    }

    [Fact]
    public async Task LoadThemeFromFileAsync_GivenJsonNull_ThrowsInvalidOperation()
    {
        var path = Path.Combine(this._dir, "null.json");
        await File.WriteAllTextAsync(path, "null");

        await Assert.ThrowsAsync<InvalidOperationException>(() => new ThemeService().LoadThemeFromFileAsync(path));
    }

    [Fact]
    public async Task LoadThemeFromFileAsync_GivenMissingRequiredColors_ThrowsInvalidOperation()
    {
        var path = Path.Combine(this._dir, "incomplete.json");
        await File.WriteAllTextAsync(path, "{ \"themeName\": \"Half\", \"accentPrimary\": \"\" }");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => new ThemeService().LoadThemeFromFileAsync(path));
        Assert.Contains("missing required", ex.Message);
    }

    [Fact]
    public async Task LoadThemeFromFileAsync_GivenCommentsAndMixedCase_Parses()
    {
        var path = Path.Combine(this._dir, "commented.json");
        await File.WriteAllTextAsync(
            path,
            "{ // comment\n \"THEMENAME\": \"Commented\", \"windowBackground\": \"#000000\", \"primaryText\": \"#FFFFFF\", \"buttonNormal\": \"#111111\", \"accentPrimary\": \"#222222\" }");

        var theme = await new ThemeService().LoadThemeFromFileAsync(path);

        Assert.Equal("Commented", theme.ThemeName);
        Assert.Equal(WidgetBlurModes.Off, theme.WidgetBlurMode);
    }

    [Theory]
    [InlineData("Soft")]
    [InlineData("Strong")]
    [InlineData("Blur")]
    public async Task LoadThemeFromFileAsync_GivenLegacyBlur_MapsToOff(string legacyMode)
    {
        var path = Path.Combine(this._dir, "legacy.fttheme");
        await File.WriteAllTextAsync(path, $"{{ \"widgetBlurMode\": \"{legacyMode}\" }}");

        var theme = await new ThemeService().LoadThemeFromFileAsync(path);

        Assert.Equal(WidgetBlurModes.Off, theme.WidgetBlurMode);
    }

    [Fact]
    public async Task LoadThemeFromFileAsync_GivenUnknownBlur_RejectsTheme()
    {
        var path = Path.Combine(this._dir, "invalid-blur.fttheme");
        await File.WriteAllTextAsync(path, "{ \"widgetBlurMode\": \"Extreme\" }");

        await Assert.ThrowsAsync<InvalidOperationException>(() => new ThemeService().LoadThemeFromFileAsync(path));
    }

    [Fact]
    public void ValidateTheme_GivenBlankRequiredColor_ReturnsFalse()
    {
        var service = new ThemeService();
        var theme = service.GetBuiltInTheme("Dark")!;
        Assert.True(service.ValidateTheme(theme));

        theme.ButtonNormal = " ";

        Assert.False(service.ValidateTheme(theme));
    }

    [Fact]
    public void ResetToDefault_AfterApplyingOtherTheme_RestoresDark()
    {
        var service = new ThemeService();
        service.ApplyTheme(service.GetBuiltInTheme("Light")!);

        service.ResetToDefault();

        Assert.Equal("Dark", service.CurrentTheme.ThemeName);
    }
}
