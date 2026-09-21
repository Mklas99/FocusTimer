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
        using var coordinator = new WorklogPersistenceCoordinator(
            store,
            NullLogger.Instance,
            notificationService,
            () => true,
            entries => persisted = entries);
        var entry = CreateEntry();

        await coordinator.FlushAsync([entry], "test");
        await store.WaitForSuccessfulAppendAsync();

        Assert.Equal(2, store.AppendCount);
        Assert.Equal(1, notificationService.NotificationCount);
        Assert.Equal(entry, Assert.Single(persisted!));
    }

    private static TimeEntry CreateEntry() => new(
        "entry-id",
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

    private sealed class RecordingNotificationService : INotificationService
    {
        public int NotificationCount { get; private set; }

        public Task ShowBreakReminderAsync(string message, bool requireAcknowledgement) => Task.CompletedTask;

        public Task ShowNotificationAsync(string title, string message)
        {
            this.NotificationCount++;
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
