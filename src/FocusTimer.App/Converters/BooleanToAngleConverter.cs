using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FocusTimer.App.Converters;

/// <summary>
/// Converts a boolean to a chevron angle (0 for collapsed).
/// </summary>
public class BooleanToAngleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? 90d : 0d;
        return 0d;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
            return d == 90d;
        return false;
    }
}
