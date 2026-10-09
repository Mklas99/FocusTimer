namespace FocusTimer.App.Controls
{
    using System;
    using Avalonia;
    using Avalonia.Controls;

    /// <summary>
    /// Arranges peer-group cards side by side when the available width allows and stacks them otherwise.
    /// </summary>
    public sealed class ResponsiveCardsPanel : Panel
    {
        /// <summary>
        /// Defines the <see cref="TwoColumnMinWidth"/> property.
        /// </summary>
        public static readonly StyledProperty<double> TwoColumnMinWidthProperty =
            AvaloniaProperty.Register<ResponsiveCardsPanel, double>(nameof(TwoColumnMinWidth), 560);

        /// <summary>
        /// Defines the <see cref="Spacing"/> property.
        /// </summary>
        public static readonly StyledProperty<double> SpacingProperty =
            AvaloniaProperty.Register<ResponsiveCardsPanel, double>(nameof(Spacing), 16);

        static ResponsiveCardsPanel()
        {
            AffectsMeasure<ResponsiveCardsPanel>(TwoColumnMinWidthProperty, SpacingProperty);
        }

        /// <summary>
        /// Gets or sets the available width at which cards sit side by side.
        /// </summary>
        public double TwoColumnMinWidth
        {
            get => this.GetValue(TwoColumnMinWidthProperty);
            set => this.SetValue(TwoColumnMinWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the gap between cards, horizontally and vertically.
        /// </summary>
        public double Spacing
        {
            get => this.GetValue(SpacingProperty);
            set => this.SetValue(SpacingProperty, value);
        }

        /// <summary>
        /// Returns whether cards share a row for the given available width.
        /// </summary>
        /// <param name="availableWidth">The available width in logical pixels.</param>
        /// <param name="cardCount">The number of visible cards.</param>
        /// <returns><see langword="true"/> when cards are arranged side by side.</returns>
        public bool UsesColumns(double availableWidth, int cardCount) =>
            cardCount > 1 && !double.IsInfinity(availableWidth) && availableWidth >= this.TwoColumnMinWidth;

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            Control[] visible = this.VisibleChildren();
            if (this.UsesColumns(availableSize.Width, visible.Length))
            {
                double columnWidth = (availableSize.Width - this.Spacing) / 2;
                double height = 0;
                foreach (Control child in visible)
                {
                    child.Measure(new Size(columnWidth, double.PositiveInfinity));
                    height = Math.Max(height, child.DesiredSize.Height);
                }

                return new Size(availableSize.Width, height);
            }

            double stackedHeight = 0;
            double width = 0;
            for (int i = 0; i < visible.Length; i++)
            {
                visible[i].Measure(new Size(availableSize.Width, double.PositiveInfinity));
                stackedHeight += visible[i].DesiredSize.Height + (i > 0 ? this.Spacing : 0);
                width = Math.Max(width, visible[i].DesiredSize.Width);
            }

            return new Size(double.IsInfinity(availableSize.Width) ? width : availableSize.Width, stackedHeight);
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            Control[] visible = this.VisibleChildren();
            if (this.UsesColumns(finalSize.Width, visible.Length))
            {
                double columnWidth = (finalSize.Width - this.Spacing) / 2;
                double rowHeight = 0;
                foreach (Control child in visible)
                {
                    rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
                }

                for (int i = 0; i < visible.Length; i++)
                {
                    double x = (i % 2) * (columnWidth + this.Spacing);
                    visible[i].Arrange(new Rect(x, 0, columnWidth, rowHeight));
                }

                return finalSize;
            }

            double y = 0;
            foreach (Control child in visible)
            {
                child.Arrange(new Rect(0, y, finalSize.Width, child.DesiredSize.Height));
                y += child.DesiredSize.Height + this.Spacing;
            }

            return finalSize;
        }

        private Control[] VisibleChildren()
        {
            var result = new System.Collections.Generic.List<Control>(this.Children.Count);
            foreach (Control child in this.Children)
            {
                if (child.IsVisible)
                {
                    result.Add(child);
                }
            }

            return [.. result];
        }
    }
}
