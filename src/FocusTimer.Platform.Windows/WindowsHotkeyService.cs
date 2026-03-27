using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using System.Runtime.InteropServices;

namespace FocusTimer.Platform.Windows;

/// <summary>
/// Windows implementation of global hotkey service using Win32 RegisterHotKey API.
/// </summary>
public class WindowsHotkeyService : IGlobalHotkeyService, IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int GWL_WNDPROC = -4;
    private readonly Dictionary<int, HotkeyDefinition> _hotkeyDefinitions = new();
    public event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;
    private int _nextHotkeyId = 1;
    private IntPtr _hwnd;
    private IntPtr _originalWndProc;
    private WndProcDelegate? _subclassWndProc;
    private bool _disposed;

    // Win32 API imports
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// Sets the window handle for receiving hotkey messages.
    /// Must be called before registering hotkeys.
    /// </summary>
    public void SetWindowHandle(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        if (_hwnd == hwnd)
        {
            return;
        }

        if (_hwnd != IntPtr.Zero)
        {
            UnregisterAll();
            RestoreWndProc();
        }

        _hwnd = hwnd;
        HookWndProc();
    }

    // Legacy registration for compatibility
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

        Register(hotkey.Value);
    }

    // New interface method
    public void Register(HotkeyDefinition definition)
    {
        if (_hwnd == IntPtr.Zero)
        {
            System.Diagnostics.Debug.WriteLine("Window handle not set. Cannot register hotkeys.");
            return;
        }

        try
        {
            var id = _nextHotkeyId++;
            var modifiers = ConvertModifiers(definition.Modifiers);
            if (RegisterHotKey(_hwnd, id, modifiers, (uint)definition.KeyCode))
            {
                _hotkeyDefinitions[id] = definition;
                System.Diagnostics.Debug.WriteLine($"Registered hotkey {id}: {definition}");
            }
            else
            {
                var error = Marshal.GetLastWin32Error();
                System.Diagnostics.Debug.WriteLine($"Failed to register hotkey {definition}: Win32 error {error}");
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

        foreach (var id in _hotkeyDefinitions.Keys.ToList())
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
        _hotkeyDefinitions.Clear();
        _nextHotkeyId = 1;
    }

    /// <summary>
    /// Processes WM_HOTKEY messages. Call this from your window's message handler.
    /// </summary>
    public void ProcessHotkeyMessage(int hotkeyId)
    {
        if (_hotkeyDefinitions.TryGetValue(hotkeyId, out var definition))
        {
            HotkeyPressed?.Invoke(this, new HotkeyPressedEventArgs(definition));
        }
    }

    private void HookWndProc()
    {
        if (_hwnd == IntPtr.Zero || _subclassWndProc != null)
        {
            return;
        }

        _originalWndProc = GetWindowLongPtr(_hwnd, GWL_WNDPROC);
        if (_originalWndProc == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();
            System.Diagnostics.Debug.WriteLine($"Failed to get current WndProc. Win32 error {error}");
            return;
        }

        _subclassWndProc = SubclassWndProc;
        var newWndProcPtr = Marshal.GetFunctionPointerForDelegate(_subclassWndProc);
        var previousWndProc = SetWindowLongPtr(_hwnd, GWL_WNDPROC, newWndProcPtr);
        if (previousWndProc == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();
            System.Diagnostics.Debug.WriteLine($"Failed to subclass WndProc. Win32 error {error}");
            _subclassWndProc = null;
            _originalWndProc = IntPtr.Zero;
            return;
        }

        _originalWndProc = previousWndProc;
        System.Diagnostics.Debug.WriteLine("Hotkey message hook installed.");
    }

    private void RestoreWndProc()
    {
        if (_hwnd == IntPtr.Zero || _originalWndProc == IntPtr.Zero)
        {
            _subclassWndProc = null;
            return;
        }

        var result = SetWindowLongPtr(_hwnd, GWL_WNDPROC, _originalWndProc);
        if (result == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();
            System.Diagnostics.Debug.WriteLine($"Failed to restore WndProc. Win32 error {error}");
        }

        _originalWndProc = IntPtr.Zero;
        _subclassWndProc = null;
    }

    private IntPtr SubclassWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_HOTKEY)
        {
            ProcessHotkeyMessage(wParam.ToInt32());
            return IntPtr.Zero;
        }

        return CallWindowProc(_originalWndProc, hWnd, msg, wParam, lParam);
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
        RestoreWndProc();
        _hwnd = IntPtr.Zero;
        _disposed = true;
    }
}
