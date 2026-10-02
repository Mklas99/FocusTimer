namespace FocusTimer.App.Tests;

using FocusTimer.App.Services;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

public class WorklogPersistenceCoordinatorTests
{
    [Fact]
    public async Task FlushAsync_GivenTransientFailure_RetriesSameEntryAndNotifiesOnce()
    {
        var store = new FailOnceWorklogStore();
        var notificationService = new RecordingNotificationService();
        IReadOnlyList<TimeEntry>? persisted = null;
        var callbackCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var coordinator = new WorklogPersistenceCoordinator(
            store,
            NullLogger.Instance,
            notificationService,
            () => true,
            entries => { persisted = entries; callbackCompleted.SetResult(); });
        var entry = CreateEntry();

        await coordinator.FlushAsync([entry], "test");
        await store.WaitForSuccessfulAppendAsync();
        await callbackCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(2, store.AppendCount);
        Assert.Equal(1, notificationService.NotificationCount);
        Assert.Equal(entry, Assert.Single(persisted!));
    }

    [Theory]
    [InlineData(WorklogOutcomeKind.ValidationFailure)]
    [InlineData(WorklogOutcomeKind.Conflict)]
    public async Task PermanentlyRejectedEntry_IsDroppedWithoutRetryOrSuccessEvent(WorklogOutcomeKind kind)
    {
        var store = new ScriptedStore(_ => Task.FromResult(new WorklogOutcome(kind, "invalid entry")));
        var notifications = new RecordingNotificationService();
        var logger = new RecordingLogger();
        var persisted = new List<TimeEntry>();
        using var coordinator = new WorklogPersistenceCoordinator(store, logger, notifications,
            () => true, entries => persisted.AddRange(entries));

        await coordinator.FlushAsync([CreateEntry()], "test");
        await coordinator.FlushAsync([], "explicit retry");

        Assert.Single(store.Batches);
        Assert.Empty(persisted);
        Assert.Equal(0, notifications.NotificationCount);
        Assert.Contains(logger.Warnings, message => message.Contains("entry-id"));
    }

    [Theory]
    [InlineData(WorklogOutcomeKind.ValidationFailure)]
    [InlineData(WorklogOutcomeKind.Conflict)]
    public async Task RejectedBatch_IsolatesBadEntriesAndPublishesOnlyAcceptedEntries(WorklogOutcomeKind kind)
    {
        var good = CreateEntry("good");
        var bad = CreateEntry("bad");
        var store = new ScriptedStore(entries => Task.FromResult(
            entries.Count > 1 || entries.Single().EntryId == "bad"
                ? new WorklogOutcome(kind, "rejected")
                : WorklogOutcome.Success()));
        var notifications = new RecordingNotificationService();
        var persisted = new List<TimeEntry>();
        using var coordinator = new WorklogPersistenceCoordinator(store, NullLogger.Instance, notifications,
            () => true, entries => persisted.AddRange(entries));

        await coordinator.FlushAsync([bad, good], "test");
        await coordinator.FlushAsync([], "explicit retry");

        Assert.Equal(good, Assert.Single(persisted));
        Assert.Equal(3, store.Batches.Count);
        Assert.Equal(0, notifications.NotificationCount);
    }

    [Fact]
    public async Task PartiallyAcceptedBatch_RetriesOnlyTheTransientFailure()
    {
        var good = CreateEntry("good");
        var transient = CreateEntry("transient");
        bool retrySucceeded = false;
        var store = new ScriptedStore(entries => Task.FromResult(entries.Count > 1
            ? new WorklogOutcome(WorklogOutcomeKind.ValidationFailure, "isolate entries")
            : entries.Single().EntryId == "transient" && !retrySucceeded
                ? new WorklogOutcome(WorklogOutcomeKind.FileInUse, "locked")
                : WorklogOutcome.Success()));
        var persisted = new List<TimeEntry>();
        var notifications = new RecordingNotificationService();
        using var coordinator = new WorklogPersistenceCoordinator(store, NullLogger.Instance, notifications,
            () => true, entries => persisted.AddRange(entries));

        await coordinator.FlushAsync([good, transient], "test");
        Assert.Equal(good, Assert.Single(persisted));
        retrySucceeded = true;
        await coordinator.FlushAsync([], "explicit retry");

        Assert.Equal(new[] { good, transient }, persisted);
        Assert.Equal(transient, Assert.Single(store.Batches.Last()));
        Assert.Equal(1, notifications.NotificationCount);
    }

    [Fact]
    public async Task StoreExceptionAndNotificationException_DoNotLoseThePendingEntry()
    {
        bool available = false;
        var store = new ScriptedStore(_ => available
            ? Task.FromResult(WorklogOutcome.Success())
            : Task.FromException<WorklogOutcome>(new IOException("disk unavailable")));
        var logger = new RecordingLogger();
        var notifications = new RecordingNotificationService { Fail = true };
        var persisted = new List<TimeEntry>();
        using var coordinator = new WorklogPersistenceCoordinator(store, logger, notifications,
            () => true, entries => persisted.AddRange(entries));
        var entry = CreateEntry();

        await coordinator.FlushAsync([entry], "test");
        await coordinator.FlushAsync([], "still unavailable");
        Assert.Equal(1, notifications.NotificationCount);
        Assert.Contains(logger.Errors, message => message.Contains("notification"));
        available = true;
        await coordinator.FlushAsync([], "explicit retry");

        Assert.Equal(entry, Assert.Single(persisted));
        Assert.All(store.Batches, batch => Assert.Equal(entry, Assert.Single(batch)));
    }

    [Fact]
    public async Task PersistedCallbackFailure_DoesNotWriteTheSameEntryAgain()
    {
        var store = new ScriptedStore(_ => Task.FromResult(WorklogOutcome.Success()));
        var logger = new RecordingLogger();
        using var coordinator = new WorklogPersistenceCoordinator(store, logger, new RecordingNotificationService(),
            () => true, _ => throw new InvalidOperationException("subscriber failed"));

        await coordinator.FlushAsync([CreateEntry()], "test");
        await coordinator.FlushAsync([], "explicit retry");

        Assert.Single(store.Batches);
        Assert.Contains("Worklog persisted callback failed.", logger.Errors);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DisablingLoggingOrDiscarding_RemovesQueuedEntriesBeforeLoggingResumes(bool disableLogging)
    {
        bool enabled = true;
        bool available = false;
        var store = new ScriptedStore(_ => Task.FromResult(available ? WorklogOutcome.Success()
            : new WorklogOutcome(WorklogOutcomeKind.FileInUse, "locked")));
        var persisted = new List<TimeEntry>();
        using var coordinator = new WorklogPersistenceCoordinator(store, NullLogger.Instance, new RecordingNotificationService(),
            () => enabled, entries => persisted.AddRange(entries));

        await coordinator.FlushAsync([CreateEntry("old")], "test");
        if (disableLogging)
        {
            enabled = false;
            await coordinator.FlushAsync([CreateEntry("while-disabled")], "disabled");
            enabled = true;
        }
        else
        {
            await coordinator.DiscardAsync();
        }

        available = true;
        var fresh = CreateEntry("fresh");
        await coordinator.FlushAsync([fresh], "logging resumed");

        Assert.Equal(fresh, Assert.Single(persisted));
        Assert.Equal(fresh, Assert.Single(store.Batches.Last()));
    }

    [Fact]
    public async Task StoreCancellation_PropagatesAndReleasesTheGateForRetry()
    {
        bool cancel = true;
        var store = new ScriptedStore(_ => cancel
            ? Task.FromCanceled<WorklogOutcome>(new CancellationToken(true))
            : Task.FromResult(WorklogOutcome.Success()));
        var persisted = new List<TimeEntry>();
        using var coordinator = new WorklogPersistenceCoordinator(store, NullLogger.Instance, new RecordingNotificationService(),
            () => true, entries => persisted.AddRange(entries));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => coordinator.FlushAsync([CreateEntry()], "test"));
        cancel = false;
        await coordinator.FlushAsync([], "explicit retry").WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Single(persisted);
    }

    [Fact]
    public async Task DisposedCoordinator_IgnoresNewEntries()
    {
        var store = new ScriptedStore(_ => Task.FromResult(WorklogOutcome.Success()));
        using var coordinator = new WorklogPersistenceCoordinator(store, NullLogger.Instance, new RecordingNotificationService(),
            () => true, _ => throw new InvalidOperationException("No persistence expected."));
        coordinator.Dispose();
        coordinator.Dispose();

        await coordinator.FlushAsync([CreateEntry()], "after shutdown");

        Assert.Empty(store.Batches);
    }

    private static TimeEntry CreateEntry(string id = "entry-id") => new(
        id,
        "session-id",
        new DateTimeOffset(2026, 4, 1, 9, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 4, 1, 9, 5, 0, 0, TimeSpan.Zero),
        "app",
        "title",
        null,
        ProjectAssignmentSource.Unassigned,
        null,
        ActivityKind.Active,
        EndReason.ManualPause,
        CaptureSource.ActiveWindow,
        SourcePlatform.Windows,
        "device",
        1,
        new DateTimeOffset(2026, 4, 1, 9, 5, 0, TimeSpan.Zero));

    private sealed class FailOnceWorklogStore : IWorklogStore
    {
        private readonly TaskCompletionSource _success = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int AppendCount { get; private set; }

        public Task<WorklogOutcome> AppendAsync(IReadOnlyCollection<TimeEntry> entries, CancellationToken cancellationToken = default)
        {
            this.AppendCount++;
            if (this.AppendCount == 1)
                return Task.FromResult(new WorklogOutcome(WorklogOutcomeKind.FileInUse, "locked"));

            this._success.SetResult();
            return Task.FromResult(WorklogOutcome.Success());
        }

        public Task<WorklogOutcome> DeleteAsync(string entryId, int expectedRevision, CancellationToken cancellationToken = default) =>
            Task.FromResult(WorklogOutcome.Success());

        public Task<WorklogReadResult> GetAsync(string entryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorklogReadResult(WorklogOutcome.Success(), []));

        public Task<WorklogOutcome> PatchAsync(string entryId, int expectedRevision, WorklogPatch patch, CancellationToken cancellationToken = default) =>
            Task.FromResult(WorklogOutcome.Success());

        public Task<WorklogReadResult> QueryAsync(WorklogQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorklogReadResult(WorklogOutcome.Success(), []));

        public Task WaitForSuccessfulAppendAsync() => this._success.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private sealed class ScriptedStore(Func<IReadOnlyCollection<TimeEntry>, Task<WorklogOutcome>> append) : IWorklogStore
    {
        public List<TimeEntry[]> Batches { get; } = new();
        public Task<WorklogOutcome> AppendAsync(IReadOnlyCollection<TimeEntry> entries, CancellationToken cancellationToken = default)
        {
            this.Batches.Add(entries.ToArray());
            return append(entries);
        }
        public Task<WorklogReadResult> GetAsync(string entryId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorklogReadResult> QueryAsync(WorklogQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorklogOutcome> PatchAsync(string entryId, int expectedRevision, WorklogPatch patch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorklogOutcome> DeleteAsync(string entryId, int expectedRevision, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class RecordingLogger : IAppLogger
    {
        public List<string> Warnings { get; } = new();
        public List<string> Errors { get; } = new();
        public void LogWarning(string message) => this.Warnings.Add(message);
        public void LogError(string message, Exception? ex = null) => this.Errors.Add(message);
        public void LogCritical(string message, Exception? ex = null) { }
        public void LogDebug(string message) { }
        public void LogInformation(string message) { }
    }

    private sealed class RecordingNotificationService : INotificationService
    {
        public int NotificationCount { get; private set; }
        public bool Fail { get; set; }

        public Task ShowBreakReminderAsync(string message, bool requireAcknowledgement) => Task.CompletedTask;

        public Task ShowNotificationAsync(string title, string message)
        {
            this.NotificationCount++;
            if (this.Fail) throw new IOException("Notifications unavailable.");
            return Task.CompletedTask;
        }
    }

    private sealed class NullLogger : IAppLogger
    {
        public static readonly NullLogger Instance = new();

        public void LogCritical(string message, Exception? ex = null) { }

        public void LogDebug(string message) { }

        public void LogError(string message, Exception? ex = null) { }

        public void LogInformation(string message) { }

        public void LogWarning(string message) { }
    }
}
