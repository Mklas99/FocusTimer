namespace FocusTimer.App.Tests;

using FocusTimer.App.Services;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

public class SettingsCommitCoordinatorTests
{
    [Fact]
    public async Task Commit_AwaitsActivationBeforeReportingSuccess()
    {
        var store = new Store();
        var autoStart = new AutoStart();
        var activation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var coordinator = new SettingsCommitCoordinator(store, autoStart, _ => activation.Task);
        var candidate = new Settings { BreakIntervalMinutes = 25, AutoStartOnLogin = true };

        Task<SettingsCommitStatus> commit = coordinator.CommitAsync(candidate, store.Saved.Clone());
        Assert.False(commit.IsCompleted);
        Assert.True(store.Pending);
        Assert.True(autoStart.Enabled);

        activation.SetResult();
        Assert.Equal(SettingsCommitStatus.Success, await commit);
        Assert.False(store.Pending);
        Assert.Equal(25, store.Saved.BreakIntervalMinutes);
    }

    [Fact]
    public async Task ActivationFailure_RestoresFileRegistrationAndRuntime()
    {
        var store = new Store();
        var autoStart = new AutoStart();
        var activations = new List<int>();
        var coordinator = new SettingsCommitCoordinator(store, autoStart, settings =>
        {
            activations.Add(settings.BreakIntervalMinutes);
            if (settings.BreakIntervalMinutes == 25)
            {
                throw new IOException("Runtime activation failed.");
            }

            return Task.CompletedTask;
        });

        SettingsCommitStatus result = await coordinator.CommitAsync(
            new Settings { BreakIntervalMinutes = 25, AutoStartOnLogin = true }, store.Saved.Clone());

        Assert.Equal(SettingsCommitStatus.Failed, result);
        Assert.Equal([25, 50], activations);
        Assert.Equal(50, store.Saved.BreakIntervalMinutes);
        Assert.False(autoStart.Enabled);
        Assert.False(store.Pending);
    }

    [Fact]
    public async Task FailedRestoration_BlocksCommitUntilRecoveryRetrySucceeds()
    {
        var store = new Store { FailRestore = true };
        var autoStart = new AutoStart { FailWhenEnabled = true };
        var coordinator = new SettingsCommitCoordinator(store, autoStart, _ => Task.CompletedTask);
        var candidate = new Settings { BreakIntervalMinutes = 25, AutoStartOnLogin = true };

        Assert.Equal(SettingsCommitStatus.RecoveryRequired,
            await coordinator.CommitAsync(candidate, store.Saved.Clone()));
        Assert.True(store.Pending);
        Assert.Equal(SettingsCommitStatus.RecoveryRequired,
            await coordinator.CommitAsync(candidate, store.Saved.Clone()));

        store.FailRestore = false;
        Assert.True(await coordinator.RecoverAsync());
        Assert.False(store.Pending);
        Assert.Equal(50, store.Saved.BreakIntervalMinutes);
        autoStart.FailWhenEnabled = false;
        Assert.Equal(SettingsCommitStatus.Success,
            await coordinator.CommitAsync(candidate, store.Saved.Clone()));
    }

    [Fact]
    public async Task StartupRecovery_RemainsPendingAcrossFailuresAndNeverLoadsCandidate()
    {
        var store = new Store { FailRestore = true };
        await store.BeginCommitAsync(new Settings { BreakIntervalMinutes = 25 }, new AutoStartRegistration(false));
        var coordinator = new SettingsCommitCoordinator(store, new AutoStart(), _ => Task.CompletedTask);

        Assert.False(await coordinator.RecoverAsync());
        Assert.False(await coordinator.RecoverAsync());
        Assert.True(store.Pending);
        await Assert.ThrowsAsync<SettingsRecoveryRequiredException>(() => store.LoadAsync());

        store.FailRestore = false;
        Assert.True(await coordinator.RecoverAsync());
        Assert.Equal(50, (await store.LoadAsync()).BreakIntervalMinutes);
    }

    [Fact]
    public async Task ConcurrentCommit_IsRejectedWithoutOverwritingThePendingCandidate()
    {
        var store = new Store();
        var activation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var coordinator = new SettingsCommitCoordinator(store, new AutoStart(), _ => activation.Task);
        var previous = store.Saved.Clone();
        var first = coordinator.CommitAsync(new Settings { BreakIntervalMinutes = 25 }, previous);

        Assert.Equal(SettingsCommitStatus.Failed,
            await coordinator.CommitAsync(new Settings { BreakIntervalMinutes = 30 }, previous));
        Assert.Equal(25, store.Saved.BreakIntervalMinutes);
        Assert.Equal(1, store.BeginCalls);

        activation.SetResult();
        Assert.Equal(SettingsCommitStatus.Success, await first);
        Assert.Equal(SettingsCommitStatus.Success,
            await coordinator.CommitAsync(new Settings { BreakIntervalMinutes = 30 }, store.Saved.Clone()));
    }

    [Fact]
    public async Task RegistrationCaptureFailure_DoesNotWriteSettingsAndAllowsRetry()
    {
        var store = new Store();
        var autoStart = new AutoStart { FailCapture = true };
        int activations = 0;
        var coordinator = new SettingsCommitCoordinator(store, autoStart, _ =>
        {
            activations++;
            return Task.CompletedTask;
        });

        Assert.Equal(SettingsCommitStatus.Failed,
            await coordinator.CommitAsync(new Settings(), store.Saved.Clone()));
        Assert.Equal("Registration unreadable.", coordinator.LastError);
        Assert.Equal(0, store.BeginCalls);
        Assert.Equal(0, activations);
        Assert.False(coordinator.RecoveryRequired);

        autoStart.FailCapture = false;
        Assert.Equal(SettingsCommitStatus.Success,
            await coordinator.CommitAsync(new Settings(), store.Saved.Clone()));
        Assert.Null(coordinator.LastError);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FileWriteFailure_RestoresOnlyWhenAJournalWasCreated(bool partialWrite)
    {
        var store = new Store { FailBegin = true, PartialBegin = partialWrite };
        var autoStart = new AutoStart();
        var activations = new List<int>();
        var coordinator = new SettingsCommitCoordinator(store, autoStart, settings =>
        {
            activations.Add(settings.BreakIntervalMinutes);
            return Task.CompletedTask;
        });

        Assert.Equal(SettingsCommitStatus.Failed, await coordinator.CommitAsync(
            new Settings { BreakIntervalMinutes = 25, AutoStartOnLogin = true }, store.Saved.Clone()));

        Assert.Equal("Candidate write failed.", coordinator.LastError);
        Assert.Equal(50, store.Saved.BreakIntervalMinutes);
        Assert.False(store.Pending);
        Assert.False(autoStart.Enabled);
        Assert.Equal(partialWrite ? new[] { 50 } : Array.Empty<int>(), activations);
        Assert.Equal(partialWrite ? 1 : 0, store.RestoreCalls);
    }

    [Fact]
    public async Task UnreadableJournalAfterWriteFailure_RequiresRecoveryBeforeAnotherCommit()
    {
        var store = new Store { FailBegin = true, PartialBegin = true, FailReadRecovery = true };
        var coordinator = new SettingsCommitCoordinator(store, new AutoStart(), _ => Task.CompletedTask);

        Assert.Equal(SettingsCommitStatus.RecoveryRequired,
            await coordinator.CommitAsync(new Settings(), store.Saved.Clone()));
        Assert.False(await coordinator.RecoverAsync());
        Assert.Equal("Recovery journal unreadable.", coordinator.LastError);
        Assert.Equal(SettingsCommitStatus.RecoveryRequired,
            await coordinator.CommitAsync(new Settings(), store.Saved.Clone()));
        Assert.Equal(1, store.BeginCalls);

        store.FailReadRecovery = false;
        Assert.True(await coordinator.RecoverAsync());
        Assert.False(store.Pending);
        Assert.False(coordinator.RecoveryRequired);
    }

    [Theory]
    [InlineData("runtime")]
    [InlineData("registration")]
    [InlineData("file")]
    [InlineData("journal")]
    public async Task CompensationFailure_AttemptsRemainingRestorationsAndRetainsJournal(string failingStage)
    {
        var store = new Store { FailRestore = failingStage == "file", FailComplete = failingStage == "journal" };
        var autoStart = new AutoStart { FailRestore = failingStage == "registration" };
        var activations = new List<int>();
        bool failRuntimeRestore = failingStage == "runtime";
        var coordinator = new SettingsCommitCoordinator(store, autoStart, settings =>
        {
            activations.Add(settings.BreakIntervalMinutes);
            if (settings.BreakIntervalMinutes == 25 || failRuntimeRestore)
            {
                throw new IOException("Activation failed.");
            }

            return Task.CompletedTask;
        });
        var previous = store.Saved.Clone();

        Assert.Equal(SettingsCommitStatus.RecoveryRequired, await coordinator.CommitAsync(
            new Settings { BreakIntervalMinutes = 25, AutoStartOnLogin = true }, previous));
        Assert.Equal(new[] { 25, 50 }, activations);
        Assert.Equal(1, autoStart.RestoreCalls);
        Assert.Equal(1, store.RestoreCalls);
        Assert.True(store.Pending);

        failRuntimeRestore = false;
        autoStart.FailRestore = false;
        store.FailRestore = false;
        store.FailComplete = false;
        Assert.True(await coordinator.RecoverAsync(previous));
        Assert.False(coordinator.RecoveryRequired);
        Assert.False(store.Pending);
        Assert.False(autoStart.Enabled);
        Assert.Equal(50, store.Saved.BreakIntervalMinutes);
        Assert.Equal(50, activations.Last());
    }

    [Fact]
    public async Task JournalCompletionFailure_RollsBackAnOtherwiseActivatedCandidate()
    {
        var store = new Store { CompleteFailuresRemaining = 1 };
        var autoStart = new AutoStart();
        var activations = new List<int>();
        var coordinator = new SettingsCommitCoordinator(store, autoStart, settings =>
        {
            activations.Add(settings.BreakIntervalMinutes);
            return Task.CompletedTask;
        });

        Assert.Equal(SettingsCommitStatus.Failed, await coordinator.CommitAsync(
            new Settings { BreakIntervalMinutes = 25, AutoStartOnLogin = true }, store.Saved.Clone()));
        Assert.Equal(new[] { 25, 50 }, activations);
        Assert.False(store.Pending);
        Assert.False(autoStart.Enabled);
        Assert.Equal(50, store.Saved.BreakIntervalMinutes);
        Assert.Equal("Journal completion failed.", coordinator.LastError);
    }

    [Fact]
    public async Task RecoveryWithoutPendingJournal_DoesNotTouchRegistrationOrRuntime()
    {
        var store = new Store();
        var autoStart = new AutoStart();
        var coordinator = new SettingsCommitCoordinator(store, autoStart,
            _ => throw new InvalidOperationException("No runtime activation expected."));

        Assert.True(await coordinator.RecoverAsync(new Settings()));
        Assert.Equal(0, autoStart.RestoreCalls);
        Assert.Equal(0, store.RestoreCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProviderWithoutJournal_CompensatesActivationFailure(bool failRollbackSave)
    {
        var provider = new PlainProvider { FailRollbackSave = failRollbackSave };
        var autoStart = new AutoStart();
        var coordinator = new SettingsCommitCoordinator(provider, autoStart, settings =>
            settings.BreakIntervalMinutes == 25
                ? Task.FromException(new IOException("Activation failed."))
                : Task.CompletedTask);

        Assert.Equal(failRollbackSave ? SettingsCommitStatus.RecoveryRequired : SettingsCommitStatus.Failed,
            await coordinator.CommitAsync(new Settings { BreakIntervalMinutes = 25, AutoStartOnLogin = true },
                provider.Saved.Clone()));
        Assert.Equal(new[] { 25, 50 }, provider.SaveAttempts);
        Assert.False(autoStart.Enabled);
        Assert.Equal(failRollbackSave ? 25 : 50, provider.Saved.BreakIntervalMinutes);
    }

    [Fact]
    public async Task ProviderWithoutJournal_SavesBeforeActivationAndSupportsNoOpRecovery()
    {
        var provider = new PlainProvider();
        var autoStart = new AutoStart();
        var coordinator = new SettingsCommitCoordinator(provider, autoStart, candidate =>
        {
            Assert.Equal(candidate.BreakIntervalMinutes, provider.Saved.BreakIntervalMinutes);
            Assert.True(autoStart.Enabled);
            return Task.CompletedTask;
        });

        Assert.True(await coordinator.RecoverAsync());
        Assert.Equal(SettingsCommitStatus.Success, await coordinator.CommitAsync(
            new Settings { BreakIntervalMinutes = 25, AutoStartOnLogin = true }, provider.Saved.Clone()));
        Assert.Equal(new[] { 25 }, provider.SaveAttempts);
    }

    private sealed class PlainProvider : ISettingsProvider
    {
        public Settings Saved { get; private set; } = new();
        public bool FailRollbackSave { get; set; }
        public List<int> SaveAttempts { get; } = new();
        public Task<Settings> LoadAsync() => Task.FromResult(this.Saved.Clone());
        public Task SaveAsync(Settings settings)
        {
            this.SaveAttempts.Add(settings.BreakIntervalMinutes);
            if (this.FailRollbackSave && this.SaveAttempts.Count > 1)
                throw new IOException("Rollback write failed.");
            this.Saved = settings.Clone();
            return Task.CompletedTask;
        }
    }

    private sealed class Store : ISettingsProvider, ISettingsCommitStore
    {
        private Settings? _previous;
        private AutoStartRegistration? _previousAutoStart;

        public Settings Saved { get; private set; } = new();

        public bool Pending { get; private set; }

        public bool FailRestore { get; set; }
        public bool FailBegin { get; set; }
        public bool PartialBegin { get; set; }
        public bool FailReadRecovery { get; set; }
        public bool FailComplete { get; set; }
        public int CompleteFailuresRemaining { get; set; }
        public int BeginCalls { get; private set; }
        public int RestoreCalls { get; private set; }

        public Task<Settings> LoadAsync() => this.Pending
            ? Task.FromException<Settings>(new SettingsRecoveryRequiredException())
            : Task.FromResult(this.Saved.Clone());

        public Task SaveAsync(Settings settings)
        {
            this.Saved = settings.Clone();
            return Task.CompletedTask;
        }

        public Task<AutoStartRegistration?> GetPendingRecoveryAsync() => this.FailReadRecovery
            ? Task.FromException<AutoStartRegistration?>(new IOException("Recovery journal unreadable."))
            : Task.FromResult(this.Pending ? this._previousAutoStart : null);

        public Task BeginCommitAsync(Settings settings, AutoStartRegistration previousAutoStart)
        {
            this.BeginCalls++;
            if (this.FailBegin && !this.PartialBegin)
                throw new IOException("Candidate write failed.");
            this._previous = this.Saved.Clone();
            this._previousAutoStart = previousAutoStart;
            this.Saved = settings.Clone();
            this.Pending = true;
            if (this.FailBegin)
                throw new IOException("Candidate write failed.");
            return Task.CompletedTask;
        }

        public Task RestorePreviousAsync()
        {
            this.RestoreCalls++;
            if (this.FailRestore)
            {
                throw new IOException("Previous file unavailable.");
            }

            this.Saved = this._previous!.Clone();
            return Task.CompletedTask;
        }

        public Task CompleteCommitAsync()
        {
            if (this.FailComplete || this.CompleteFailuresRemaining-- > 0)
                throw new IOException("Journal completion failed.");
            this.Pending = false;
            return Task.CompletedTask;
        }
    }

    private sealed class AutoStart : IAutoStartService
    {
        public bool Enabled { get; private set; }

        public bool FailWhenEnabled { get; set; }
        public bool FailCapture { get; set; }
        public bool FailRestore { get; set; }
        public int RestoreCalls { get; private set; }
        public AutoStartRegistration CaptureRegistration() => this.FailCapture
            ? throw new IOException("Registration unreadable.")
            : new(this.Enabled);
        public void RestoreRegistration(AutoStartRegistration registration)
        {
            this.RestoreCalls++;
            if (this.FailRestore)
                throw new IOException("Registration restoration denied.");
            this.SetAutoStart(registration.Enabled);
        }

        public bool IsAutoStartEnabled() => this.Enabled;

        public void SetAutoStart(bool enabled)
        {
            if (enabled && this.FailWhenEnabled)
            {
                throw new IOException("Registration denied.");
            }

            this.Enabled = enabled;
        }
    }
}
