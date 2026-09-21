#pragma warning disable

namespace FocusTimer.App.Services;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

/// <summary>Queues completed worklog entries until the store confirms a successful append.</summary>
internal sealed class WorklogPersistenceCoordinator : IDisposable
{
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(1),
    ];

    private readonly IWorklogStore _worklogStore;
    private readonly IAppLogger _logger;
    private readonly INotificationService _notificationService;
    private readonly Func<bool> _isWorkLoggingEnabled;
    private readonly Action<IReadOnlyList<TimeEntry>> _onPersisted;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly CancellationTokenSource _retryCancellation = new();
    private readonly List<TimeEntry> _pending = new();
    private bool _failureNotified;
    private bool _retryScheduled;
    private int _retryAttempt;
    private bool _disposed;

    /// <summary>Initializes a coordinator for one widget lifetime.</summary>
    public WorklogPersistenceCoordinator(
        IWorklogStore worklogStore,
        IAppLogger logger,
        INotificationService notificationService,
        Func<bool> isWorkLoggingEnabled,
        Action<IReadOnlyList<TimeEntry>> onPersisted)
    {
        _worklogStore = worklogStore;
        _logger = logger;
        _notificationService = notificationService;
        _isWorkLoggingEnabled = isWorkLoggingEnabled;
        _onPersisted = onPersisted;
    }

    /// <summary>Adds entries and attempts to persist the complete pending batch.</summary>
    public Task FlushAsync(IReadOnlyList<TimeEntry> entries, string reason) => FlushCoreAsync(entries, reason);

    /// <summary>Discards in-memory entries when the user explicitly disables work logging.</summary>
    public async Task DiscardAsync()
    {
        await _gate.WaitAsync();
        try
        {
            _pending.Clear();
            _retryAttempt = 0;
            _logger.LogInformation("Discarded unsaved worklog entries because work logging was disabled.");
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Attempts one final flush within the supplied shutdown budget.</summary>
    public void FlushBeforeShutdown(TimeSpan timeout)
    {
        try
        {
            if (!FlushCoreAsync([], "application shutdown").Wait(timeout))
                _logger.LogWarning("Timed out while flushing queued worklog entries on shutdown.");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to flush queued worklog entries on shutdown.", ex);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _retryCancellation.Cancel();
        _retryCancellation.Dispose();
    }

    private async Task FlushCoreAsync(IReadOnlyList<TimeEntry> entries, string reason)
    {
        if (_disposed)
            return;

        WorklogOutcome? failure = null;
        IReadOnlyList<TimeEntry>? persisted = null;
        await _gate.WaitAsync();
        try
        {
            _pending.AddRange(entries);
            if (!_isWorkLoggingEnabled())
            {
                _pending.Clear();
                _retryAttempt = 0;
                return;
            }

            if (_pending.Count == 0)
                return;

            var batch = _pending.ToArray();
            _logger.LogInformation($"Persisting {batch.Length} queued worklog entries due to {reason}.");
            var outcome = await _worklogStore.AppendAsync(batch);
            if (outcome.IsSuccess)
            {
                _pending.Clear();
                _retryAttempt = 0;
                persisted = batch;
                if (_failureNotified)
                    _logger.LogInformation("Queued worklog entries were persisted after a previous failure.");
                _failureNotified = false;
            }
            else
            {
                failure = outcome;
            }
        }
        finally
        {
            _gate.Release();
        }

        if (persisted is not null)
            _onPersisted(persisted);

        if (failure is not null)
            await ReportFailureAndScheduleRetryAsync(failure);
    }

    private async Task ReportFailureAndScheduleRetryAsync(WorklogOutcome failure)
    {
        _logger.LogWarning($"Unable to persist queued worklog entries: {failure.Kind} {failure.Message}");
        if (!_failureNotified)
        {
            _failureNotified = true;
            try
            {
                await _notificationService.ShowNotificationAsync(
                    "Focus Timer",
                    "Worklog entries could not be saved. FocusTimer will retry while it remains open.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to show the worklog persistence notification.", ex);
            }
        }

        if (_retryScheduled || _disposed)
            return;

        _retryScheduled = true;
        _ = RetryAsync();
    }

    private async Task RetryAsync()
    {
        try
        {
            var delay = RetryDelays[Math.Min(_retryAttempt++, RetryDelays.Length - 1)];
            await Task.Delay(delay, _retryCancellation.Token);
            _retryScheduled = false;
            await FlushCoreAsync([], "automatic retry");
        }
        catch (OperationCanceledException)
        {
            // Disposal intentionally stops in-memory retries.
        }
    }
}
