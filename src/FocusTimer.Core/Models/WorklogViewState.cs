namespace FocusTimer.Core.Models;

/// <summary>
/// Small view preferences of the Worklog window that are remembered between runs. They are kept apart from the
/// settings, so changing a view never touches the Settings draft or its commit flow.
/// </summary>
/// <param name="TimelineHourHeight">The timeline's hour height (its zoom), or null when it was never changed.</param>
/// <param name="TimelineGroupingId">The id of the grouping the timeline is split by, or null for all entries together.</param>
public sealed record WorklogViewState(double? TimelineHourHeight = null, string? TimelineGroupingId = null);
