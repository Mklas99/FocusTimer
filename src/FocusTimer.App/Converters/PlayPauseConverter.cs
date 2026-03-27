namespace FocusTimer.App.Converters
{
    using System;
    using System.Globalization;
    using Avalonia.Data.Converters;

    /// <summary>
    /// Converts IsRunning boolean to "Pause" or "Play" text.
    /// </summary>
    public class PlayPauseConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Return MaterialIcon name depending on isRunning
            if (value is bool isRunning)
            {
                return isRunning ? "Pause" : "Play";
            }

            return "Play";
        }

        /// <inheritdoc/>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
