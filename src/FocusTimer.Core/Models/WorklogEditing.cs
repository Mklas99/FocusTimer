#pragma warning disable

namespace FocusTimer.Core.Models;

/// <summary>Describes a manual entry the user wants to add.</summary>
/// <param name="Day">The local day the time belongs to.</param>
/// <param name="Start">The local start time of day.</param>
/// <param name="Duration">The elapsed time worked.</param>
/// <param name="WindowTitle">Optional free text; empty when not given.</param>
/// <param name="Project">Optional project text.</param>
public sealed record ManualEntryRequest(DateOnly Day, TimeOnly Start, TimeSpan Duration, string? WindowTitle, string? Project);

/// <summary>Describes the values the user may change on an existing entry.</summary>
/// <param name="WindowTitle">The new window text.</param>
/// <param name="Project">The new project text; empty clears it.</param>
/// <param name="Duration">The new duration; when equal to the current one the end time is not touched.</param>
public sealed record WorklogEntryEdit(string? WindowTitle, string? Project, TimeSpan Duration);

/// <summary>Reports what happened to an add, edit, or delete.</summary>
/// <param name="Kind">The outcome class; <see cref="WorklogOutcomeKind.Success"/> when it worked.</param>
/// <param name="Message">A message for the user when it did not work.</param>
/// <param name="Entry">The stored entry after an add.</param>
/// <param name="Overlaps">Other persisted entries of the same day that overlap the saved one.</param>
public sealed record WorklogEditResult(
    WorklogOutcomeKind Kind,
    string? Message = null,
    TimeEntry? Entry = null,
    IReadOnlyList<TimeEntry>? Overlaps = null)
{
    /// <summary>Gets a value indicating whether the change was stored.</summary>
    public bool IsSuccess => this.Kind == WorklogOutcomeKind.Success;

    /// <summary>Gets a value indicating whether the day should be reloaded because it changed elsewhere.</summary>
    public bool NeedsReload => this.Kind is WorklogOutcomeKind.Conflict or WorklogOutcomeKind.NotFound;

    /// <summary>Gets the overlapping entries, never null.</summary>
    public IReadOnlyList<TimeEntry> OverlappingEntries => this.Overlaps ?? [];
}

/// <summary>Published after the user added, edited, or deleted an entry.</summary>
public sealed class WorklogChangedEvent
{
    /// <summary>Gets or sets the local day that changed.</summary>
    public DateOnly Day { get; set; }

    /// <summary>Gets or sets a value indicating whether that day is the current local day.</summary>
    public bool IsToday { get; set; }
}
