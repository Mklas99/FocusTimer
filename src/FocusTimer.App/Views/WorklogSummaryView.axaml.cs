namespace FocusTimer.App.Views
{
    using Avalonia.Controls;

    /// <summary>
    /// Shows the grouped breakdown of tracked time. It binds only to its own view model,
    /// so any window can host it.
    /// </summary>
    public partial class WorklogSummaryView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorklogSummaryView"/> class.
        /// </summary>
        public WorklogSummaryView()
        {
            this.InitializeComponent();
        }
    }
}
