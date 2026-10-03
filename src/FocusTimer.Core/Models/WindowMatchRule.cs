namespace FocusTimer.Core.Models;

using FocusTimer.Core.Services;

/// <summary>
/// Identifies a foreground window by application name and/or window title.
/// Patterns are case-insensitive globs where <c>*</c> matches any run and <c>?</c> matches one character.
/// </summary>
/// <param name="AppPattern">The process-name pattern, or null/blank to ignore the application.</param>
/// <param name="TitlePattern">The window-title pattern, or null/blank to ignore the title.</param>
public sealed record WindowMatchRule(string? AppPattern, string? TitlePattern)
{
    /// <summary>Gets a value indicating whether the rule has at least one non-blank pattern.</summary>
    public bool IsValid => !string.IsNullOrWhiteSpace(this.AppPattern) || !string.IsNullOrWhiteSpace(this.TitlePattern);

    /// <summary>Determines whether the rule matches a window; every non-blank pattern must match.</summary>
    /// <param name="window">The sampled window, or null when none is available.</param>
    /// <returns>True when the rule is valid and matches.</returns>
    public bool Matches(ActiveWindowInfo? window)
    {
        if (window is null || !this.IsValid)
        {
            return false;
        }

        return (string.IsNullOrWhiteSpace(this.AppPattern) || MatchesApplication(this.AppPattern.Trim(), window.ProcessName))
            && (string.IsNullOrWhiteSpace(this.TitlePattern) || GlobMatcher.IsMatch(
                    this.TitlePattern.Trim(),
                    window.WindowTitle));
    }

    // The extension is optional on both sides. It is only dropped from a pattern whose stem names something
    // (so "keepass.exe" matches "KeePass"), never from a wildcard-only stem, so "*.exe" stays "any .exe".
    private static bool MatchesApplication(string pattern, string processName)
    {
        var name = StripExecutableExtension(processName);
        if (GlobMatcher.IsMatch(pattern, processName) || GlobMatcher.IsMatch(pattern, name))
        {
            return true;
        }

        var stem = StripExecutableExtension(pattern);
        return stem.Length != pattern.Length && stem.Any(c => c is not '*' and not '?')
            && GlobMatcher.IsMatch(stem, name);
    }

    private static string StripExecutableExtension(string name) =>
        name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;
}
