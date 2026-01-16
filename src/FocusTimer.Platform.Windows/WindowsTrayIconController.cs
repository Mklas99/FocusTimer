using System;
using Avalonia.Controls;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

namespace FocusTimer.Platform.Windows
{
    public class WindowsTrayIconController : ITrayIconController
    {
        private TrayIcon? _trayIcon;
        public event EventHandler<MenuActionEventArgs>? MenuAction;
        public event EventHandler? OnEntriesLogged;

        public void UpdateState(TimerState state)
        {
            // TODO: Use _trayIconAssets.GetIcon(state) to set tray icon
            // TODO: Set tooltip text using unified logic
        }

        public void ShowMenu()
        {
            // TODO: Show tray menu
        }

        public void HideMenu()
        {
            // TODO: Hide tray menu
        }

        // TODO: Handle tray menu interactions and raise MenuAction event
        public void RaiseEntriesLogged()
        {
            OnEntriesLogged?.Invoke(this, EventArgs.Empty);
        }

        public void RaiseEntriesLogged(IEnumerable<TimeEntry> entries)
        {
            OnEntriesLogged?.Invoke(this, EventArgs.Empty);
        }
        public void SetTrayIcon(TrayIcon trayIcon)
        {
            _trayIcon = trayIcon;
        }
        
    }
}
