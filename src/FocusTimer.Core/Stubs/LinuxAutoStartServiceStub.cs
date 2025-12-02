using FocusTimer.Core.Interfaces;

namespace FocusTimer.Core.Stubs;

/// <summary>
/// Linux stub for auto-start service.
/// TODO: Implement using .desktop file in ~/.config/autostart/
/// </summary>
public class LinuxAutoStartServiceStub : IAutoStartService
{
    public void SetAutoStart(bool enabled)
    {
        System.Diagnostics.Debug.WriteLine($"[Linux Stub] SetAutoStart: {enabled}");
        
        // TODO: Implement by creating/removing .desktop file
        // Example path: ~/.config/autostart/FocusTimer.desktop
        // Desktop file should contain:
        // [Desktop Entry]
        // Type=Application
        // Name=FocusTimer
        // Exec=/path/to/FocusTimer
        // X-GNOME-Autostart-enabled=true
    }

    public bool IsAutoStartEnabled()
    {
        System.Diagnostics.Debug.WriteLine("[Linux Stub] IsAutoStartEnabled called");
        // TODO: Check if .desktop file exists in ~/.config/autostart/
        return false;
    }
}
