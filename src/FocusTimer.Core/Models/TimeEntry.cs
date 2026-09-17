#pragma warning disable

namespace FocusTimer.Core.Models;

/// <summary>Represents one closed, durable worklog entry.</summary>
public sealed record TimeEntry(
    string EntryId,
    string SessionId,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    string AppName,
    string WindowTitle,
    string? ProjectTag,
    ProjectAssignmentSource ProjectAssignmentSource,
    string? ProjectRuleId,
    ActivityKind ActivityKind,
    EndReason EndReason,
    CaptureSource CaptureSource,
    SourcePlatform SourcePlatform,
    string? SourceDeviceId,
    int Revision,
    DateTimeOffset LastModifiedAtUtc)
{
    /// <summary>Gets the derived, non-editable duration.</summary>
    public TimeSpan Duration => this.EndedAt - this.StartedAt;
}
