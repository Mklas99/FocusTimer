namespace FocusTimer.App.Tests;

using Avalonia.Media;
using FocusTimer.App.Services;

public class ThemeContrastTests
{
    [Fact]
    public void EnsureContrastAcross_RejectsAnInfeasibleSetInsteadOfReturningAFailedEndpoint()
    {
        Assert.Throws<InvalidOperationException>(() => ThemeContrast.EnsureContrastAcross(
            Colors.Gray, 4.5, Colors.Black, Colors.Gray, Colors.White));
    }

    [Fact]
    public void EnsureContrastAcross_FindsTheFeasibleMiddleBetweenOpposingEndpoints()
    {
        Color result = ThemeContrast.EnsureContrastAcross(Colors.White, 4.5, Colors.Black, Colors.White);
        Assert.True(ThemeContrast.Ratio(result, Colors.Black) >= 4.5);
        Assert.True(ThemeContrast.Ratio(result, Colors.White) >= 4.5);
    }

    [Fact]
    public void Ratio_MatchesTheWcagReferenceValues()
    {
        Assert.Equal(21.0, ThemeContrast.Ratio(Colors.White, Colors.Black), 2);
        Assert.Equal(1.0, ThemeContrast.Ratio(Colors.Gray, Colors.Gray), 2);
        Assert.Equal(ThemeContrast.Ratio(Colors.White, Colors.Black), ThemeContrast.Ratio(Colors.Black, Colors.White), 6);
    }

    [Fact]
    public void EnsureContrast_KeepsAnAlreadyReadableColorUnchanged()
    {
        var accent = Color.Parse("#FD971F");
        var surface = Color.Parse("#272822");

        Assert.Equal(accent, ThemeContrast.EnsureContrast(accent, surface));
    }

    [Theory]
    [InlineData("#0078D7", "#2D2D30")]
    [InlineData("#5E81AC", "#2E3440")]
    [InlineData("#0078D7", "#F5F5F5")]
    [InlineData("#777777", "#808080")]
    public void EnsureContrast_ReachesTheTargetByMovingTowardTheReadableEnd(string preferredHex, string surfaceHex)
    {
        var preferred = Color.Parse(preferredHex);
        var surface = Color.Parse(surfaceHex);

        Color result = ThemeContrast.EnsureContrast(preferred, surface);

        Assert.True(ThemeContrast.Ratio(result, surface) >= ThemeContrast.TextRatio, $"{result} on {surface}");
        Assert.Equal(255, result.A);
    }

    [Fact]
    public void EnsureContrast_PreservesTheAccentHueWhenItAdjustsADarkSurface()
    {
        var accent = Color.Parse("#0078D7");

        Color result = ThemeContrast.EnsureContrast(accent, Color.Parse("#2D2D30"));

        Assert.True(result.B > result.G && result.G > result.R, "the adjusted accent should stay blue");
        Assert.True(result.R > accent.R, "a dark surface is answered by moving toward white");
    }

    [Fact]
    public void EnsureContrast_IgnoresTheAccentAlphaChannel()
    {
        Color result = ThemeContrast.EnsureContrast(Color.Parse("#400078D7"), Color.Parse("#000000"));

        Assert.Equal(255, result.A);
        Assert.True(ThemeContrast.Ratio(result, Colors.Black) >= ThemeContrast.TextRatio);
    }
}
