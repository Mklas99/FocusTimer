namespace FocusTimer.App.Controls
{
    using System;
    using Avalonia;
    using Avalonia.Controls;
    using FocusTimer.App.ViewModels;

    /// <summary>
    /// Lays out timeline blocks along a vertical time axis: the vertical position and height follow the entry's
    /// time, overlapping entries share the width of their column as lanes, and a grouped timeline is split into
    /// equally wide columns. Each child's data context is its <see cref="TimelineBlockViewModel"/>.
    /// </summary>
    public class TimelinePanel : Panel
    {
        /// <summary>The height of one hour on the axis at the default zoom, in device-independent pixels.</summary>
        public const double DefaultHourHeight = 60;

        /// <summary>The smallest height of a block, so very short entries stay visible and clickable.</summary>
        public const double MinimumBlockHeight = WorklogTimelineViewModel.MinimumBlockHeight;

        /// <summary>The narrowest a group column gets; with more groups than fit, the timeline scrolls sideways.</summary>
        public const double MinColumnWidth = 150;

        /// <summary>Identifies <see cref="HourHeight"/>.</summary>
        public static readonly StyledProperty<double> HourHeightProperty =
            AvaloniaProperty.Register<TimelinePanel, double>(nameof(HourHeight), DefaultHourHeight);

        /// <summary>Identifies <see cref="ColumnCount"/>.</summary>
        public static readonly StyledProperty<int> ColumnCountProperty =
            AvaloniaProperty.Register<TimelinePanel, int>(nameof(ColumnCount), 1);

        private const double LaneGap = 4;
        private const double ColumnGap = 8;

        static TimelinePanel()
        {
            AffectsMeasure<TimelinePanel>(HourHeightProperty, ColumnCountProperty);
            AffectsArrange<TimelinePanel>(HourHeightProperty, ColumnCountProperty);
        }

        /// <summary>Gets or sets the height of one hour on the axis; the user zooms by changing it.</summary>
        public double HourHeight
        {
            get => this.GetValue(HourHeightProperty);
            set => this.SetValue(HourHeightProperty, value);
        }

        /// <summary>Gets or sets the number of group columns; one when the timeline is not grouped.</summary>
        public int ColumnCount
        {
            get => this.GetValue(ColumnCountProperty);
            set => this.SetValue(ColumnCountProperty, value);
        }

        /// <summary>Gets the total height of the axis: 24 hours.</summary>
        public double AxisHeight => 24 * this.HourHeight;

        /// <summary>
        /// Works out how wide the columns need to be: the available width, but not less than the minimum column width
        /// times the number of columns. A single column asks for nothing and takes whatever it is given.
        /// </summary>
        /// <param name="availableWidth">The width on offer; infinite inside a scroll viewer.</param>
        /// <param name="columnCount">The number of columns.</param>
        /// <returns>The width to use.</returns>
        public static double ContentWidth(double availableWidth, int columnCount)
        {
            var needed = columnCount > 1 ? columnCount * MinColumnWidth : 0;
            return double.IsInfinity(availableWidth) ? needed : Math.Max(availableWidth, needed);
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            var width = ContentWidth(availableSize.Width, this.ColumnCount);
            foreach (var child in this.Children)
            {
                if (child.DataContext is TimelineBlockViewModel block)
                {
                    child.Measure(new Size(LaneWidth((width / Math.Max(1, block.ColumnCount)) - (block.ColumnCount > 1 ? ColumnGap : 0), block), this.BlockHeight(block)));
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

                var columnWidth = finalSize.Width / Math.Max(1, block.ColumnCount);
                var columnGap = block.ColumnCount > 1 ? ColumnGap : 0;
                var laneWidth = LaneWidth(columnWidth - columnGap, block);
                var x = (block.Column * columnWidth) + (block.Lane * (laneWidth + LaneGap));
                var y = block.StartMinute / 60 * this.HourHeight;
                child.Arrange(new Rect(x, y, laneWidth, this.BlockHeight(block)));
            }

            return finalSize;
        }

        private static double LaneWidth(double columnWidth, TimelineBlockViewModel block) =>
            Math.Max(0, (columnWidth - (LaneGap * (block.LaneCount - 1))) / Math.Max(1, block.LaneCount));

        private double BlockHeight(TimelineBlockViewModel block) =>
            Math.Max(MinimumBlockHeight, block.LengthMinutes / 60 * this.HourHeight);
    }
}
