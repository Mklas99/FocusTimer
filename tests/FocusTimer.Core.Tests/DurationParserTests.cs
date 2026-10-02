namespace FocusTimer.Core.Tests;

using FocusTimer.Core.Services;

public class DurationParserTests
{
    [Theory]
    [InlineData("2h 30m", 150)]
    [InlineData("2h30m", 150)]
    [InlineData("45m", 45)]
    [InlineData("1h", 60)]
    [InlineData("2.5h", 150)]
    [InlineData("2,5h", 150)]
    [InlineData("0.25h", 15)]
    [InlineData("  1H 5M  ", 65)]
    [InlineData("24h", 1440)]
    public void TryParse_GivenAcceptedFormat_ReturnsWholeMinutes(string text, int expectedMinutes)
    {
        Assert.True(DurationParser.TryParse(text, out var duration, out var error), error);

        Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), duration);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("2 hours")]
    [InlineData("90")]
    [InlineData("-1h")]
    [InlineData("0m")]
    [InlineData("0h")]
    [InlineData("0.001h")]
    [InlineData("25h")]
    [InlineData("1h 30")]
    public void TryParse_GivenUnusableText_FailsWithMessageShowingExample(string? text)
    {
        Assert.False(DurationParser.TryParse(text, out var duration, out var error));

        Assert.Equal(TimeSpan.Zero, duration);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void TryParse_GivenGarbage_MessageContainsAcceptedExample()
    {
        DurationParser.TryParse("soon", out _, out var error);

        Assert.Contains("2h 30m", error);
    }

    [Theory]
    [InlineData(150, "2h 30m")]
    [InlineData(45, "45m")]
    [InlineData(60, "1h")]
    public void Format_RoundTripsWithParser(int minutes, string expected)
    {
        var text = DurationParser.Format(TimeSpan.FromMinutes(minutes));

        Assert.Equal(expected, text);
        Assert.True(DurationParser.TryParse(text, out var parsed, out _));
        Assert.Equal(TimeSpan.FromMinutes(minutes), parsed);
    }
}
