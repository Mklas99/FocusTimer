namespace FocusTimer.App.Controls
{
    using System;
    using Avalonia;
    using Avalonia.Controls;
    using FocusTimer.App.ViewModels;

    /// <summary>
    /// Lays out timeline blocks along a vertical time axis: the vertical position and height follow the entry's
    /// time, and overlapping entries share the width as lanes. Each child's data context is its
    /// <see cref="TimelineBlockViewModel"/>.
    /// </summary>
    public class TimelinePanel : Panel
    {
        /// <summary>The height of one hour on the axis at the default zoom, in device-independent pixels.</summary>
        public const double DefaultHourHeight = 60;

        /// <summary>The smallest height of a block, so very short entries stay visible and clickable.</summary>
        public const double MinimumBlockHeight = FocusTimer.App.ViewModels.WorklogTimelineViewModel.MinimumBlockHeight;

        /// <summary>Identifies <see cref="HourHeight"/>.</summary>
        public static readonly StyledProperty<double> HourHeightProperty =
            AvaloniaProperty.Register<TimelinePanel, double>(nameof(HourHeight), DefaultHourHeight);

        private const double LaneGap = 4;

        static TimelinePanel()
        {
            AffectsMeasure<TimelinePanel>(HourHeightProperty);
            AffectsArrange<TimelinePanel>(HourHeightProperty);
        }

        /// <summary>Gets or sets the height of one hour on the axis; the user zooms by changing it.</summary>
        public double HourHeight
        {
            get => this.GetValue(HourHeightProperty);
            set => this.SetValue(HourHeightProperty, value);
        }

        /// <summary>Gets the total height of the axis: 24 hours.</summary>
        public double AxisHeight => 24 * this.HourHeight;

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            var width = double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width;
            foreach (var child in this.Children)
            {
                if (child.DataContext is TimelineBlockViewModel block)
                {
                    child.Measure(new Size(LaneWidth(width, block), this.BlockHeight(block)));
                }
            }

            return new Size(width, this.AxisHeight);
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (var child in this.Children)
            {
                if (child.DataContext is not TimelineBlockViewModel block)
                {
                    child.Arrange(new Rect(0, 0, 0, 0));
                    continue;
                }

                var laneWidth = LaneWidth(finalSize.Width, block);
                var x = block.Lane * (laneWidth + LaneGap);
                var y = block.StartMinute / 60 * this.HourHeight;
                child.Arrange(new Rect(x, y, laneWidth, this.BlockHeight(block)));
            }

            return finalSize;
        }

        private static double LaneWidth(double totalWidth, TimelineBlockViewModel block) =>
            Math.Max(0, (totalWidth - (LaneGap * (block.LaneCount - 1))) / Math.Max(1, block.LaneCount));

        private double BlockHeight(TimelineBlockViewModel block) =>
            Math.Max(MinimumBlockHeight, block.LengthMinutes / 60 * this.HourHeight);
    }
}
