namespace FocusTimer.Core.Models;

/// <summary>One group of a summary.</summary>
/// <param name="Key">The stable grouping key.</param>
/// <param name="Label">The text to show for the group.</param>
/// <param name="Duration">The accumulated, range-clipped duration.</param>
/// <param name="Share">The duration divided by the summary total, from 0 to 1.</param>
/// <param name="EntryCount">The number of entries that contributed.</param>
/// <param name="IsUnassigned">Whether this is the bucket for entries without a project.</param>
public sealed record SummaryRow(string Key, string Label, TimeSpan Duration, double Share, int EntryCount, bool IsUnassigned);
