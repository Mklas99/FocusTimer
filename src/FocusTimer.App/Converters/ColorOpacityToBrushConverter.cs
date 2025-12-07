using System;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;

namespace FocusTimer.App.Converters
{
    /// <summary>
    /// Converts Color + local opacity + overall opacity to a SolidColorBrush.
    /// </summary>
    public class ColorOpacityToBrushConverter : IMultiValueConverter
    {
        public static readonly ColorOpacityToBrushConverter Instance = new();

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 3)
                return Avalonia.Data.BindingNotification.UnsetValue;

            // Accept color as Avalonia.Media.Color or hex string
            Avalonia.Media.Color color;
            if (values[0] is Avalonia.Media.Color c)
                color = c;
            else if (values[0] is string s && TryParseColor(s, out var parsed))
                color = parsed;
            else
                return Avalonia.Data.BindingNotification.UnsetValue;

            if (!TryGetDouble(values[1], out var localOpacity) || !TryGetDouble(values[2], out var overallOpacity))
                return Avalonia.Data.BindingNotification.UnsetValue;

            var finalOpacity = Clamp01(localOpacity) * Clamp01(overallOpacity);
            var brush = new SolidColorBrush(color, finalOpacity);
            return brush;
        }

        private static bool TryGetDouble(object? value, out double result)
        {
            if (value is double d) { result = d; return true; }
            if (value is float f) { result = f; return true; }
            if (value is string s && double.TryParse(s, out var parsed)) { result = parsed; return true; }
            result = 1.0;
            return false;
        }

        private static double Clamp01(double v) => v < 0 ? 0 : v > 1 ? 1 : v;

        private static bool TryParseColor(string hex, out Avalonia.Media.Color color)
        {
            try
            {
                color = Avalonia.Media.Color.Parse(hex);
                return true;
            }
            catch
            {
                color = default;
                return false;
            }
        }
    }
}
