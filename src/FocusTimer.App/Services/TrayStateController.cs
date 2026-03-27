using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Threading;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

namespace FocusTimer.App.Services
{
    public class TrayStateController : ITrayIconController
    {
        private readonly ITimerService _timerService;
        private readonly TodayStatsService _todayStatsService;
        private readonly IAppLogger _logger;
        private readonly TrayIconAssets _trayIconAssets;
        private TrayIcon? _trayIcon;
        private TimerState _pendingState;
        private bool _isInitialized;

        public event EventHandler<MenuActionEventArgs>? MenuAction;
        public event EventHandler? OnEntriesLogged;

        public TrayStateController(
            ITimerService timerService,
            TodayStatsService todayStatsService,
            IAppLogger logger)
        {
            _timerService = timerService;
            _todayStatsService = todayStatsService;
            _logger = logger;
            _trayIconAssets = new TrayIconAssets();
            _pendingState = timerService.CurrentState;
            _isInitialized = false;
        }

        public void SetTrayIcon(TrayIcon trayIcon)
        {
            _trayIcon = trayIcon;
            
            // Subscribe to state changes only after tray icon is set
            if (!_isInitialized)
            {
                _timerService.StateChanged += OnTimerStateChanged;
                _isInitialized = true;

                _ = RefreshTodayAndUpdateTooltipAsync();
                
                // Apply pending state immediately
                UpdateState(_pendingState);
            }
        }

        private void OnTimerStateChanged(object? sender, TimerState state)
        {
            if (_trayIcon == null)
            {
                _pendingState = state;
                return;
            }
            
            UpdateState(state);
        }

        public void UpdateState(TimerState state)
        {
            _pendingState = state;
            
            if (_trayIcon == null)
            {
                return;
            }

            if (Dispatcher.UIThread.CheckAccess())
            {
                UpdateTrayIconInternal(state);
            }
            else
            {
                Dispatcher.UIThread.Invoke(() => UpdateTrayIconInternal(state));
            }
        }

        private void UpdateTrayIconInternal(TimerState state)
        {
            if (_trayIcon == null) return;

            _trayIcon.Icon = _trayIconAssets.GetIcon(state);

            var stateText = state switch
            {
                TimerState.Running => "Running",
                TimerState.Paused => "Paused",
                TimerState.Idle => "Idle",
                _ => "Unknown"
            };

            _trayIcon.ToolTipText = $"Focus Timer: {stateText} | {_todayStatsService.GetTodaySummaryText()}";
        }

        public void ShowMenu()
        {
            // Menu is shown automatically by the TrayIcon control
        }

        public void HideMenu()
        {
            // Menu is hidden automatically by the TrayIcon control
        }

        public void RaiseEntriesLogged(IEnumerable<TimeEntry> entries)
        {
            _ = RefreshAfterEntriesLoggedAsync(entries);
            OnEntriesLogged?.Invoke(this, EventArgs.Empty);
        }

        private async Task RefreshAfterEntriesLoggedAsync(IEnumerable<TimeEntry> entries)
        {
            try
            {
                await _todayStatsService.AddEntriesAsync(entries);
                UpdateState(_pendingState);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to refresh tray tooltip after entries were logged.", ex);
            }
        }

        private async Task RefreshTodayAndUpdateTooltipAsync()
        {
            try
            {
                await _todayStatsService.RefreshTodayAsync();
                UpdateState(_pendingState);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to initialize tray tooltip with today's stats.", ex);
            }
        }
    }
}
