namespace FocusTimer.Core.Services;

using FocusTimer.Core.Models;

/// <summary>Evaluates an ordered list of window rules; shared by exclusion, segmentation, and project rules.</summary>
public static class WindowRuleMatcher
{
    /// <summary>Returns the first rule in list order that matches the window.</summary>
    /// <param name="rules">The ordered rules.</param>
    /// <param name="window">The sampled window, or null.</param>
    /// <returns>The first matching rule, or null.</returns>
    public static WindowMatchRule? FindFirst(IEnumerable<WindowMatchRule>? rules, ActiveWindowInfo? window)
    {
        if (rules is null || window is null)
        {
            return null;
        }

        foreach (var rule in rules)
        {
            if (rule is not null && rule.Matches(window))
            {
                return rule;
            }
        }

        return null;
    }
}
