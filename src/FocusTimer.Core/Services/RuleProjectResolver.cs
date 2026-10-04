namespace FocusTimer.Core.Services;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

/// <summary>
/// Keeps an explicit project, otherwise labels automatically captured entries with the first matching project rule.
/// Resolution happens at read time, so rule edits apply to existing entries and nothing stored changes.
/// </summary>
public sealed class RuleProjectResolver : IProjectResolver
{
    private readonly IProjectRuleProvider _rules;

    /// <summary>Initializes a new instance of the <see cref="RuleProjectResolver"/> class.</summary>
    /// <param name="rules">The applied rules.</param>
    public RuleProjectResolver(IProjectRuleProvider rules)
    {
        this._rules = rules;
    }

    /// <inheritdoc/>
    public string? Resolve(TimeEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.ProjectTag) || entry.CaptureSource != CaptureSource.ActiveWindow)
        {
            return entry.ProjectTag;
        }

        var window = new ActiveWindowInfo { ProcessName = entry.AppName, WindowTitle = entry.WindowTitle };
        foreach (var rule in this._rules.Rules)
        {
            if (rule.Matches(window))
            {
                return rule.ProjectName!.Trim();
            }
        }

        return entry.ProjectTag;
    }
}
