namespace FocusTimer.Core.Services;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

/// <summary>Holds the applied project rules so views read them without reloading settings.</summary>
public sealed class ProjectRuleStore : IProjectRuleProvider
{
    private volatile IReadOnlyList<ProjectRule> _rules = Array.Empty<ProjectRule>();

    /// <summary>Occurs after the rule list changed.</summary>
    public event EventHandler? RulesChanged;

    /// <inheritdoc/>
    public IReadOnlyList<ProjectRule> Rules => this._rules;

    /// <summary>Replaces the applied rules; an identical list raises no event.</summary>
    /// <param name="rules">The rules, invalid ones are dropped.</param>
    public void Update(IEnumerable<ProjectRule>? rules)
    {
        ProjectRule[] valid = (rules ?? Enumerable.Empty<ProjectRule>()).Where(r => r is { IsValid: true }).ToArray();
        if (this._rules.SequenceEqual(valid))
        {
            return;
        }

        this._rules = valid;
        this.RulesChanged?.Invoke(this, EventArgs.Empty);
    }
}
