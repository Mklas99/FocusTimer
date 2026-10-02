namespace FocusTimer.App.ViewModels
{
    using System;

    /// <summary>
    /// One entry placed on the day's time axis. Entries that overlap in time share the width as separate lanes,
    /// so none is hidden.
    /// </summary>
    public sealed class TimelineBlockViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimelineBlockViewModel"/> class.
        /// </summary>
        /// <param name="row">The entry as shown in the table.</param>
        /// <param name="lane">The lane the block is drawn in, starting at zero.</param>
        /// <param name="laneCount">How many lanes the block's overlap group has.</param>
        /// <param name="column">The group column the block is in; zero when the timeline is not grouped.</param>
        /// <param name="columnCount">How many group columns there are; one when the timeline is not grouped.</param>
        public TimelineBlockViewModel(WorklogEntryRowViewModel row, int lane, int laneCount, int column = 0, int columnCount = 1)
        {
            this.Row = row;
            this.Lane = lane;
            this.LaneCount = laneCount;
            this.Column = column;
            this.ColumnCount = columnCount;
            var entry = row.Entry;
            this.StartMinute = entry.StartedAt.TimeOfDay.TotalMinutes;
            this.LengthMinutes = entry.Duration.TotalMinutes;
            this.TimeText = $"{row.StartText}–{row.EndText}";
            this.TitleText = row.IsManual ? $"Manual · {row.WindowText}" : row.ApplicationText;
            this.DetailText = row.IsManual ? row.DurationText : row.WindowText;
            this.ToolTipText = $"{row.Summary}\n{row.WindowText}\nProject: {row.ProjectText}\nDuration {row.DurationText} ({row.SourceText})";
        }

        /// <summary>Gets the row of the Entries table this block shows.</summary>
        public WorklogEntryRowViewModel Row { get; }

        /// <summary>Gets the group column, starting at zero.</summary>
        public int Column { get; }

        /// <summary>Gets the number of group columns.</summary>
        public int ColumnCount { get; }

        /// <summary>Gets the lane, starting at zero.</summary>
        public int Lane { get; }

        /// <summary>Gets the number of lanes in this block's overlap group.</summary>
        public int LaneCount { get; }

        /// <summary>Gets the start as minutes since local midnight.</summary>
        public double StartMinute { get; }

        /// <summary>Gets the length in minutes.</summary>
        public double LengthMinutes { get; }

        /// <summary>Gets the time span text, such as 09:00–10:30.</summary>
        public string TimeText { get; }

        /// <summary>Gets the first line: the application, or "Manual" with the window text for manual entries.</summary>
        public string TitleText { get; }

        /// <summary>Gets the second line.</summary>
        public string DetailText { get; }

        /// <summary>Gets the tooltip text.</summary>
        public string ToolTipText { get; }

        /// <summary>Gets a value indicating whether the user added this entry by hand.</summary>
        public bool IsManual => this.Row.IsManual;
    }
}
