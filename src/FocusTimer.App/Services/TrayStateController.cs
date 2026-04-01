namespace FocusTimer.App.Services
{
    using System;
    using System.Collections.Generic;
    using Avalonia.Controls;
    using Avalonia.Threading;
    using FocusTimer.App.Assets;
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;
    using FocusTimer.Core.Services;

    /// <summary>
    /// Controls the tray icon state and updates based on timer state changes.
    /// </summary>
    public class TrayStateController : ITrayIconController
    {
        private readonly ITimerService _timerService;
        private readonly TodayStatsService _todayStatsService;
        private readonly IAppLogger _logger;
        private readonly TrayIconAssets _trayIconAssets;
        private TrayIcon? _trayIcon;
        private TimerState _pendingState;
        private bool _isInitialized;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrayStateController"/> class.
        /// </summary>
        /// <param name="timerService">The timer service for managing timer state changes.</param>
        /// <param name="todayStatsService">The service for retrieving today's statistics.</param>
        /// <param name="logger">The logger for recording error and informational messages.</param>
        public TrayStateController(
            ITimerService timerService,
            TodayStatsService todayStatsService,
            IAppLogger logger)
        {
            this._timerService = timerService;
            this._todayStatsService = todayStatsService;
            this._logger = logger;
            this._trayIconAssets = new TrayIconAssets();
            this._pendingState = timerService.CurrentState;
            this._isInitialized = false;
        }

#pragma warning disable CS0067
        /// <inheritdoc/>
        public event EventHandler<MenuActionEventArgs>? MenuAction;
#pragma warning restore CS0067

        /// <inheritdoc/>
        public event EventHandler? OnEntriesLogged;

        /// <inheritdoc/>
        public void SetTrayIcon(TrayIcon trayIcon)
        {
            this._trayIcon = trayIcon;

            // Subscribe to state changes only after tray icon is set
            if (!this._isInitialized)
            {
                this._timerService.StateChanged += this.OnTimerStateChanged;
                this._isInitialized = true;

                _ = this.RefreshTodayAndUpdateTooltipAsync();

                // Apply pending state immediately
                this.UpdateState(this._pendingState);
            }
        }

        /// <inheritdoc/>
        public void UpdateState(TimerState state)
        {
            this._pendingState = state;

            if (this._trayIcon == null)
            {
                return;
            }

            if (Dispatcher.UIThread.CheckAccess())
            {
                this.UpdateTrayIconInternal(state);
            }
            else
            {
                Dispatcher.UIThread.Invoke(() => this.UpdateTrayIconInternal(state));
            }
        }

        /// <inheritdoc/>
        public void ShowMenu()
        {
            // Menu is shown automatically by the TrayIcon control
        }

        /// <inheritdoc/>
        public void HideMenu()
        {
            // Menu is hidden automatically by the TrayIcon control
        }

        /// <inheritdoc/>
        public void RaiseEntriesLogged(IEnumerable<TimeEntry> entries)
        {
            _ = this.RefreshAfterEntriesLoggedAsync(entries);
            this.OnEntriesLogged?.Invoke(this, EventArgs.Empty);
        }

        private void OnTimerStateChanged(object? sender, TimerState state)
        {
            if (this._trayIcon == null)
            {
                this._pendingState = state;
                return;
            }

            this.UpdateState(state);
        }

        private void UpdateTrayIconInternal(TimerState state)
        {
            if (this._trayIcon == null)
            {
                return;
            }

            this._trayIcon.Icon = this._trayIconAssets.GetIcon(state);

            var stateText = state switch
            {
                TimerState.Running => "Running",
                TimerState.Paused => "Paused",
                TimerState.Idle => "Idle",
                _ => "Unknown",
            };

            this._trayIcon.ToolTipText = $"Focus Timer: {stateText} | {this._todayStatsService.GetTodaySummaryText()}";
        }

        private async Task RefreshAfterEntriesLoggedAsync(IEnumerable<TimeEntry> entries)
        {
            try
            {
                await this._todayStatsService.AddEntriesAsync(entries);
                this.UpdateState(this._pendingState);
            }
            catch (Exception ex)
            {
                this._logger.LogError("Failed to refresh tray tooltip after entries were logged.", ex);
            }
        }

        private async Task RefreshTodayAndUpdateTooltipAsync()
        {
            try
            {
                await this._todayStatsService.RefreshTodayAsync();
                this.UpdateState(this._pendingState);
            }
            catch (Exception ex)
            {
                this._logger.LogError("Failed to initialize tray tooltip with today's stats.", ex);
            }
        }
    }
}
