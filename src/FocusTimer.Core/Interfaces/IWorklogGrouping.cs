namespace FocusTimer.Core.Interfaces;

using FocusTimer.Core.Models;

/// <summary>Decides which row an entry is counted in. Register new implementations to add a grouping.</summary>
public interface IWorklogGrouping
{
    /// <summary>Gets the stable id used in summary requests.</summary>
    string Id { get; }

    /// <summary>Gets the name shown to the user.</summary>
    string DisplayName { get; }

    /// <summary>Selects the group for an entry.</summary>
    /// <param name="entry">The entry to classify.</param>
    /// <param name="context">Resolved details about the entry.</param>
    /// <returns>The entry's group.</returns>
    GroupKey Select(TimeEntry entry, GroupingContext context);
}
