namespace FocusTimer.App.ViewModels
{
    /// <summary>
    /// The states the worklog summary view can be in.
    /// </summary>
    public enum SummaryViewStatus
    {
        /// <summary>Nothing has been requested yet.</summary>
        Idle,

        /// <summary>A summary is being read.</summary>
        Loading,

        /// <summary>Rows are available.</summary>
        Ready,

        /// <summary>The worklog was read but contains no time for the range.</summary>
        NoData,

        /// <summary>The worklog could not be read.</summary>
        Error,
    }
}
