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
        /// <summary>The height of one hour on the axis, in device-independent pixels.</summary>
        public const double HourHeight = 60;

        /// <summary>The smallest height of a block, so very short entries stay visible and clickable.</summary>
        public const double MinimumBlockHeight = 20;

        private const double LaneGap = 4;

        /// <summary>Gets the total height of the axis: 24 hours.</summary>
        public static double AxisHeight => 24 * HourHeight;

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            var width = double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width;
            foreach (var child in this.Children)
            {
                if (child.DataContext is TimelineBlockViewModel block)
                {
                    child.Measure(new Size(LaneWidth(width, block), BlockHeight(block)));
                }
            }

            return new Size(width, AxisHeight);
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
                var y = block.StartMinute / 60 * HourHeight;
                child.Arrange(new Rect(x, y, laneWidth, BlockHeight(block)));
            }

            return finalSize;
        }

        private static double LaneWidth(double totalWidth, TimelineBlockViewModel block) =>
            Math.Max(0, (totalWidth - (LaneGap * (block.LaneCount - 1))) / Math.Max(1, block.LaneCount));

        private static double BlockHeight(TimelineBlockViewModel block) =>
            Math.Max(MinimumBlockHeight, block.LengthMinutes / 60 * HourHeight);
    }
}
