namespace FocusTimer.App.ViewModels
{
    /// <summary>
    /// One column of the grouped timeline: all entries of one application, project, or window, with the group's
    /// total time for the column header.
    /// </summary>
    public sealed class TimelineGroupViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimelineGroupViewModel"/> class.
        /// </summary>
        /// <param name="index">The column, starting at zero.</param>
        /// <param name="label">The group's name.</param>
        /// <param name="isUnassigned">Whether this is the bucket for entries without a value.</param>
        /// <param name="total">The group's total time.</param>
        /// <param name="entryCount">The number of entries in the group.</param>
        public TimelineGroupViewModel(int index, string label, bool isUnassigned, System.TimeSpan total, int entryCount)
        {
            this.Index = index;
            this.Label = label;
            this.IsUnassigned = isUnassigned;
            this.TotalText = SummaryFormatting.Duration(total);
            this.CountText = SummaryFormatting.EntryCount(entryCount);
            this.ToolTipText = $"{label}\n{this.TotalText}, {this.CountText}";
        }

        /// <summary>Gets the column, starting at zero.</summary>
        public int Index { get; }

        /// <summary>Gets the group's name.</summary>
        public string Label { get; }

        /// <summary>Gets a value indicating whether this is the bucket for entries without a value.</summary>
        public bool IsUnassigned { get; }

        /// <summary>Gets the group's total time, such as "2h 15m".</summary>
        public string TotalText { get; }

        /// <summary>Gets the number of entries, such as "3 entries".</summary>
        public string CountText { get; }

        /// <summary>Gets the tooltip text.</summary>
        public string ToolTipText { get; }
    }
}
