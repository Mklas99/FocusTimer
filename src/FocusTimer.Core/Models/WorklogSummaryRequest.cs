namespace FocusTimer.Core.Models;

/// <summary>Describes what to summarize.</summary>
/// <param name="Range">The time range to cover.</param>
/// <param name="GroupingId">The id of a registered grouping.</param>
/// <param name="Filter">Optional restrictions; null means none.</param>
public sealed record WorklogSummaryRequest(SummaryRange Range, string GroupingId, SummaryFilter? Filter = null);
