#pragma warning disable

namespace FocusTimer.Core.Models;

/// <summary>Classifies captured work activity.</summary>
public enum ActivityKind { Active }
/// <summary>Describes why an entry ended.</summary>
public enum EndReason { ApplicationChange, ManualPause, IdlePause, DayBoundary, ApplicationExit, Unknown }
/// <summary>Describes how an entry was captured.</summary>
public enum CaptureSource { ActiveWindow }
/// <summary>Describes the originating platform.</summary>
public enum SourcePlatform { Windows, Linux, MacOS, Unknown }
/// <summary>Describes project attribution provenance.</summary>
public enum ProjectAssignmentSource { Unassigned, Session, Rule, Editor, Imported }

/// <summary>Provides stable persisted spellings for controlled worklog values.</summary>
public static class WorklogValueCodec
{
    /// <summary>Returns the stable serialized representation of a controlled value.</summary>
    public static string ToStoredValue<T>(T value) where T : struct, Enum => value switch
    {
        ActivityKind.Active => "active",
        EndReason.ApplicationChange => "application-change",
        EndReason.ManualPause => "manual-pause",
        EndReason.IdlePause => "idle-pause",
        EndReason.DayBoundary => "day-boundary",
        EndReason.ApplicationExit => "application-exit",
        EndReason.Unknown => "unknown",
        CaptureSource.ActiveWindow => "active-window",
        SourcePlatform.Windows => "windows",
        SourcePlatform.Linux => "linux",
        SourcePlatform.MacOS => "macos",
        SourcePlatform.Unknown => "unknown",
        ProjectAssignmentSource.Unassigned => "unassigned",
        ProjectAssignmentSource.Session => "session",
        ProjectAssignmentSource.Rule => "rule",
        ProjectAssignmentSource.Editor => "editor",
        ProjectAssignmentSource.Imported => "imported",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };
}
