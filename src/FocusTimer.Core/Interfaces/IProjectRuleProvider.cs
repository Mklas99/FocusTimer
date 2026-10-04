namespace FocusTimer.Core.Interfaces;

using FocusTimer.Core.Models;

/// <summary>Supplies the currently applied project rules at read time.</summary>
public interface IProjectRuleProvider
{
    /// <summary>Occurs after the applied rules changed.</summary>
    event EventHandler? RulesChanged;

    /// <summary>Gets the ordered project rules.</summary>
    IReadOnlyList<ProjectRule> Rules { get; }
}
