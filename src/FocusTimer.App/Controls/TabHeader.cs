namespace FocusTimer.App.Controls
{
    using Avalonia;
    using Avalonia.Automation;
    using Avalonia.Controls;
    using Avalonia.Layout;
    using Avalonia.Media;
    using Material.Icons;
    using Material.Icons.Avalonia;

    /// <summary>
    /// Tab header with an outlined icon beside its label. A hidden semibold copy of the label reserves the
    /// selected width so the tab row does not shift when the selection changes.
    /// </summary>
    public sealed class TabHeader : UserControl
    {
        /// <summary>
        /// Defines the <see cref="Icon"/> property.
        /// </summary>
        public static readonly StyledProperty<MaterialIconKind> IconProperty =
            AvaloniaProperty.Register<TabHeader, MaterialIconKind>(nameof(Icon));

        /// <summary>
        /// Defines the <see cref="Text"/> property.
        /// </summary>
        public static readonly StyledProperty<string?> TextProperty =
            AvaloniaProperty.Register<TabHeader, string?>(nameof(Text));

        private readonly MaterialIcon _icon;
        private readonly TextBlock _label;
        private readonly TextBlock _widthReserve;

        /// <summary>
        /// Initializes a new instance of the <see cref="TabHeader"/> class.
        /// </summary>
        public TabHeader()
        {
            this._icon = new MaterialIcon
            {
                Width = 18,
                Height = 18,
                VerticalAlignment = VerticalAlignment.Center,
            };
            this._label = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                Classes = { "tab-label" },
            };
            this._widthReserve = new TextBlock
            {
                FontWeight = FontWeight.SemiBold,
                Opacity = 0,
                IsHitTestVisible = false,
                VerticalAlignment = VerticalAlignment.Center,
            };
            AutomationProperties.SetAccessibilityView(this._widthReserve, AccessibilityView.Raw);
            AutomationProperties.SetAccessibilityView(this._icon, AccessibilityView.Raw);

            var labelHost = new Grid { VerticalAlignment = VerticalAlignment.Center };
            labelHost.Children.Add(this._widthReserve);
            labelHost.Children.Add(this._label);

            this.Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Children = { this._icon, labelHost },
            };
        }

        /// <summary>
        /// Gets or sets the outlined Material icon shown beside the label.
        /// </summary>
        public MaterialIconKind Icon
        {
            get => this.GetValue(IconProperty);
            set => this.SetValue(IconProperty, value);
        }

        /// <summary>
        /// Gets or sets the tab label.
        /// </summary>
        public string? Text
        {
            get => this.GetValue(TextProperty);
            set => this.SetValue(TextProperty, value);
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == IconProperty)
            {
                this._icon.Kind = this.Icon;
            }
            else if (change.Property == TextProperty)
            {
                this._label.Text = this.Text;
                this._widthReserve.Text = this.Text;
            }
        }
    }
}
