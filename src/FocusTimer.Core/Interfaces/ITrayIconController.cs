namespace FocusTimer.Core.Interfaces
{
    using System;
    using Avalonia.Controls;
    using Avalonia.Controls.Notifications; // Add this if TrayIcon is in this namespace, otherwise use the correct namespace
    using FocusTimer.Core.Models;

    /// <summary>
    /// Defines the contract for controlling tray icon behavior and menu interactions.
    /// </summary>
    public interface ITrayIconController
    {
        /// <summary>
        /// Occurs when a menu action is triggered.
        /// </summary>
        event EventHandler<MenuActionEventArgs>? MenuAction;

        /// <summary>
        /// Occurs when time entries have been logged.
        /// </summary>
        event EventHandler? OnEntriesLogged;

        /// <summary>
        /// Updates the tray icon state based on the provided timer state.
        /// </summary>
        /// <param name="state">The current timer state.</param>
        void UpdateState(TimerState state);

        /// <summary>
        /// Shows the tray icon menu.
        /// </summary>
        void ShowMenu();

        /// <summary>
        /// Hides the tray icon menu.
        /// </summary>
        void HideMenu();

        /// <summary>
        /// Raises the EntriesLogged event with the provided time entries.
        /// </summary>
        /// <param name="entries">The collection of time entries that were logged.</param>
        void RaiseEntriesLogged(IEnumerable<FocusTimer.Core.Models.TimeEntry> entries);

        /// <summary>
        /// Sets the tray icon for the controller.
        /// </summary>
        /// <param name="trayIcon">The tray icon to be set.</param>
        void SetTrayIcon(TrayIcon trayIcon);
    }

    /// <summary>
    /// Provides event arguments for menu action interactions.
    /// </summary>
    public class MenuActionEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MenuActionEventArgs"/> class.
        /// </summary>
        /// <param name="action">The menu action identifier.</param>
        public MenuActionEventArgs(string action) => this.Action = action;

        /// <summary>
        /// Gets or sets the menu action identifier.
        /// </summary>
        public string Action { get; set; }
    }
}
