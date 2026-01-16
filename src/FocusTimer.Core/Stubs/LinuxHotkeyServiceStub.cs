using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

namespace FocusTimer.Core.Stubs;

/// <summary>
/// Linux stub for hotkey service.
/// TODO: Global hotkeys on Linux require X11/Wayland-specific implementation.
/// </summary>
public class LinuxHotkeyServiceStub : IGlobalHotkeyService
{
    public event EventHandler<HotkeyPressedEventArgs> HotkeyPressed;

    public void Register(HotkeyDefinition definition)
    {
        throw new NotImplementedException();
    }

    public void RegisterHotkey(string hotkeyDefinition, Action callback)
    {
        System.Diagnostics.Debug.WriteLine($"[Linux Stub] RegisterHotkey: {hotkeyDefinition}");
        
        // TODO: Implement using X11 XGrabKey or other platform-specific APIs
        // Note: This is complex on Linux and may require different approaches
        // for X11 vs Wayland vs other display servers.
    }

    public void UnregisterAll()
    {
        System.Diagnostics.Debug.WriteLine("[Linux Stub] UnregisterAll");
        // TODO: Implement hotkey cleanup
    }
}
