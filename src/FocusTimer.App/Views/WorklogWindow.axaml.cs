namespace FocusTimer.App.Views
{
    using System;
    using Avalonia.Controls;
    using FocusTimer.App.ViewModels;

    /// <summary>
    /// The window that shows tracked entries, their timeline, and the summary for one day at a time.
    /// </summary>
    public partial class WorklogWindow : Window
    {
        private bool _firstActivation = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorklogWindow"/> class.
        /// </summary>
        public WorklogWindow()
        {
            this.InitializeComponent();
            this.Activated += this.OnWindowActivated;
        }

        private void OnWindowActivated(object? sender, EventArgs e)
        {
            // The first activation is the one that opens the window, which the controller already loaded.
            if (this._firstActivation)
            {
                this._firstActivation = false;
                return;
            }

            if (this.DataContext is WorklogWindowViewModel viewModel)
            {
                _ = viewModel.ActivateAsync();
            }
        }

        private void OnTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!ReferenceEquals(e.Source, sender)
                || this.DataContext is not WorklogWindowViewModel viewModel
                || (sender as TabControl)?.SelectedItem is not TabItem { Tag: string tag }
                || !Enum.TryParse<WorklogTab>(tag, out var tab)
                || tab == viewModel.SelectedTab)
            {
                return;
            }

            _ = viewModel.SelectTabAsync(tab);
        }
    }
}
