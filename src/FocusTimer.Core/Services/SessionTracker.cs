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
    private bool _captureRunning;
    private PendingStart? _pendingStart;
    private int _pollingIntervalSeconds = 10;
    private long _scheduleRevision;
    private long _lastCaptureTimestamp;
    private readonly List<TimeEntry> _completedEntries = new();
    private ActiveWindowInfo? _currentWindow;
    private OpenSegment? _current;
    private string? _projectTag;
    private string? _sessionId;
    private long _sessionGeneration;
    private bool _tracking;
    private bool _trackingEnabled = true;
    private WindowMatchRule[] _exclusionRules = Array.Empty<WindowMatchRule>();
    private bool _excluded;
    private bool _captureDue;

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
    public Task StartAsync(string? projectTag)
    {
        PendingStart request;
        lock (_stateLock)
        {
            if (!_trackingEnabled || _tracking)
            {
                _projectTag = projectTag;
                return Task.CompletedTask;
            }

            _projectTag = projectTag;
            _tracking = true;
            _sessionId = Guid.NewGuid().ToString("D");
            var generation = ++_sessionGeneration;
            request = new PendingStart(generation, _scheduleRevision);
            if (_captureRunning)
            {
                _pendingStart?.Completion.TrySetResult();
                _pendingStart = request;
                return request.Completion.Task;
            }

            _captureRunning = true;
        }

        _ = CaptureAsync(request.Generation, request.Revision, request);
        return request.Completion.Task;
    }

    /// <summary>Sets the ordered exclusion rules and requests a foreground sample at the next maintenance tick.</summary>
    public void SetExclusionRules(IEnumerable<WindowMatchRule>? rules)
    {
        var valid = (rules ?? Enumerable.Empty<WindowMatchRule>()).Where(r => r is { IsValid: true }).ToArray();
        lock (_stateLock)
        {
            if (_exclusionRules.SequenceEqual(valid)) return;
            _exclusionRules = valid;
            _captureDue = true;
        }
    }

    /// <summary>Changes foreground sampling cadence without changing session boundaries.</summary>
    public void SetPollingInterval(int seconds)
    {
        seconds = seconds is >= 1 and <= 60 ? seconds : 10;
        lock (_stateLock)
        {
            if (_pollingIntervalSeconds == seconds) return;
            _pollingIntervalSeconds = seconds;
            _scheduleRevision++;
            _lastCaptureTimestamp = _clock.GetTimestamp();
        }
    }

    /// <summary>Maintains day boundaries each tick and samples only when due.</summary>
    public Task OnTimerTickAsync()
    {
        long generation;
        long revision;
        lock (_stateLock)
        {
            if (!_trackingEnabled || !_tracking || (_current is null && !_excluded)) return Task.CompletedTask;
            SplitAtMidnight(_clock.GetLocalNow());
            if (_captureRunning || (!_captureDue && _clock.GetElapsedTime(_lastCaptureTimestamp) < TimeSpan.FromSeconds(_pollingIntervalSeconds)))
                return Task.CompletedTask;
            _captureRunning = true;
            _captureDue = false;
            generation = _sessionGeneration;
            revision = _scheduleRevision;
        }

        return CaptureAsync(generation, revision, null);
    }

    private async Task CaptureAsync(long generation, long revision, PendingStart? initial)
    {
        try
        {
            while (true)
            {
                ActiveWindowInfo? window = null;
                var succeeded = true;
                try { window = await _activeWindowService.GetForegroundWindowAsync().ConfigureAwait(false); }
                catch (Exception ex)
                {
                    succeeded = false;
                    if (initial is not null) _logger.LogError("Failed to start session tracking.", ex);
                    else _logger.LogWarning($"Window tracking tick failed: {ex.Message}");
                }

                lock (_stateLock)
                {
                    if (IsCurrentSession(generation))
                    {
                        var now = _clock.GetLocalNow();
                        SplitAtMidnight(now);
                        var excluded = initial is not null
                            ? succeeded && WindowRuleMatcher.FindFirst(_exclusionRules, window) is not null
                            : succeeded ? WindowRuleMatcher.FindFirst(_exclusionRules, window) is not null : _excluded;
                        if (excluded)
                        {
                            CloseCurrentEntry(now, EndReason.ApplicationChange);
                            _currentWindow = null;
                            _excluded = true;
                        }
                        else if (initial is not null || (succeeded && _excluded))
                        {
                            _excluded = false;
                            CreateNewEntry(window, now);
                        }
                        else if (succeeded && _current is not null && HasWindowChanged(_currentWindow, window))
                        {
                            CloseCurrentEntry(now, EndReason.ApplicationChange);
                            CreateNewEntry(window, now);
                        }

                        if (revision == _scheduleRevision) _lastCaptureTimestamp = _clock.GetTimestamp();
                    }

                    initial?.Completion.TrySetResult();
                    initial = _pendingStart;
                    _pendingStart = null;
                    if (initial is null)
                    {
                        _captureRunning = false;
                        return;
                    }

                    generation = initial.Generation;
                    revision = initial.Revision;
                }
            }
        }
        catch (Exception ex)
        {
            lock (_stateLock)
            {
                _captureRunning = false;
                initial?.Completion.TrySetException(ex);
                _pendingStart?.Completion.TrySetException(ex);
                _pendingStart = null;
            }

            _logger.LogError("Tracking update failed.", ex);
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
            var now = _clock.GetLocalNow();
            SplitAtMidnight(now);
            CloseCurrentEntry(now, reason);
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
        _excluded = false;
        _sessionId = null;
        _sessionGeneration++;
        _pendingStart?.Completion.TrySetResult();
        _pendingStart = null;
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
            // Close one second early so no persisted entry ends on the next day's date; the second is not recorded.
            CloseCurrentEntry(TimeZoneInfo.ConvertTime(boundary.AddSeconds(-1), _timeZone), EndReason.DayBoundary);
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

    private sealed class PendingStart(long generation, long revision)
    {
        public long Generation { get; } = generation;
        public long Revision { get; } = revision;
        public TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
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
