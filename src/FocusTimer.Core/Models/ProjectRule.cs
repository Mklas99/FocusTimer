namespace FocusTimer.Core.Models;

using FocusTimer.Core.Services;

/// <summary>Maps windows matching an application and/or title pattern to a project name.</summary>
/// <param name="AppPattern">The process-name pattern, or null/blank to ignore the application.</param>
/// <param name="TitlePattern">The window-title pattern, or null/blank to ignore the title.</param>
/// <param name="ProjectName">The project assigned to matching entries.</param>
public sealed record ProjectRule(string? AppPattern, string? TitlePattern, string? ProjectName)
{
    /// <summary>Gets a value indicating whether the rule has a pattern and a non-blank project name.</summary>
    public bool IsValid => !string.IsNullOrWhiteSpace(this.ProjectName) && this.WindowRule.IsValid;

    /// <summary>Gets the window rule formed by the two patterns.</summary>
    public WindowMatchRule WindowRule => new(this.AppPattern, this.TitlePattern);

    /// <summary>Determines whether the rule matches a window.</summary>
    /// <param name="window">The window, or null.</param>
    /// <returns>True when the rule is valid and matches.</returns>
    public bool Matches(ActiveWindowInfo? window) => this.IsValid && this.WindowRule.Matches(window);
}
