#pragma warning disable

namespace FocusTimer.Core.Services;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

/// <summary>Tracks active windows and produces immutable, closed worklog segments.</summary>
public sealed class SessionTracker
{
    private readonly IActiveWindowService _activeWindowService;
    private readonly IAppLogger _logger;
    private readonly TimeProvider _clock;
    private readonly TimeZoneInfo _timeZone;
    private readonly ISourcePlatformProvider _platformProvider;
    private readonly Func<string?> _deviceIdProvider;
    private readonly List<TimeEntry> _completedEntries = new();
    private ActiveWindowInfo? _currentWindow;
    private OpenSegment? _current;
    private string? _projectTag;
    private string? _sessionId;
    private bool _tracking;
    private bool _trackingEnabled = true;

    /// <summary>Initializes a tracker using system time and unknown device identity.</summary>
    public SessionTracker(IActiveWindowService activeWindowService, IAppLogger logger)
        : this(activeWindowService, logger, TimeProvider.System, new SourcePlatformProvider(), () => null) { }

    /// <summary>Initializes a tracker with deterministic time and metadata providers.</summary>
    public SessionTracker(IActiveWindowService activeWindowService, IAppLogger logger, TimeProvider clock,
        ISourcePlatformProvider platformProvider, Func<string?> deviceIdProvider)
    {
        this._activeWindowService = activeWindowService ?? throw new ArgumentNullException(nameof(activeWindowService));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._clock = clock ?? throw new ArgumentNullException(nameof(clock));
        this._timeZone = this._clock.LocalTimeZone;
        this._platformProvider = platformProvider ?? throw new ArgumentNullException(nameof(platformProvider));
        this._deviceIdProvider = deviceIdProvider ?? throw new ArgumentNullException(nameof(deviceIdProvider));
    }

    /// <summary>Gets whether completed segments await persistence.</summary>
    public bool HasCompletedEntries => this._completedEntries.Count > 0;
    /// <summary>Gets buffered plus active segment count.</summary>
    public int CompletedEntryCount => this._completedEntries.Count + (this._current is null ? 0 : 1);

    /// <summary>Enables or disables capture, discarding non-persisted segments when disabled.</summary>
    public void SetTrackingEnabled(bool enabled)
    {
        this._trackingEnabled = enabled;
        if (!enabled)
        { this._tracking = false; this._current = null; this._currentWindow = null; this._completedEntries.Clear(); }
    }

    /// <summary>Begins an uninterrupted running session.</summary>
    public async Task StartAsync(string? projectTag)
    {
        if (!this._trackingEnabled || this._tracking)
        { this._projectTag = projectTag; return; }
        this._projectTag = projectTag;
        this._tracking = true;
        this._sessionId = Guid.NewGuid().ToString("D");
        var sessionId = this._sessionId;
        try
        {
            var window = await this._activeWindowService.GetForegroundWindowAsync();
            if (this._tracking && this._sessionId == sessionId)
            {
                this.CreateNewEntry(window, this._clock.GetLocalNow());
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError("Failed to start session tracking.", ex);
            if (this._tracking && this._sessionId == sessionId)
            {
                this.CreateNewEntry(null, this._clock.GetLocalNow());
            }
        }
    }

    /// <summary>Polls for window or local-day boundary changes.</summary>
    public async Task OnTimerTickAsync()
    {
        if (!this._trackingEnabled || !this._tracking || this._current is null)
            return;
        try
        {
            var now = this._clock.GetLocalNow();
            this.SplitAtMidnight(now);
            var window = await this._activeWindowService.GetForegroundWindowAsync();
            if (HasWindowChanged(this._currentWindow, window))
            { this.CloseCurrentEntry(now, EndReason.ApplicationChange); this.CreateNewEntry(window, now); }
        }
        catch (Exception ex) { this._logger.LogWarning($"Window tracking tick failed: {ex.Message}"); }
    }

    /// <summary>Changes project attribution for the active and future segment.</summary>
    public void UpdateProjectTag(string? projectTag) { this._projectTag = projectTag; if (this._current is not null) this._current.ProjectTag = projectTag; }

    /// <summary>Stops tracking and returns closed segments.</summary>
    public IReadOnlyList<TimeEntry> CollectAndResetSegments(EndReason reason = EndReason.ManualPause)
    {
        this.StopTracking(reason);
        var entries = this.DrainCompletedSegments();
        return entries;
    }

    /// <summary>Closes the active segment while leaving completed segments available for a later flush.</summary>
    /// <param name="reason">The reason the active segment ended.</param>
    public void StopTracking(EndReason reason)
    {
        this._tracking = false;
        this.CloseCurrentEntry(this._clock.GetLocalNow(), reason);
        this._currentWindow = null;
        this._sessionId = null;
    }

    /// <summary>Returns completed segments while tracking continues.</summary>
    public IReadOnlyList<TimeEntry> DrainCompletedSegments() { var entries = this._completedEntries.ToList(); this._completedEntries.Clear(); return entries; }

    private static bool HasWindowChanged(ActiveWindowInfo? previous, ActiveWindowInfo? current) => previous is null || current is null ? previous != current : previous.ProcessName != current.ProcessName || previous.WindowTitle != current.WindowTitle;
    private void SplitAtMidnight(DateTimeOffset now)
    {
        while (this._current is not null && this._current.StartedAt.Date < now.Date)
        {
            var boundaryLocal = DateTime.SpecifyKind(this._current.StartedAt.Date.AddDays(1), DateTimeKind.Unspecified);
            var boundary = new DateTimeOffset(boundaryLocal, this._timeZone.GetUtcOffset(boundaryLocal));
            this.CloseCurrentEntry(boundary, EndReason.DayBoundary);
            this.CreateNewEntry(this._currentWindow, boundary);
        }
    }
    private void CloseCurrentEntry(DateTimeOffset endedAt, EndReason reason)
    {
        if (this._current is not { } current)
            return;
        this._current = null;
        if (endedAt <= current.StartedAt)
            return;
        this._completedEntries.Add(new TimeEntry(current.EntryId, this._sessionId!, current.StartedAt, endedAt,
            current.AppName, current.WindowTitle, current.ProjectTag, string.IsNullOrWhiteSpace(current.ProjectTag) ? ProjectAssignmentSource.Unassigned : ProjectAssignmentSource.Session,
            null, ActivityKind.Active, reason, CaptureSource.ActiveWindow, this._platformProvider.GetCurrentPlatform(), this._deviceIdProvider(), 1, this._clock.GetUtcNow()));
    }
    private void CreateNewEntry(ActiveWindowInfo? window, DateTimeOffset startedAt)
    { this._currentWindow = window; this._current = new OpenSegment(Guid.NewGuid().ToString("D"), startedAt, window?.ProcessName ?? "Unknown", window?.WindowTitle ?? "No active window", this._projectTag); }
    private sealed class OpenSegment { public OpenSegment(string entryId, DateTimeOffset startedAt, string appName, string windowTitle, string? projectTag) { this.EntryId = entryId; this.StartedAt = startedAt; this.AppName = appName; this.WindowTitle = windowTitle; this.ProjectTag = projectTag; } public string EntryId { get; } public DateTimeOffset StartedAt { get; } public string AppName { get; } public string WindowTitle { get; } public string? ProjectTag { get; set; } }
}
