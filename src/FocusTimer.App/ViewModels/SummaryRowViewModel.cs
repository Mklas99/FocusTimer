namespace FocusTimer.App.ViewModels
{
    using FocusTimer.Core.Models;

    /// <summary>
    /// A display-ready row of the worklog summary.
    /// </summary>
    public sealed class SummaryRowViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SummaryRowViewModel"/> class.
        /// </summary>
        /// <param name="row">The summary row to display.</param>
        public SummaryRowViewModel(SummaryRow row)
        {
            this.Label = row.Label;
            this.DurationText = SummaryFormatting.Duration(row.Duration);
            this.ShareText = SummaryFormatting.Percent(row.Share);
            this.SharePercent = row.Share * 100;
            this.EntryCountText = SummaryFormatting.EntryCount(row.EntryCount);
            this.IsUnassigned = row.IsUnassigned;
        }

        /// <summary>Gets the group name.</summary>
        public string Label { get; }

        /// <summary>Gets the formatted duration.</summary>
        public string DurationText { get; }

        /// <summary>Gets the formatted share.</summary>
        public string ShareText { get; }

        /// <summary>Gets the share from 0 to 100, for a progress bar.</summary>
        public double SharePercent { get; }

        /// <summary>Gets the formatted entry count.</summary>
        public string EntryCountText { get; }

        /// <summary>Gets a value indicating whether this is the bucket for time without a project.</summary>
        public bool IsUnassigned { get; }
    }
}
