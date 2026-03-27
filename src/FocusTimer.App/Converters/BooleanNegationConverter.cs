namespace FocusTimer.App.Converters
{
    using System;
    using System.Globalization;
    using Avalonia.Data.Converters;

    /// <summary>
    /// Inverts a boolean value. Useful for visibility bindings where you want
    /// to show one view when a flag is false and another when true.
    /// </summary>
    public class BooleanNegationConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return !b;
            }

            return Avalonia.AvaloniaProperty.UnsetValue;
        }

        /// <inheritdoc/>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return !b;
            }

            return Avalonia.AvaloniaProperty.UnsetValue;
        }
    }
}
