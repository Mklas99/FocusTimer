namespace FocusTimer.Core.Models;

/// <summary>Optional restrictions applied to a summary. Omitted members mean "no restriction".</summary>
/// <param name="Application">Only entries of this application (case-insensitive), or null for all.</param>
/// <param name="Project">Only entries of this resolved project (case-insensitive), or null for all.</param>
public sealed record SummaryFilter(string? Application = null, string? Project = null)
{
    /// <summary>Gets a filter that restricts nothing.</summary>
    public static SummaryFilter None { get; } = new();
}
