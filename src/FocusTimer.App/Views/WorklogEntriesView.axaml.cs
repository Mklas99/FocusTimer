namespace FocusTimer.App.Views
{
    using Avalonia.Controls;
    using Avalonia.Input;
    using FocusTimer.App.ViewModels;

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

        private void OnProjectDropDownClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // Offer the projects already used without requiring the user to type first.
            var box = this.FindControl<AutoCompleteBox>("ProjectBox");
            if (box is not null)
            {
                box.Focus();
                box.IsDropDownOpen = true;
            }
        }

        private void OnOverlapWarningTapped(object? sender, TappedEventArgs e) =>
            (this.DataContext as WorklogEntriesViewModel)?.DismissOverlapWarningCommand.Execute(null);

        private void OnActionErrorTapped(object? sender, TappedEventArgs e) =>
            (this.DataContext as WorklogEntriesViewModel)?.DismissActionErrorCommand.Execute(null);
    }
}
