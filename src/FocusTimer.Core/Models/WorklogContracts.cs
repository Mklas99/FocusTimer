#pragma warning disable

namespace FocusTimer.Core.Models;

/// <summary>Validates durable worklog records before storage access.</summary>
public static class WorklogEntryValidator
{
    /// <summary>Returns validation errors, if any.</summary>
    public static IReadOnlyList<string> Validate(TimeEntry entry)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(entry.EntryId))
            errors.Add("Entry ID is required.");
        if (string.IsNullOrWhiteSpace(entry.SessionId))
            errors.Add("Session ID is required.");
        if (entry.EndedAt <= entry.StartedAt)
            errors.Add("End time must be later than start time.");
        if (entry.Revision < 1)
            errors.Add("Revision must be at least one.");
        if (entry.StartedAt.Date != entry.EndedAt.Date &&
            !(entry.EndedAt.TimeOfDay == TimeSpan.Zero && entry.EndedAt.Date == entry.StartedAt.Date.AddDays(1)))
            errors.Add("Entries must not span local calendar days.");
        return errors;
    }
}

/// <summary>Filters an overlap query.</summary>
public sealed record WorklogQuery(DateTimeOffset StartInclusive, DateTimeOffset EndExclusive, string? Application = null,
    string? Project = null, string? SessionId = null, ActivityKind? ActivityKind = null, CaptureSource? CaptureSource = null)
{ public bool IsValid => this.EndExclusive > this.StartInclusive; }
/// <summary>Represents permitted changes to an entry.</summary>
public sealed record WorklogPatch(DateTimeOffset StartedAt, DateTimeOffset EndedAt, string AppName, string WindowTitle,
    string? ProjectTag, ProjectAssignmentSource ProjectAssignmentSource, string? ProjectRuleId, ActivityKind ActivityKind,
    EndReason EndReason, CaptureSource CaptureSource);
/// <summary>Classifies a storage outcome.</summary>
public enum WorklogOutcomeKind { Success, ValidationFailure, NotFound, Conflict, UnsupportedSchema, MalformedData, IoFailure, FileInUse }
/// <summary>Describes a non-fatal read diagnostic.</summary>
public sealed record WorklogWarning(string FilePath, int? RecordNumber, string Message);
/// <summary>Represents a typed operation outcome.</summary>
public sealed record WorklogOutcome(WorklogOutcomeKind Kind, string? Message = null, IReadOnlyList<WorklogWarning>? Warnings = null)
{ public bool IsSuccess => this.Kind == WorklogOutcomeKind.Success; public static WorklogOutcome Success(IReadOnlyList<WorklogWarning>? warnings = null) => new(WorklogOutcomeKind.Success, null, warnings); }
/// <summary>Contains entries and diagnostics from a read operation.</summary>
public sealed record WorklogReadResult(WorklogOutcome Outcome, IReadOnlyList<TimeEntry> Entries);
