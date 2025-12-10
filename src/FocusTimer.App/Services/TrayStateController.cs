using Avalonia.Controls;
using System;
using FocusTimer.App.ViewModels;
using FocusTimer.Core.Models;

namespace FocusTimer.App.Services
{
    public class TrayStateController
    {
        private readonly TrayIcon _trayIcon;
        private readonly TimerWidgetViewModel _timerViewModel;
        private readonly TodayStatsService _todayStats;
        private readonly WindowIcon _iconRunning = new WindowIcon("avares://FocusTimer.App/Assets/FocusTimer-play.png");
        private readonly WindowIcon _iconPaused = new WindowIcon("avares://FocusTimer.App/Assets/FocusTimer-pause.png");

        public TrayStateController(TrayIcon trayIcon, TimerWidgetViewModel timerViewModel, TodayStatsService todayStats)
        {
            _trayIcon = trayIcon;
            _timerViewModel = timerViewModel;
            _todayStats = todayStats;

            _timerViewModel.PropertyChanged += TimerViewModel_PropertyChanged;
            UpdateTrayIconAndTooltip();
        }

        public void OnEntriesLogged(IEnumerable<TimeEntry> entries)
        {
            _todayStats.AddEntries(entries);
            UpdateTrayIconAndTooltip();
        }

        private void TimerViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TimerWidgetViewModel.IsRunning))
                UpdateTrayIconAndTooltip();
        }

        private void UpdateTrayIconAndTooltip()
        {
            _trayIcon.Icon = _timerViewModel.IsRunning ? _iconRunning : _iconPaused;
            var state = _timerViewModel.IsRunning ? "Running" : "Paused";
            var today = _todayStats.GetTodayTotal();
            var todayStr = today.TotalMinutes > 0 ? $"\nToday: {(int)today.TotalHours}h {today.Minutes}m" : "";
            _trayIcon.ToolTipText = $"FocusTimer – {state}{todayStr}";
        }
    }
}
