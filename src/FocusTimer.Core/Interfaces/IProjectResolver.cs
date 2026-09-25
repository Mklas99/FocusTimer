namespace FocusTimer.Core.Interfaces;

using FocusTimer.Core.Models;

/// <summary>Decides which project an entry belongs to when it is summarized.</summary>
public interface IProjectResolver
{
    /// <summary>Resolves the project for an entry.</summary>
    /// <param name="entry">The entry to resolve.</param>
    /// <returns>The project name, or null when the entry has no project.</returns>
    string? Resolve(TimeEntry entry);
}
