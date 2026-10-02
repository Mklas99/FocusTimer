namespace FocusTimer.Core.Models;

/// <summary>Builds well-known summary ranges. New presets are added here without touching the summary service.</summary>
public static class SummaryRanges
{
    /// <summary>Gets the local calendar day that contains the provider's current time.</summary>
    /// <param name="timeProvider">The clock and time zone to use.</param>
    /// <returns>The range from local midnight to the next local midnight.</returns>
    public static SummaryRange Today(TimeProvider timeProvider) =>
        Day(DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime), timeProvider);

    /// <summary>Gets the local calendar day <paramref name="day"/> in the provider's time zone.</summary>
    /// <param name="day">The local day.</param>
    /// <param name="timeProvider">The clock and time zone to use.</param>
    /// <returns>The range from that day's local midnight to the next local midnight.</returns>
    public static SummaryRange Day(DateOnly day, TimeProvider timeProvider)
    {
        var date = day.ToDateTime(TimeOnly.MinValue);
        var timeZone = timeProvider.LocalTimeZone;
        var nextDate = date.AddDays(1);
        return new SummaryRange(
            new DateTimeOffset(date, timeZone.GetUtcOffset(date)),
            new DateTimeOffset(nextDate, timeZone.GetUtcOffset(nextDate)));
    }
}
