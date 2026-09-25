namespace FocusTimer.Core.Interfaces;

using FocusTimer.Core.Models;

/// <summary>Extra information a grouping may need about the entry it is classifying.</summary>
/// <param name="Project">The project resolved for the entry, or null when it has none.</param>
public sealed record GroupingContext(string? Project);
