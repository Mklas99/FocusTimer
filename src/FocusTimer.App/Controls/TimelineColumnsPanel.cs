namespace FocusTimer.App.Controls
{
    using System;
    using Avalonia;
    using Avalonia.Controls;
    using FocusTimer.App.ViewModels;

    /// <summary>
    /// Lays out the column headers and column separators of a grouped timeline across the same equally wide columns
    /// as <see cref="TimelinePanel"/>, so they line up with the blocks below. Each child's data context is its
    /// <see cref="TimelineGroupViewModel"/>.
    /// </summary>
    public class TimelineColumnsPanel : Panel
    {
        /// <summary>Identifies <see cref="ColumnCount"/>.</summary>
        public static readonly StyledProperty<int> ColumnCountProperty =
            AvaloniaProperty.Register<TimelineColumnsPanel, int>(nameof(ColumnCount), 1);

        static TimelineColumnsPanel()
        {
            AffectsMeasure<TimelineColumnsPanel>(ColumnCountProperty);
            AffectsArrange<TimelineColumnsPanel>(ColumnCountProperty);
        }

        /// <summary>Gets or sets the number of columns.</summary>
        public int ColumnCount
        {
            get => this.GetValue(ColumnCountProperty);
            set => this.SetValue(ColumnCountProperty, value);
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            var width = TimelinePanel.ContentWidth(availableSize.Width, this.ColumnCount);
            var columnWidth = width / Math.Max(1, this.ColumnCount);
            var height = 0.0;
            foreach (var child in this.Children)
            {
                child.Measure(new Size(columnWidth, double.IsInfinity(availableSize.Height) ? double.PositiveInfinity : availableSize.Height));
                height = Math.Max(height, child.DesiredSize.Height);
            }

            return new Size(width, height);
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var columnWidth = finalSize.Width / Math.Max(1, this.ColumnCount);
            foreach (var child in this.Children)
            {
                var index = child.DataContext is TimelineGroupViewModel group ? group.Index : 0;
                child.Arrange(new Rect(index * columnWidth, 0, columnWidth, finalSize.Height));
            }

            return finalSize;
        }
    }
}
