namespace FocusTimer.Core.Interfaces
{
    using System;
    using FocusTimer.Core.Models;

    /// <summary>
    /// Service for managing global hotkey registration and events.
    /// </summary>
    public interface IGlobalHotkeyService
    {
        /// <summary>
        /// Occurs when a registered hotkey is pressed.
        /// </summary>
        event EventHandler<HotkeyPressedEventArgs> HotkeyPressed;

        /// <summary>
        /// Registers a global hotkey.
        /// </summary>
        /// <param name="definition">The hotkey definition to register.</param>
        void Register(HotkeyDefinition definition);

        /// <summary>
        /// Unregisters all registered hotkeys.
        /// </summary>
        void UnregisterAll();
    }

    /// <summary>
    /// Event arguments for hotkey press events.
    /// </summary>
    public class HotkeyPressedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HotkeyPressedEventArgs"/> class.
        /// </summary>
        /// <param name="definition">The hotkey definition that was pressed.</param>
        public HotkeyPressedEventArgs(HotkeyDefinition definition) => this.Definition = definition;

        /// <summary>
        /// Gets or sets the hotkey definition.
        /// </summary>
        public HotkeyDefinition Definition { get; set; }
    }
}
