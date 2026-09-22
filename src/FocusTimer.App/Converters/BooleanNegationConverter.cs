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
            return value is bool b ? !b : Avalonia.AvaloniaProperty.UnsetValue;
        }

        /// <inheritdoc/>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Negation is its own inverse.
            return this.Convert(value, targetType, parameter, culture);
        }
    }
}
