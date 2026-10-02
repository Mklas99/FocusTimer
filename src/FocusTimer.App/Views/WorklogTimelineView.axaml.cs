namespace FocusTimer.App.Views
{
    using System.ComponentModel;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Input;
    using Avalonia.Interactivity;
    using Avalonia.Threading;

    using FocusTimer.App.ViewModels;

    /// <summary>
    /// Draws the loaded day as blocks along a time axis. It binds only to its own view model,
    /// so any window can host it.
    /// </summary>
    public partial class WorklogTimelineView : UserControl
    {
        private WorklogTimelineViewModel? _viewModel;
        private double _lastHourHeight = WorklogTimelineViewModel.DefaultHourHeight;
        private double? _anchorY;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorklogTimelineView"/> class.
        /// </summary>
        public WorklogTimelineView()
        {
            this.InitializeComponent();
            this.DataContextChanged += this.OnDataContextChanged;

            // Tunnel, so the scroll viewer does not scroll before Ctrl + wheel is turned into a zoom.
            this.AddHandler(InputElement.PointerWheelChangedEvent, this.OnPointerWheelChanged, RoutingStrategies.Tunnel);
            this.AddHandler(InputElement.KeyDownEvent, this.OnKeyDown, RoutingStrategies.Bubble);
        }

        private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (!e.KeyModifiers.HasFlag(KeyModifiers.Control) || this._viewModel is null || e.Delta.Y == 0)
            {
                return;
            }

            e.Handled = true;
            var scroller = this.FindControl<ScrollViewer>("Scroller");
            if (scroller is null)
            {
                return;
            }

            // Keep the moment under the pointer where it is while the scale changes.
            this._anchorY = e.GetPosition(scroller).Y;
            if (e.Delta.Y > 0)
            {
                this._viewModel.ZoomIn();
            }
            else
            {
                this._viewModel.ZoomOut();
            }
        }

        private void KeepAnchorAfterZoom()
        {
            if (this._viewModel is null || this.FindControl<ScrollViewer>("Scroller") is not { } scroller)
            {
                return;
            }

            var oldHeight = this._lastHourHeight;
            var newHeight = this._viewModel.HourHeight;
            this._lastHourHeight = newHeight;
            if (scroller.Viewport.Height <= 0)
            {
                // Not laid out yet, so there is nothing to keep in place: start at the first entry.
                this._anchorY = null;
                this.ScrollToFirstEntry();
                return;
            }

            // The wheel anchors on the pointer; buttons and keys anchor on the middle of what is visible.
            var anchorY = this._anchorY ?? (scroller.Viewport.Height / 2);
            this._anchorY = null;
            this.ScrollTo(WorklogTimelineViewModel.AnchoredOffset(scroller.Offset.Y, anchorY, oldHeight, newHeight));
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (!e.KeyModifiers.HasFlag(KeyModifiers.Control) || this._viewModel is null)
            {
                return;
            }

            switch (e.Key)
            {
                case Key.OemPlus:
                case Key.Add:
                    this._viewModel.ZoomIn();
                    e.Handled = true;
                    break;
                case Key.OemMinus:
                case Key.Subtract:
                    this._viewModel.ZoomOut();
                    e.Handled = true;
                    break;
                case Key.D0:
                case Key.NumPad0:
                    this._viewModel.ResetZoom();
                    e.Handled = true;
                    break;
            }
        }

        private void ScrollTo(double offset) =>
            Dispatcher.UIThread.Post(
                () =>
                {
                    if (this.FindControl<ScrollViewer>("Scroller") is { } scroller)
                    {
                        scroller.Offset = new Vector(0, offset);
                    }
                },
                DispatcherPriority.Loaded);

        private void OnDataContextChanged(object? sender, System.EventArgs e)
        {
            if (this._viewModel is not null)
            {
                this._viewModel.PropertyChanged -= this.OnViewModelChanged;
            }

            this._viewModel = this.DataContext as WorklogTimelineViewModel;
            if (this._viewModel is not null)
            {
                this._lastHourHeight = this._viewModel.HourHeight;
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
            else if (e.PropertyName == nameof(WorklogTimelineViewModel.HourHeight))
            {
                this.KeepAnchorAfterZoom();
            }
        }

        private void ScrollToFirstEntry()
        {
            if (this._viewModel is null)
            {
                return;
            }

            // Wait for the new blocks to be laid out before moving the viewport; the scale is read when it runs.
            Dispatcher.UIThread.Post(
                () =>
                {
                    if (this._viewModel is not null && this.FindControl<ScrollViewer>("Scroller") is { } scroller)
                    {
                        scroller.Offset = new Vector(0, this._viewModel.ScrollTargetMinute / 60 * this._viewModel.HourHeight);
                    }
                },
                DispatcherPriority.Loaded);
        }
    }
}
