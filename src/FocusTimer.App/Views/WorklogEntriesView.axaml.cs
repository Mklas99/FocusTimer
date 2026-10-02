namespace FocusTimer.App.Views
{
    using Avalonia.Controls;

    /// <summary>
    /// Shows the stored entries of one day as a table. It binds only to its own view model,
    /// so any window can host it.
    /// </summary>
    public partial class WorklogEntriesView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorklogEntriesView"/> class.
        /// </summary>
        public WorklogEntriesView()
        {
            this.InitializeComponent();
        }
    }
}
