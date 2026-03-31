namespace FocusTimer.App.ViewModels
{
    using System;
    using Avalonia;
    using Avalonia.Media;
    using ReactiveUI;

    /// <summary>
    /// View model for the compact color picker dialog.
    /// </summary>
    public class ColorPickerWindowViewModel : ReactiveObject
    {
        private bool _isUpdatingFromColor;
        private string _colorHex;
        private double _alpha;
        private double _red;
        private double _green;
        private double _blue;
        private double _hue;
        private double _saturation;
        private double _value;
        private IBrush _previewBrush;
        private IBrush _previewTextBrush;
        private IBrush _saturationValueBaseBrush;
        private IBrush _alphaGradientBrush;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorPickerWindowViewModel"/> class.
        /// </summary>
        /// <param name="initialColor">The initial color value.</param>
        public ColorPickerWindowViewModel(string initialColor)
        {
            var color = ParseColor(initialColor);
            this._colorHex = FormatColor(color);
            this._previewBrush = new SolidColorBrush(color);
            this._previewTextBrush = new SolidColorBrush(Colors.White);
            this._saturationValueBaseBrush = new SolidColorBrush(color);
            this._alphaGradientBrush = CreateAlphaGradientBrush(color);
            this.UpdateFromColor(color);
        }

        /// <summary>
        /// Gets or sets the color as hex text.
        /// </summary>
        public string ColorHex
        {
            get => this._colorHex;
            set
            {
                if (string.Equals(this._colorHex, value, StringComparison.Ordinal))
                {
                    return;
                }

                this.RaiseAndSetIfChanged(ref this._colorHex, value);
                if (!this._isUpdatingFromColor && TryParseColor(value, out var color))
                {
                    this.UpdateFromColor(color);
                }
            }
        }

        /// <summary>
        /// Gets or sets the alpha channel.
        /// </summary>
        public double Alpha
        {
            get => this._alpha;
            set
            {
                var clamped = ClampByteChannel(value);
                if (Math.Abs(this._alpha - clamped) > double.Epsilon)
                {
                    this.RaiseAndSetIfChanged(ref this._alpha, clamped);
                    this.UpdateFromRgbChannels();
                }
            }
        }

        /// <summary>
        /// Gets or sets the red channel.
        /// </summary>
        public double Red
        {
            get => this._red;
            set
            {
                var clamped = ClampByteChannel(value);
                if (Math.Abs(this._red - clamped) > double.Epsilon)
                {
                    this.RaiseAndSetIfChanged(ref this._red, clamped);
                    this.UpdateFromRgbChannels();
                }
            }
        }

        /// <summary>
        /// Gets or sets the green channel.
        /// </summary>
        public double Green
        {
            get => this._green;
            set
            {
                var clamped = ClampByteChannel(value);
                if (Math.Abs(this._green - clamped) > double.Epsilon)
                {
                    this.RaiseAndSetIfChanged(ref this._green, clamped);
                    this.UpdateFromRgbChannels();
                }
            }
        }

        /// <summary>
        /// Gets or sets the blue channel.
        /// </summary>
        public double Blue
        {
            get => this._blue;
            set
            {
                var clamped = ClampByteChannel(value);
                if (Math.Abs(this._blue - clamped) > double.Epsilon)
                {
                    this.RaiseAndSetIfChanged(ref this._blue, clamped);
                    this.UpdateFromRgbChannels();
                }
            }
        }

        /// <summary>
        /// Gets or sets the hue component in degrees.
        /// </summary>
        public double Hue
        {
            get => this._hue;
            set
            {
                var normalized = NormalizeHue(value);
                if (Math.Abs(this._hue - normalized) > double.Epsilon)
                {
                    this.RaiseAndSetIfChanged(ref this._hue, normalized);
                    this.UpdateFromHsvChannels();
                }
            }
        }

        /// <summary>
        /// Gets or sets the saturation component in the range 0-1.
        /// </summary>
        public double Saturation
        {
            get => this._saturation;
            set
            {
                var clamped = Clamp01(value);
                if (Math.Abs(this._saturation - clamped) > double.Epsilon)
                {
                    this.RaiseAndSetIfChanged(ref this._saturation, clamped);
                    this.RaisePropertyChanged(nameof(this.SaturationPercent));
                    this.UpdateFromHsvChannels();
                }
            }
        }

        /// <summary>
        /// Gets or sets the saturation percentage in the range 0-100.
        /// </summary>
        public double SaturationPercent
        {
            get => Math.Round(this.Saturation * 100, MidpointRounding.AwayFromZero);
            set => this.Saturation = value / 100d;
        }

        /// <summary>
        /// Gets or sets the value component in the range 0-1.
        /// </summary>
        public double Value
        {
            get => this._value;
            set
            {
                var clamped = Clamp01(value);
                if (Math.Abs(this._value - clamped) > double.Epsilon)
                {
                    this.RaiseAndSetIfChanged(ref this._value, clamped);
                    this.RaisePropertyChanged(nameof(this.ValuePercent));
                    this.UpdateFromHsvChannels();
                }
            }
        }

        /// <summary>
        /// Gets or sets the value percentage in the range 0-100.
        /// </summary>
        public double ValuePercent
        {
            get => Math.Round(this.Value * 100, MidpointRounding.AwayFromZero);
            set => this.Value = value / 100d;
        }

        /// <summary>
        /// Gets the current preview brush.
        /// </summary>
        public IBrush PreviewBrush
        {
            get => this._previewBrush;
            private set => this.RaiseAndSetIfChanged(ref this._previewBrush, value);
        }

        /// <summary>
        /// Gets the brush used for preview text.
        /// </summary>
        public IBrush PreviewTextBrush
        {
            get => this._previewTextBrush;
            private set => this.RaiseAndSetIfChanged(ref this._previewTextBrush, value);
        }

        /// <summary>
        /// Gets the base hue brush for the saturation/value surface.
        /// </summary>
        public IBrush SaturationValueBaseBrush
        {
            get => this._saturationValueBaseBrush;
            private set => this.RaiseAndSetIfChanged(ref this._saturationValueBaseBrush, value);
        }

        /// <summary>
        /// Gets the alpha gradient brush.
        /// </summary>
        public IBrush AlphaGradientBrush
        {
            get => this._alphaGradientBrush;
            private set => this.RaiseAndSetIfChanged(ref this._alphaGradientBrush, value);
        }

        /// <summary>
        /// Gets the normalized selected color.
        /// </summary>
        public string SelectedColorHex => FormatColor(this.CreateCurrentColor());

        private static double ClampByteChannel(double value)
        {
            return Math.Clamp(Math.Round(value, MidpointRounding.AwayFromZero), 0, 255);
        }

        private static double Clamp01(double value)
        {
            return Math.Clamp(value, 0, 1);
        }

        private static double NormalizeHue(double value)
        {
            var normalized = value % 360;
            if (normalized < 0)
            {
                normalized += 360;
            }

            return normalized;
        }

        private static Color ParseColor(string value)
        {
            return TryParseColor(value, out var color) ? color : Color.Parse("#FFFFFFFF");
        }

        private static bool TryParseColor(string value, out Color color)
        {
            try
            {
                color = Color.Parse(value);
                return true;
            }
            catch (FormatException)
            {
                color = default;
                return false;
            }
            catch (ArgumentException)
            {
                color = default;
                return false;
            }
        }

        private static string FormatColor(Color color)
        {
            return color.A == byte.MaxValue
                ? $"#{color.R:X2}{color.G:X2}{color.B:X2}"
                : $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private static Color CreateContrastColor(Color color)
        {
            var luminance = ((0.2126 * color.R) + (0.7152 * color.G) + (0.0722 * color.B)) / 255;
            return luminance > 0.58 ? Colors.Black : Colors.White;
        }

        private static IBrush CreateAlphaGradientBrush(Color color)
        {
            return new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.FromArgb(0, color.R, color.G, color.B), 0),
                    new GradientStop(Color.FromArgb(byte.MaxValue, color.R, color.G, color.B), 1),
                },
            };
        }

        private static Color ColorFromHsv(double hue, double saturation, double value, byte alpha)
        {
            hue = NormalizeHue(hue);
            saturation = Clamp01(saturation);
            value = Clamp01(value);

            if (saturation <= double.Epsilon)
            {
                var channel = (byte)Math.Round(value * 255, MidpointRounding.AwayFromZero);
                return Color.FromArgb(alpha, channel, channel, channel);
            }

            var sector = hue / 60;
            var sectorIndex = (int)Math.Floor(sector);
            var fractional = sector - sectorIndex;
            var p = value * (1 - saturation);
            var q = value * (1 - (saturation * fractional));
            var t = value * (1 - (saturation * (1 - fractional)));

            var (red, green, blue) = sectorIndex switch
            {
                0 => (value, t, p),
                1 => (q, value, p),
                2 => (p, value, t),
                3 => (p, q, value),
                4 => (t, p, value),
                _ => (value, p, q),
            };

            return Color.FromArgb(
                alpha,
                (byte)Math.Round(red * 255, MidpointRounding.AwayFromZero),
                (byte)Math.Round(green * 255, MidpointRounding.AwayFromZero),
                (byte)Math.Round(blue * 255, MidpointRounding.AwayFromZero));
        }

        private static void ToHsv(Color color, double fallbackHue, out double hue, out double saturation, out double value)
        {
            var red = color.R / 255d;
            var green = color.G / 255d;
            var blue = color.B / 255d;
            var max = Math.Max(red, Math.Max(green, blue));
            var min = Math.Min(red, Math.Min(green, blue));
            var delta = max - min;

            value = max;
            saturation = max <= double.Epsilon ? 0 : delta / max;

            if (delta <= double.Epsilon)
            {
                hue = NormalizeHue(fallbackHue);
                return;
            }

            if (Math.Abs(max - red) <= double.Epsilon)
            {
                hue = 60 * (((green - blue) / delta) % 6);
            }
            else if (Math.Abs(max - green) <= double.Epsilon)
            {
                hue = 60 * (((blue - red) / delta) + 2);
            }
            else
            {
                hue = 60 * (((red - green) / delta) + 4);
            }

            hue = NormalizeHue(hue);
        }

        private Color CreateCurrentColor()
        {
            return Color.FromArgb(
                (byte)ClampByteChannel(this.Alpha),
                (byte)ClampByteChannel(this.Red),
                (byte)ClampByteChannel(this.Green),
                (byte)ClampByteChannel(this.Blue));
        }

        private void UpdateFromRgbChannels()
        {
            if (this._isUpdatingFromColor)
            {
                return;
            }

            this.UpdateFromColor(this.CreateCurrentColor(), this._hue);
        }

        private void UpdateFromHsvChannels()
        {
            if (this._isUpdatingFromColor)
            {
                return;
            }

            var color = ColorFromHsv(this.Hue, this.Saturation, this.Value, (byte)ClampByteChannel(this.Alpha));
            this.UpdateFromColor(color, this.Hue);
        }

        private void UpdateFromColor(Color color, double? preferredHue = null)
        {
            this._isUpdatingFromColor = true;

            try
            {
                ToHsv(color, preferredHue ?? this._hue, out var hue, out var saturation, out var value);

                this.Alpha = color.A;
                this.Red = color.R;
                this.Green = color.G;
                this.Blue = color.B;
                this.Hue = hue;
                this.Saturation = saturation;
                this.Value = value;
                this.ColorHex = FormatColor(color);
                this.PreviewBrush = new SolidColorBrush(color);
                this.PreviewTextBrush = new SolidColorBrush(CreateContrastColor(color));
                this.SaturationValueBaseBrush = new SolidColorBrush(ColorFromHsv(this.Hue, 1, 1, byte.MaxValue));
                this.AlphaGradientBrush = CreateAlphaGradientBrush(color);
                this.RaisePropertyChanged(nameof(this.SelectedColorHex));
                this.RaisePropertyChanged(nameof(this.SaturationPercent));
                this.RaisePropertyChanged(nameof(this.ValuePercent));
            }
            finally
            {
                this._isUpdatingFromColor = false;
            }
        }
    }
}
