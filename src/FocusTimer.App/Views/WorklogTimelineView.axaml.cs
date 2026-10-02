namespace FocusTimer.App.Views
{
    using System.ComponentModel;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Threading;
    using FocusTimer.App.Controls;
    using FocusTimer.App.ViewModels;

    /// <summary>
    /// Draws the loaded day as blocks along a time axis. It binds only to its own view model,
    /// so any window can host it.
    /// </summary>
    public partial class WorklogTimelineView : UserControl
    {
        private WorklogTimelineViewModel? _viewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorklogTimelineView"/> class.
        /// </summary>
        public WorklogTimelineView()
        {
            this.InitializeComponent();
            this.DataContextChanged += this.OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, System.EventArgs e)
        {
            if (this._viewModel is not null)
            {
                this._viewModel.PropertyChanged -= this.OnViewModelChanged;
            }

            this._viewModel = this.DataContext as WorklogTimelineViewModel;
            if (this._viewModel is not null)
            {
                this._viewModel.PropertyChanged += this.OnViewModelChanged;
                this.ScrollToFirstEntry();
            }
        }

        private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WorklogTimelineViewModel.ScrollTargetMinute))
            {
                this.ScrollToFirstEntry();
            }
        }

        private void ScrollToFirstEntry()
        {
            if (this._viewModel is null)
            {
                return;
            }

            // Wait for the new blocks to be laid out before moving the viewport.
            var offset = this._viewModel.ScrollTargetMinute / 60 * TimelinePanel.HourHeight;
            Dispatcher.UIThread.Post(
                () =>
                {
                    if (this.FindControl<ScrollViewer>("Scroller") is { } scroller)
                    {
                        scroller.Offset = new Vector(0, offset);
                    }
                },
                DispatcherPriority.Loaded);
        }
    }
}
