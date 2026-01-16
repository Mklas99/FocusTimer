using System;
using FocusTimer.Core.Models;
using Avalonia.Controls;
using Avalonia.Controls.Notifications; // Add this if TrayIcon is in this namespace, otherwise use the correct namespace

namespace FocusTimer.Core.Interfaces
{
    public interface ITrayIconController
    {
        void UpdateState(TimerState state);
        void ShowMenu();
        void HideMenu();
        void RaiseEntriesLogged(IEnumerable<FocusTimer.Core.Models.TimeEntry> entries);
        void SetTrayIcon(TrayIcon trayIcon);
        event EventHandler<MenuActionEventArgs>? MenuAction;
        event EventHandler? OnEntriesLogged;
    }

    public class MenuActionEventArgs : EventArgs
    {
        public string Action { get; set; }
        public MenuActionEventArgs(string action) => Action = action;
    }
}
