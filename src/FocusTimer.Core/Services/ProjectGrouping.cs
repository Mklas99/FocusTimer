namespace FocusTimer.Core.Services;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

/// <summary>Groups entries by resolved project; entries without a project share one Unassigned row.</summary>
public sealed class ProjectGrouping : IWorklogGrouping
{
    /// <summary>The id of this grouping.</summary>
    public const string GroupingId = "project";

    /// <inheritdoc/>
    public string Id => GroupingId;

    /// <inheritdoc/>
    public string DisplayName => "By project";

    /// <inheritdoc/>
    public GroupKey Select(TimeEntry entry, GroupingContext context)
    {
        var project = context.Project?.Trim();

        // The two prefixes keep a project literally named "Unassigned" apart from the bucket.
        return string.IsNullOrEmpty(project)
            ? new GroupKey("none:", "Unassigned", true)
            : new GroupKey("project:" + project.ToUpperInvariant(), project);
    }
}
