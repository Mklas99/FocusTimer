namespace FocusTimer.Core.Services;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

/// <summary>Groups entries by window title; entries without a title share one "No window title" row.</summary>
public sealed class WindowGrouping : IWorklogGrouping
{
    /// <summary>The id of this grouping.</summary>
    public const string GroupingId = "window";

    /// <summary>The label of the row for entries without a window title.</summary>
    public const string NoWindowLabel = "No window title";

    /// <inheritdoc/>
    public string Id => GroupingId;

    /// <inheritdoc/>
    public string DisplayName => "By window";

    /// <inheritdoc/>
    public GroupKey Select(TimeEntry entry, GroupingContext context)
    {
        var title = entry.WindowTitle?.Trim();

        // The two prefixes keep a window literally titled "No window title" apart from the bucket.
        return string.IsNullOrEmpty(title)
            ? new GroupKey("none:", NoWindowLabel, true)
            : new GroupKey("window:" + title.ToUpperInvariant(), title);
    }
}
