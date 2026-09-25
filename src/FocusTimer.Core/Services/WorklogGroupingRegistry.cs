namespace FocusTimer.Core.Services;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

/// <summary>Holds every grouping a summary can use, in the order they are offered to the user.</summary>
public sealed class WorklogGroupingRegistry
{
    private readonly List<IWorklogGrouping> _groupings;

    /// <summary>Initializes a new instance of the <see cref="WorklogGroupingRegistry"/> class.</summary>
    /// <param name="groupings">The groupings, in display order. Ids must be unique ignoring letter case.</param>
    public WorklogGroupingRegistry(IEnumerable<IWorklogGrouping> groupings)
    {
        this._groupings = [.. groupings];
        var duplicate = this._groupings
            .GroupBy(g => g.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException($"Duplicate grouping id '{duplicate.Key}'.", nameof(groupings));
        }
    }

    /// <summary>Gets the groupings in display order.</summary>
    public IReadOnlyList<IWorklogGrouping> All => this._groupings;

    /// <summary>Finds a grouping by id.</summary>
    /// <param name="id">The grouping id, ignoring letter case.</param>
    /// <param name="grouping">The grouping when found.</param>
    /// <returns>True when a grouping with that id exists.</returns>
    public bool TryGet(string id, out IWorklogGrouping grouping)
    {
        var found = this._groupings.FirstOrDefault(g => string.Equals(g.Id, id, StringComparison.OrdinalIgnoreCase));
        grouping = found!;
        return found is not null;
    }
}
