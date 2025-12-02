namespace FocusTimer.Core.Interfaces;

/// <summary>
/// Service for registering and managing global hotkeys.
/// </summary>
public interface IHotkeyService
{
    /// <summary>
    /// Registers a global hotkey with a callback action.
    /// </summary>
    void RegisterHotkey(string hotkeyDefinition, Action callback);

    /// <summary>
    /// Unregisters all hotkeys.
    /// </summary>
    void UnregisterAll();
}
