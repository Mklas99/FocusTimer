namespace FocusTimer.Core.Services;

/// <summary>Works out which local days the worklog window and manual entry may use.</summary>
public static class WorklogDayBounds
{
    /// <summary>Gets the earliest day that retention keeps, or null when retention is turned off.</summary>
    /// <param name="today">The current local day.</param>
    /// <param name="dataRetentionDays">The configured retention in days; zero or less means unlimited.</param>
    /// <returns>Today minus (retention days minus one), or null for unlimited retention.</returns>
    public static DateOnly? EarliestDay(DateOnly today, int dataRetentionDays) =>
        dataRetentionDays <= 0 ? null : today.AddDays(-(dataRetentionDays - 1));

    /// <summary>Tells whether a day may be selected or written to.</summary>
    /// <param name="day">The day to check.</param>
    /// <param name="today">The current local day.</param>
    /// <param name="dataRetentionDays">The configured retention in days; zero or less means unlimited.</param>
    /// <returns>True when the day is not in the future and not older than retention allows.</returns>
    public static bool IsAllowed(DateOnly day, DateOnly today, int dataRetentionDays) =>
        day <= today && (EarliestDay(today, dataRetentionDays) is not { } earliest || day >= earliest);
}
