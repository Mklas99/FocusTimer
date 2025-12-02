using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using System.Runtime.InteropServices;

namespace FocusTimer.Platform.Windows;

/// <summary>
/// Windows implementation of global hotkey service using Win32 RegisterHotKey API.
/// </summary>
public class WindowsHotkeyService : IHotkeyService, IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private readonly Dictionary<int, Action> _hotkeyCallbacks = new();
    private int _nextHotkeyId = 1;
    private IntPtr _hwnd;
    private bool _disposed;

    // Win32 API imports
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    /// <summary>
    /// Sets the window handle for receiving hotkey messages.
    /// Must be called before registering hotkeys.
    /// </summary>
    public void SetWindowHandle(IntPtr hwnd)
    {
        _hwnd = hwnd;
    }

    public void RegisterHotkey(string hotkeyDefinition, Action callback)
    {
        if (_hwnd == IntPtr.Zero)
        {
            System.Diagnostics.Debug.WriteLine("Window handle not set. Cannot register hotkeys.");
            return;
        }

        var hotkey = HotkeyDefinition.Parse(hotkeyDefinition);
        if (hotkey == null)
        {
            System.Diagnostics.Debug.WriteLine($"Invalid hotkey definition: {hotkeyDefinition}");
            return;
        }

        RegisterHotkey(hotkey.Value, callback);
    }

    public void RegisterHotkey(HotkeyDefinition hotkey, Action callback)
    {
        if (_hwnd == IntPtr.Zero)
        {
            System.Diagnostics.Debug.WriteLine("Window handle not set. Cannot register hotkeys.");
            return;
        }

        try
        {
            var id = _nextHotkeyId++;
            var modifiers = ConvertModifiers(hotkey.Modifiers);
            
            if (RegisterHotKey(_hwnd, id, modifiers, (uint)hotkey.KeyCode))
            {
                _hotkeyCallbacks[id] = callback;
                System.Diagnostics.Debug.WriteLine($"Registered hotkey {id}: {hotkey}");
            }
            else
            {
                var error = Marshal.GetLastWin32Error();
                System.Diagnostics.Debug.WriteLine($"Failed to register hotkey {hotkey}: Win32 error {error}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception registering hotkey: {ex.Message}");
        }
    }

    public void UnregisterAll()
    {
        if (_hwnd == IntPtr.Zero)
            return;

        foreach (var id in _hotkeyCallbacks.Keys.ToList())
        {
            try
            {
                UnregisterHotKey(_hwnd, id);
                System.Diagnostics.Debug.WriteLine($"Unregistered hotkey {id}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to unregister hotkey {id}: {ex.Message}");
            }
        }

        _hotkeyCallbacks.Clear();
    }

    /// <summary>
    /// Processes WM_HOTKEY messages. Call this from your window's message handler.
    /// </summary>
    public void ProcessHotkeyMessage(int hotkeyId)
    {
        if (_hotkeyCallbacks.TryGetValue(hotkeyId, out var callback))
        {
            try
            {
                callback.Invoke();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Hotkey callback failed: {ex.Message}");
            }
        }
    }

    private static uint ConvertModifiers(HotkeyModifiers modifiers)
    {
        uint result = 0;
        
        if (modifiers.HasFlag(HotkeyModifiers.Alt))
            result |= 0x0001; // MOD_ALT
        if (modifiers.HasFlag(HotkeyModifiers.Control))
            result |= 0x0002; // MOD_CONTROL
        if (modifiers.HasFlag(HotkeyModifiers.Shift))
            result |= 0x0004; // MOD_SHIFT
        if (modifiers.HasFlag(HotkeyModifiers.Win))
            result |= 0x0008; // MOD_WIN

        return result;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        UnregisterAll();
        _disposed = true;
    }
}
