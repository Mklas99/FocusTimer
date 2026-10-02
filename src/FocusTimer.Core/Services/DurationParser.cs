namespace FocusTimer.Core.Services;

using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>Reads a user-typed duration such as "2h 30m", "45m", "1h" or "2.5h" in whole minutes.</summary>
public static partial class DurationParser
{
    /// <summary>Gets an example shown with every rejection message.</summary>
    public const string ExampleText = "Enter a duration like 2h 30m, 45m, or 2.5h.";

    private static readonly TimeSpan MaxDuration = TimeSpan.FromHours(24);

    /// <summary>Tries to read a duration.</summary>
    /// <param name="text">The typed text.</param>
    /// <param name="duration">The duration in whole minutes when reading succeeds.</param>
    /// <param name="error">A message for the user when reading fails.</param>
    /// <returns>True when the text is a usable, positive duration of at most 24 hours.</returns>
    public static bool TryParse(string? text, out TimeSpan duration, out string error)
    {
        duration = TimeSpan.Zero;
        var trimmed = text?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            error = "A duration is required. " + ExampleText;
            return false;
        }

        double minutes;
        var decimalHours = DecimalHours().Match(trimmed);
        var hoursAndMinutes = HoursAndMinutes().Match(trimmed);
        if (decimalHours.Success)
        {
            var hours = double.Parse(decimalHours.Groups[1].Value.Replace(',', '.'), CultureInfo.InvariantCulture);
            minutes = Math.Round(hours * 60, MidpointRounding.AwayFromZero);
        }
        else if (hoursAndMinutes.Success && (hoursAndMinutes.Groups[1].Success || hoursAndMinutes.Groups[2].Success))
        {
            var hours = hoursAndMinutes.Groups[1].Success ? double.Parse(hoursAndMinutes.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
            var mins = hoursAndMinutes.Groups[2].Success ? double.Parse(hoursAndMinutes.Groups[2].Value, CultureInfo.InvariantCulture) : 0;
            minutes = (hours * 60) + mins;
        }
        else
        {
            error = $"\"{trimmed}\" is not a duration. " + ExampleText;
            return false;
        }

        if (minutes < 1)
        {
            error = "The duration must be at least one minute. " + ExampleText;
            return false;
        }

        var value = TimeSpan.FromMinutes(minutes);
        if (value > MaxDuration)
        {
            error = "The duration cannot be longer than 24 hours.";
            return false;
        }

        duration = value;
        error = string.Empty;
        return true;
    }

    /// <summary>Formats a duration the way the parser reads it, for example "2h 30m".</summary>
    /// <param name="duration">The duration to format; seconds are not shown.</param>
    /// <returns>The text.</returns>
    public static string Format(TimeSpan duration)
    {
        var totalMinutes = (int)Math.Round(duration.TotalMinutes, MidpointRounding.AwayFromZero);
        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;
        return hours == 0 ? $"{minutes}m" : minutes == 0 ? $"{hours}h" : $"{hours}h {minutes}m";
    }

    [GeneratedRegex(@"^(\d+(?:[.,]\d+)?)\s*h$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DecimalHours();

    [GeneratedRegex(@"^(?:(\d+)\s*h)?\s*(?:(\d+)\s*m)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HoursAndMinutes();
}
