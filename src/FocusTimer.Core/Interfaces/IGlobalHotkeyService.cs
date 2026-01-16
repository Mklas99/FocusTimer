using System;
using FocusTimer.Core.Models;

namespace FocusTimer.Core.Interfaces
{
    public interface IGlobalHotkeyService
    {
        event EventHandler<HotkeyPressedEventArgs> HotkeyPressed;
        void Register(HotkeyDefinition definition);
        void UnregisterAll();
    }

    public class HotkeyPressedEventArgs : EventArgs
    {
        public HotkeyDefinition Definition { get; set; }
        public HotkeyPressedEventArgs(HotkeyDefinition definition) => Definition = definition;
    }
}
