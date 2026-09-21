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
    private readonly object _stateLock = new();
    private readonly SemaphoreSlim _tickGate = new(1, 1);
    private readonly List<TimeEntry> _completedEntries = new();
    private ActiveWindowInfo? _currentWindow;
    private OpenSegment? _current;
    private string? _projectTag;
    private string? _sessionId;
    private long _sessionGeneration;
    private bool _tracking;
    private bool _trackingEnabled = true;

    /// <summary>Initializes a tracker using system time and unknown device identity.</summary>
    public SessionTracker(IActiveWindowService activeWindowService, IAppLogger logger)
        : this(activeWindowService, logger, TimeProvider.System, new SourcePlatformProvider(), () => null) { }

    /// <summary>Initializes a tracker with deterministic time and metadata providers.</summary>
    public SessionTracker(IActiveWindowService activeWindowService, IAppLogger logger, TimeProvider clock,
        ISourcePlatformProvider platformProvider, Func<string?> deviceIdProvider)
    {
        _activeWindowService = activeWindowService ?? throw new ArgumentNullException(nameof(activeWindowService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _timeZone = _clock.LocalTimeZone;
        _platformProvider = platformProvider ?? throw new ArgumentNullException(nameof(platformProvider));
        _deviceIdProvider = deviceIdProvider ?? throw new ArgumentNullException(nameof(deviceIdProvider));
    }

    /// <summary>Gets whether completed segments await persistence.</summary>
    public bool HasCompletedEntries
    {
        get { lock (_stateLock) return _completedEntries.Count > 0; }
    }

    /// <summary>Gets buffered plus active segment count.</summary>
    public int CompletedEntryCount
    {
        get { lock (_stateLock) return _completedEntries.Count + (_current is null ? 0 : 1); }
    }

    /// <summary>Enables or disables capture, discarding non-persisted segments when disabled.</summary>
    public void SetTrackingEnabled(bool enabled)
    {
        lock (_stateLock)
        {
            _trackingEnabled = enabled;
            if (enabled)
                return;

            InvalidateSession();
            _current = null;
            _currentWindow = null;
            _completedEntries.Clear();
        }
    }

    /// <summary>Begins an uninterrupted running session.</summary>
    public async Task StartAsync(string? projectTag)
    {
        long generation;
        lock (_stateLock)
        {
            if (!_trackingEnabled || _tracking)
            {
                _projectTag = projectTag;
                return;
            }

            _projectTag = projectTag;
            _tracking = true;
            _sessionId = Guid.NewGuid().ToString("D");
            generation = ++_sessionGeneration;
        }

        try
        {
            var window = await _activeWindowService.GetForegroundWindowAsync();
            lock (_stateLock)
            {
                if (IsCurrentSession(generation))
                    CreateNewEntry(window, _clock.GetLocalNow());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to start session tracking.", ex);
            lock (_stateLock)
            {
                if (IsCurrentSession(generation))
                    CreateNewEntry(null, _clock.GetLocalNow());
            }
        }
    }

    /// <summary>Polls for window or local-day boundary changes.</summary>
    public async Task OnTimerTickAsync()
    {
        await _tickGate.WaitAsync();
        try
        {
            long generation;
            lock (_stateLock)
            {
                if (!_trackingEnabled || !_tracking || _current is null)
                    return;

                generation = _sessionGeneration;
            }

            ActiveWindowInfo? window;
            try
            {
                window = await _activeWindowService.GetForegroundWindowAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Window tracking tick failed: {ex.Message}");
                return;
            }

            lock (_stateLock)
            {
                if (!IsCurrentSession(generation) || _current is null)
                    return;

                var now = _clock.GetLocalNow();
                SplitAtMidnight(now);
                if (HasWindowChanged(_currentWindow, window))
                {
                    CloseCurrentEntry(now, EndReason.ApplicationChange);
                    CreateNewEntry(window, now);
                }
            }
        }
        finally
        {
            _tickGate.Release();
        }
    }

    /// <summary>Changes project attribution for the active and future segment.</summary>
    public void UpdateProjectTag(string? projectTag)
    {
        lock (_stateLock)
        {
            _projectTag = projectTag;
            if (_current is not null)
                _current.ProjectTag = projectTag;
        }
    }

    /// <summary>Stops tracking and returns closed segments.</summary>
    public IReadOnlyList<TimeEntry> CollectAndResetSegments(EndReason reason = EndReason.ManualPause)
    {
        StopTracking(reason);
        return DrainCompletedSegments();
    }

    /// <summary>Closes the active segment while leaving completed segments available for a later flush.</summary>
    public void StopTracking(EndReason reason)
    {
        lock (_stateLock)
        {
            CloseCurrentEntry(_clock.GetLocalNow(), reason);
            InvalidateSession();
            _currentWindow = null;
        }
    }

    /// <summary>Returns completed segments while tracking continues.</summary>
    public IReadOnlyList<TimeEntry> DrainCompletedSegments()
    {
        lock (_stateLock)
        {
            var entries = _completedEntries.ToList();
            _completedEntries.Clear();
            return entries;
        }
    }

    private bool IsCurrentSession(long generation) =>
        _trackingEnabled && _tracking && _sessionId is not null && _sessionGeneration == generation;

    private void InvalidateSession()
    {
        _tracking = false;
        _sessionId = null;
        _sessionGeneration++;
    }

    private static bool HasWindowChanged(ActiveWindowInfo? previous, ActiveWindowInfo? current) =>
        previous is null || current is null
            ? previous != current
            : previous.ProcessName != current.ProcessName || previous.WindowTitle != current.WindowTitle;

    private void SplitAtMidnight(DateTimeOffset now)
    {
        while (_current is not null && _current.StartedAt.Date < now.Date)
        {
            var boundaryLocal = DateTime.SpecifyKind(_current.StartedAt.Date.AddDays(1), DateTimeKind.Unspecified);
            var boundary = new DateTimeOffset(boundaryLocal, _timeZone.GetUtcOffset(boundaryLocal));
            CloseCurrentEntry(boundary, EndReason.DayBoundary);
            CreateNewEntry(_currentWindow, boundary);
        }
    }

    private void CloseCurrentEntry(DateTimeOffset endedAt, EndReason reason)
    {
        if (_current is not { } current || _sessionId is not { } sessionId)
            return;

        _current = null;
        if (endedAt <= current.StartedAt)
            return;

        _completedEntries.Add(new TimeEntry(
            current.EntryId,
            sessionId,
            current.StartedAt,
            endedAt,
            current.AppName,
            current.WindowTitle,
            current.ProjectTag,
            string.IsNullOrWhiteSpace(current.ProjectTag) ? ProjectAssignmentSource.Unassigned : ProjectAssignmentSource.Session,
            null,
            ActivityKind.Active,
            reason,
            CaptureSource.ActiveWindow,
            _platformProvider.GetCurrentPlatform(),
            _deviceIdProvider(),
            1,
            _clock.GetUtcNow()));
    }

    private void CreateNewEntry(ActiveWindowInfo? window, DateTimeOffset startedAt)
    {
        _currentWindow = window;
        _current = new OpenSegment(
            Guid.NewGuid().ToString("D"),
            startedAt,
            window?.ProcessName ?? "Unknown",
            window?.WindowTitle ?? "No active window",
            _projectTag);
    }

    private sealed class OpenSegment
    {
        public OpenSegment(string entryId, DateTimeOffset startedAt, string appName, string windowTitle, string? projectTag)
        {
            EntryId = entryId;
            StartedAt = startedAt;
            AppName = appName;
            WindowTitle = windowTitle;
            ProjectTag = projectTag;
        }

        public string EntryId { get; }
        public DateTimeOffset StartedAt { get; }
        public string AppName { get; }
        public string WindowTitle { get; }
        public string? ProjectTag { get; set; }
    }
}
