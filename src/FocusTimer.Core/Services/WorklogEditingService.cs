namespace FocusTimer.Core.Services;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

/// <summary>Adds, edits, and deletes worklog entries on top of the storage-neutral store contract.</summary>
public sealed class WorklogEditingService : IWorklogEditingService
{
    /// <summary>The fixed application label of every manual entry.</summary>
    public const string ManualApplicationName = "Manual entry";

    /// <summary>The longest project text that is accepted.</summary>
    public const int MaxProjectLength = 100;

    private readonly IWorklogStore _store;
    private readonly ISettingsProvider _settingsProvider;
    private readonly InstallationIdentity _identity;
    private readonly ISourcePlatformProvider _platformProvider;
    private readonly TimeProvider _timeProvider;
    private readonly IEventBus? _eventBus;
    private readonly IAppLogger _logger;

    /// <summary>Initializes a new instance of the <see cref="WorklogEditingService"/> class.</summary>
    /// <param name="store">The worklog to change.</param>
    /// <param name="settingsProvider">Supplies the retention setting.</param>
    /// <param name="identity">Supplies the device id stored on new entries.</param>
    /// <param name="platformProvider">Supplies the platform stored on new entries.</param>
    /// <param name="timeProvider">The clock and time zone.</param>
    /// <param name="eventBus">Receives <see cref="WorklogChangedEvent"/>; optional.</param>
    /// <param name="logger">The application logger.</param>
    public WorklogEditingService(
        IWorklogStore store,
        ISettingsProvider settingsProvider,
        InstallationIdentity identity,
        ISourcePlatformProvider platformProvider,
        TimeProvider timeProvider,
        IEventBus? eventBus,
        IAppLogger logger)
    {
        this._store = store;
        this._settingsProvider = settingsProvider;
        this._identity = identity;
        this._platformProvider = platformProvider;
        this._timeProvider = timeProvider;
        this._eventBus = eventBus;
        this._logger = logger;
    }

    /// <summary>Finds the persisted entries that strictly overlap a subject; entries that only touch do not overlap.</summary>
    /// <param name="dayEntries">The persisted entries of the subject's day.</param>
    /// <param name="subject">The entry that was saved.</param>
    /// <returns>The overlapping entries in start order, never including the subject itself.</returns>
    public static IReadOnlyList<TimeEntry> FindOverlaps(IEnumerable<TimeEntry> dayEntries, TimeEntry subject) =>
        dayEntries
            .Where(e => e.EntryId != subject.EntryId && e.StartedAt < subject.EndedAt && subject.StartedAt < e.EndedAt)
            .OrderBy(e => e.StartedAt)
            .ToList();

    /// <inheritdoc/>
    public async Task<WorklogEditResult> AddManualAsync(ManualEntryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var timeZone = this._timeProvider.LocalTimeZone;
        var now = this._timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(this._timeProvider.GetLocalNow().DateTime);

        var projectError = ValidateProject(request.Project);
        if (projectError is not null)
        {
            return Rejected(projectError);
        }

        if (request.Duration < TimeSpan.FromMinutes(1))
        {
            return Rejected("The duration must be at least one minute. " + DurationParser.ExampleText);
        }

        if (request.Day > today)
        {
            return Rejected("Time cannot be added for a day that has not started yet.");
        }

        var retention = await this.LoadRetentionDaysAsync().ConfigureAwait(false);
        if (!WorklogDayBounds.IsAllowed(request.Day, today, retention))
        {
            return Rejected("That day is outside the retention window and would be removed by cleanup. "
                + $"The earliest day you can add time to is {WorklogDayBounds.EarliestDay(today, retention):yyyy-MM-dd}.");
        }

        var local = DateTime.SpecifyKind(request.Day.ToDateTime(request.Start), DateTimeKind.Unspecified);
        if (timeZone.IsInvalidTime(local))
        {
            return Rejected($"{request.Start:HH:mm} does not exist on {request.Day:yyyy-MM-dd} because the clocks change that day. Pick another start time.");
        }

        var offset = timeZone.IsAmbiguousTime(local)
            ? timeZone.GetAmbiguousTimeOffsets(local).Max()
            : timeZone.GetUtcOffset(local);
        var startedAt = new DateTimeOffset(local, offset);
        var endedAt = TimeZoneInfo.ConvertTime(startedAt + request.Duration, timeZone);
        if (DateOnly.FromDateTime(endedAt.DateTime) != request.Day)
        {
            return Rejected("The entry would run past 23:59 of that day. Shorten the duration or start earlier.");
        }

        if (endedAt > now)
        {
            return Rejected("The entry would end in the future. Shorten the duration or choose an earlier start.");
        }

        var entry = new TimeEntry(
            Guid.NewGuid().ToString("D"),
            Guid.NewGuid().ToString("D"),
            startedAt,
            endedAt,
            ManualApplicationName,
            (request.WindowTitle ?? string.Empty).Trim(),
            NormalizeProject(request.Project),
            string.IsNullOrWhiteSpace(request.Project) ? ProjectAssignmentSource.Unassigned : ProjectAssignmentSource.Editor,
            null,
            ActivityKind.Active,
            EndReason.Unknown,
            CaptureSource.Manual,
            this._platformProvider.GetCurrentPlatform(),
            this.TryGetDeviceId(),
            1,
            now);

        WorklogOutcome outcome;
        try
        {
            outcome = await this._store.AppendAsync([entry], cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            this._logger.LogError("Adding a manual worklog entry failed.", ex);
            return Rejected("The entry could not be saved.", WorklogOutcomeKind.IoFailure);
        }

        if (!outcome.IsSuccess)
        {
            return Failed(outcome);
        }

        var overlaps = await this.FindStoredOverlapsAsync(entry, cancellationToken).ConfigureAwait(false);
        this.PublishChanged(request.Day, today);
        return new WorklogEditResult(WorklogOutcomeKind.Success, null, entry, overlaps);
    }

    /// <inheritdoc/>
    public async Task<WorklogEditResult> UpdateAsync(TimeEntry entry, WorklogEntryEdit edit, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(edit);
        var timeZone = this._timeProvider.LocalTimeZone;
        var today = DateOnly.FromDateTime(this._timeProvider.GetLocalNow().DateTime);

        var projectError = ValidateProject(edit.Project);
        if (projectError is not null)
        {
            return Rejected(projectError);
        }

        if (edit.Duration < TimeSpan.FromMinutes(1))
        {
            return Rejected("The duration must be at least one minute. " + DurationParser.ExampleText);
        }

        var windowTitle = (edit.WindowTitle ?? string.Empty).Trim();
        var project = NormalizeProject(edit.Project);
        var projectChanged = !string.Equals(project ?? string.Empty, entry.ProjectTag ?? string.Empty, StringComparison.Ordinal);
        var durationChanged = !SameMinute(edit.Duration, entry.Duration);
        if (!projectChanged && !durationChanged && string.Equals(windowTitle, entry.WindowTitle, StringComparison.Ordinal))
        {
            return new WorklogEditResult(WorklogOutcomeKind.Success);
        }

        var endedAt = entry.EndedAt;
        if (durationChanged)
        {
            endedAt = TimeZoneInfo.ConvertTime(entry.StartedAt + edit.Duration, timeZone);
            if (endedAt.Date != entry.StartedAt.Date)
            {
                return Rejected("The entry would run past 23:59 of its day. Use a shorter duration.");
            }
        }

        var assignmentSource = entry.ProjectAssignmentSource;
        if (projectChanged)
        {
            assignmentSource = project is null ? ProjectAssignmentSource.Unassigned : ProjectAssignmentSource.Editor;
        }

        var patch = new WorklogPatch(
            entry.StartedAt,
            endedAt,
            entry.AppName,
            windowTitle,
            projectChanged ? project : entry.ProjectTag,
            assignmentSource,
            projectChanged ? null : entry.ProjectRuleId,
            entry.ActivityKind,
            entry.EndReason,
            entry.CaptureSource);

        WorklogOutcome outcome;
        try
        {
            outcome = await this._store.PatchAsync(entry.EntryId, entry.Revision, patch, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            this._logger.LogError("Editing a worklog entry failed.", ex);
            return Rejected("The change could not be saved.", WorklogOutcomeKind.IoFailure);
        }

        if (!outcome.IsSuccess)
        {
            return Failed(outcome);
        }

        var saved = entry with
        {
            WindowTitle = patch.WindowTitle,
            EndedAt = patch.EndedAt,
            ProjectTag = patch.ProjectTag,
            ProjectAssignmentSource = patch.ProjectAssignmentSource,
            ProjectRuleId = patch.ProjectRuleId,
            Revision = entry.Revision + 1,
        };
        var overlaps = durationChanged
            ? await this.FindStoredOverlapsAsync(saved, cancellationToken).ConfigureAwait(false)
            : [];
        this.PublishChanged(DateOnly.FromDateTime(entry.StartedAt.Date), today);
        return new WorklogEditResult(WorklogOutcomeKind.Success, null, saved, overlaps);
    }

    /// <inheritdoc/>
    public async Task<WorklogEditResult> DeleteAsync(TimeEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var today = DateOnly.FromDateTime(this._timeProvider.GetLocalNow().DateTime);
        WorklogOutcome outcome;
        try
        {
            outcome = await this._store.DeleteAsync(entry.EntryId, entry.Revision, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            this._logger.LogError("Deleting a worklog entry failed.", ex);
            return Rejected("The entry could not be deleted.", WorklogOutcomeKind.IoFailure);
        }

        if (!outcome.IsSuccess)
        {
            return Failed(outcome);
        }

        this.PublishChanged(DateOnly.FromDateTime(entry.StartedAt.Date), today);
        return new WorklogEditResult(WorklogOutcomeKind.Success);
    }

    private static bool SameMinute(TimeSpan left, TimeSpan right) =>
        Math.Abs((left - right).TotalSeconds) < 30;

    private static string? NormalizeProject(string? project)
    {
        var trimmed = project?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static string? ValidateProject(string? project) =>
        (project?.Trim().Length ?? 0) > MaxProjectLength
            ? $"The project name cannot be longer than {MaxProjectLength} characters."
            : null;

    private static WorklogEditResult Rejected(string message, WorklogOutcomeKind kind = WorklogOutcomeKind.ValidationFailure) =>
        new(kind, message);

    private static WorklogEditResult Failed(WorklogOutcome outcome) => new(outcome.Kind, FailureMessage(outcome));

    private static string FailureMessage(WorklogOutcome outcome) => outcome.Kind switch
    {
        WorklogOutcomeKind.Conflict => "This entry was changed elsewhere since it was loaded. The day has been reloaded; try again.",
        WorklogOutcomeKind.NotFound => "This entry no longer exists. The day has been reloaded.",
        WorklogOutcomeKind.FileInUse => "The worklog file is in use by another program. Try again in a moment.",
        WorklogOutcomeKind.UnsupportedSchema => "That day's worklog file uses a format this version cannot change, so it was left untouched.",
        WorklogOutcomeKind.MalformedData => "That day's worklog file could not be read safely, so it was left untouched.",
        WorklogOutcomeKind.IoFailure => "The worklog could not be written. " + outcome.Message,
        _ => outcome.Message ?? "The change was not accepted.",
    };

    private async Task<int> LoadRetentionDaysAsync()
    {
        try
        {
            return (await this._settingsProvider.LoadAsync().ConfigureAwait(false)).DataRetentionDays;
        }
        catch (Exception ex)
        {
            this._logger.LogError("Could not read the retention setting; assuming no limit.", ex);
            return 0;
        }
    }

    private string? TryGetDeviceId()
    {
        try
        {
            return this._identity.DeviceId;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private async Task<IReadOnlyList<TimeEntry>> FindStoredOverlapsAsync(TimeEntry subject, CancellationToken cancellationToken)
    {
        try
        {
            var timeZone = this._timeProvider.LocalTimeZone;
            var day = subject.StartedAt.Date;
            var start = new DateTimeOffset(day, timeZone.GetUtcOffset(day));
            var next = day.AddDays(1);
            var end = new DateTimeOffset(next, timeZone.GetUtcOffset(next));
            var read = await this._store.QueryAsync(new WorklogQuery(start, end), cancellationToken).ConfigureAwait(false);
            return read.Outcome.IsSuccess ? FindOverlaps(read.Entries, subject) : [];
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            this._logger.LogWarning("Could not check for overlapping entries: " + ex.Message);
            return [];
        }
    }

    private void PublishChanged(DateOnly day, DateOnly today) =>
        this._eventBus?.Publish(new WorklogChangedEvent { Day = day, IsToday = day == today });
}
