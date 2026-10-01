namespace FocusTimer.App.Services
{
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;

    /// <summary>Result of a full Settings commit.</summary>
    public enum SettingsCommitStatus
    {
        /// <summary>All stages succeeded.</summary>
        Success,

        /// <summary>A stage failed and the previous state was restored.</summary>
        Failed,

        /// <summary>The previous state could not be fully restored.</summary>
        RecoveryRequired,
    }

    /// <summary>Coordinates file, registration, and runtime settings changes.</summary>
    public sealed class SettingsCommitCoordinator
    {
        private readonly ISettingsProvider _provider;
        private readonly ISettingsCommitStore? _store;
        private readonly IAutoStartService _autoStart;
        private readonly Func<Settings, Task> _activate;
        private readonly SemaphoreSlim _gate = new(1, 1);

        /// <summary>Initializes a new instance of the <see cref="SettingsCommitCoordinator"/> class.</summary>
        /// <param name="provider">Settings provider.</param>
        /// <param name="autoStart">Operating-system registration service.</param>
        /// <param name="activate">Awaited runtime activation.</param>
        public SettingsCommitCoordinator(
            ISettingsProvider provider,
            IAutoStartService autoStart,
            Func<Settings, Task> activate)
        {
            this._provider = provider;
            this._store = provider as ISettingsCommitStore;
            this._autoStart = autoStart;
            this._activate = activate;
        }

        /// <summary>Gets a value indicating whether recovery is required.</summary>
        public bool RecoveryRequired { get; private set; }

        /// <summary>Gets the last commit or recovery failure detail.</summary>
        public string? LastError { get; private set; }

        /// <summary>Commits a candidate or restores the previous state after failure.</summary>
        /// <param name="candidate">Candidate settings.</param>
        /// <param name="previous">Last successful settings.</param>
        /// <returns>The commit status.</returns>
        public async Task<SettingsCommitStatus> CommitAsync(Settings candidate, Settings previous)
        {
            if (this.RecoveryRequired)
            {
                return SettingsCommitStatus.RecoveryRequired;
            }

            if (!await this._gate.WaitAsync(0))
            {
                return SettingsCommitStatus.Failed;
            }

            try
            {
                this.LastError = null;
                AutoStartRegistration previousRegistration = this._autoStart.CaptureRegistration();
                try
                {
                    if (this._store is { } store)
                    {
                        await store.BeginCommitAsync(candidate, previousRegistration);
                    }
                    else
                    {
                        await this._provider.SaveAsync(candidate);
                    }
                }
                catch (Exception ex)
                {
                    this.LastError = ex.Message;
                    try
                    {
                        if (this._store is { } pendingStore && await pendingStore.GetPendingRecoveryAsync() is not null)
                        {
                            return await this.CompensateAsync(previous, previousRegistration);
                        }
                    }
                    catch
                    {
                        this.RecoveryRequired = true;
                        return SettingsCommitStatus.RecoveryRequired;
                    }

                    return SettingsCommitStatus.Failed;
                }

                try
                {
                    this._autoStart.SetAutoStart(candidate.AutoStartOnLogin);
                    await this._activate(candidate);
                    if (this._store is { } committedStore)
                    {
                        await committedStore.CompleteCommitAsync();
                    }

                    return SettingsCommitStatus.Success;
                }
                catch (Exception ex)
                {
                    this.LastError = ex.Message;
                    return await this.CompensateAsync(previous, previousRegistration);
                }
            }
            catch (Exception ex)
            {
                this.LastError = ex.Message;
                return SettingsCommitStatus.Failed;
            }
            finally
            {
                this._gate.Release();
            }
        }

        /// <summary>Retries an unfinished file and registration restoration.</summary>
        /// <param name="previousRuntime">Previous runtime settings when recovery happens in an open window.</param>
        /// <returns>True when no recovery remains.</returns>
        public async Task<bool> RecoverAsync(Settings? previousRuntime = null)
        {
            if (this._store is not { } store)
            {
                this.RecoveryRequired = false;
                return true;
            }

            try
            {
                AutoStartRegistration? previousRegistration = await store.GetPendingRecoveryAsync();
                if (previousRegistration is null)
                {
                    this.RecoveryRequired = false;
                    return true;
                }

                this._autoStart.RestoreRegistration(previousRegistration);
                await store.RestorePreviousAsync();
                if (previousRuntime != null)
                {
                    await this._activate(previousRuntime);
                }

                await store.CompleteCommitAsync();
                this.RecoveryRequired = false;
                return true;
            }
            catch (Exception ex)
            {
                this.LastError = ex.Message;
                this.RecoveryRequired = true;
                return false;
            }
        }

        private async Task<SettingsCommitStatus> CompensateAsync(Settings previous, AutoStartRegistration previousRegistration)
        {
            bool restored = true;
            try
            {
                await this._activate(previous);
            }
            catch
            {
                restored = false;
            }

            try
            {
                this._autoStart.RestoreRegistration(previousRegistration);
            }
            catch
            {
                restored = false;
            }

            if (this._store is { } store)
            {
                try
                {
                    await store.RestorePreviousAsync();
                }
                catch
                {
                    restored = false;
                }

                if (restored)
                {
                    try
                    {
                        await store.CompleteCommitAsync();
                    }
                    catch
                    {
                        restored = false;
                    }
                }
            }
            else
            {
                try
                {
                    await this._provider.SaveAsync(previous);
                }
                catch
                {
                    restored = false;
                }
            }

            this.RecoveryRequired = !restored;
            return restored ? SettingsCommitStatus.Failed : SettingsCommitStatus.RecoveryRequired;
        }
    }
}
