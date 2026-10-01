namespace FocusTimer.Core.Services;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

/// <summary>Uses the project label stored with the entry when it was tracked.</summary>
public sealed class StoredProjectResolver : IProjectResolver
{
    /// <inheritdoc/>
    public string? Resolve(TimeEntry entry) => entry.ProjectTag;
}
