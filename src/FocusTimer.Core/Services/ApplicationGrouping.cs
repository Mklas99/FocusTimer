namespace FocusTimer.Core.Services;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

/// <summary>Groups entries by application name, ignoring letter case.</summary>
public sealed class ApplicationGrouping : IWorklogGrouping
{
    /// <summary>The id of this grouping.</summary>
    public const string GroupingId = "app";

    /// <inheritdoc/>
    public string Id => GroupingId;

    /// <inheritdoc/>
    public string DisplayName => "By application";

    /// <inheritdoc/>
    public GroupKey Select(TimeEntry entry, GroupingContext context)
    {
        var name = entry.AppName?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            name = "Unknown";
        }

        return new GroupKey("app:" + name.ToUpperInvariant(), name);
    }
}
