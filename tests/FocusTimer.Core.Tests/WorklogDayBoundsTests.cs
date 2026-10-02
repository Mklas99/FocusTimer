namespace FocusTimer.Core.Tests;

using FocusTimer.Core.Services;

public class WorklogDayBoundsTests
{
    private static readonly DateOnly Today = new(2026, 5, 4);

    [Theory]
    [InlineData(1, 2026, 5, 4)]
    [InlineData(90, 2026, 2, 4)]
    [InlineData(7, 2026, 4, 28)]
    public void EarliestDay_GivenRetention_IsTodayMinusRetentionMinusOne(int retention, int year, int month, int day)
    {
        Assert.Equal(new DateOnly(year, month, day), WorklogDayBounds.EarliestDay(Today, retention));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void EarliestDay_GivenRetentionOff_IsUnlimited(int retention)
    {
        Assert.Null(WorklogDayBounds.EarliestDay(Today, retention));
        Assert.True(WorklogDayBounds.IsAllowed(new DateOnly(2000, 1, 1), Today, retention));
    }

    [Fact]
    public void IsAllowed_AtBoundaries()
    {
        Assert.True(WorklogDayBounds.IsAllowed(Today, Today, 90));
        Assert.False(WorklogDayBounds.IsAllowed(Today.AddDays(1), Today, 90));
        Assert.True(WorklogDayBounds.IsAllowed(Today.AddDays(-89), Today, 90));
        Assert.False(WorklogDayBounds.IsAllowed(Today.AddDays(-90), Today, 90));
        Assert.True(WorklogDayBounds.IsAllowed(Today, Today, 1));
        Assert.False(WorklogDayBounds.IsAllowed(Today.AddDays(-1), Today, 1));
    }
}
