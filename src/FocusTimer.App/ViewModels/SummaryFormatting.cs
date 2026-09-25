namespace FocusTimer.App.ViewModels
{
    using System;

    /// <summary>
    /// Formats summary values for display, consistently with the tray tooltip.
    /// </summary>
    public static class SummaryFormatting
    {
        /// <summary>
        /// Formats a duration as hours and minutes.
        /// </summary>
        /// <param name="duration">The duration to format.</param>
        /// <returns>For example "5h 42m", or "&lt;1m" for a non-zero duration below one minute.</returns>
        public static string Duration(TimeSpan duration)
        {
            if (duration > TimeSpan.Zero && duration < TimeSpan.FromMinutes(1))
            {
                return "<1m";
            }

            return $"{(int)duration.TotalHours}h {duration.Minutes:D2}m";
        }

        /// <summary>
        /// Formats a share as a whole percentage.
        /// </summary>
        /// <param name="share">A value from 0 to 1.</param>
        /// <returns>For example "54%", or "&lt;1%" for a non-zero share below one percent.</returns>
        public static string Percent(double share)
        {
            if (share > 0 && share < 0.01)
            {
                return "<1%";
            }

            return $"{Math.Round(share * 100):0}%";
        }

        /// <summary>
        /// Formats an entry count.
        /// </summary>
        /// <param name="count">The number of entries.</param>
        /// <returns>For example "1 entry" or "3 entries".</returns>
        public static string EntryCount(int count) => count == 1 ? "1 entry" : $"{count} entries";
    }
}
