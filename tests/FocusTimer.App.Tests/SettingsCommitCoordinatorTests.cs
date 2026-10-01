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

    private sealed class Store : ISettingsProvider, ISettingsCommitStore
    {
        private Settings? _previous;
        private AutoStartRegistration? _previousAutoStart;

        public Settings Saved { get; private set; } = new();

        public bool Pending { get; private set; }

        public bool FailRestore { get; set; }

        public Task<Settings> LoadAsync() => this.Pending
            ? Task.FromException<Settings>(new SettingsRecoveryRequiredException())
            : Task.FromResult(this.Saved.Clone());

        public Task SaveAsync(Settings settings)
        {
            this.Saved = settings.Clone();
            return Task.CompletedTask;
        }

        public Task<AutoStartRegistration?> GetPendingRecoveryAsync() =>
            Task.FromResult(this.Pending ? this._previousAutoStart : null);

        public Task BeginCommitAsync(Settings settings, AutoStartRegistration previousAutoStart)
        {
            this._previous = this.Saved.Clone();
            this._previousAutoStart = previousAutoStart;
            this.Saved = settings.Clone();
            this.Pending = true;
            return Task.CompletedTask;
        }

        public Task RestorePreviousAsync()
        {
            if (this.FailRestore)
            {
                throw new IOException("Previous file unavailable.");
            }

            this.Saved = this._previous!.Clone();
            return Task.CompletedTask;
        }

        public Task CompleteCommitAsync()
        {
            this.Pending = false;
            return Task.CompletedTask;
        }
    }

    private sealed class AutoStart : IAutoStartService
    {
        public bool Enabled { get; private set; }

        public bool FailWhenEnabled { get; set; }

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
