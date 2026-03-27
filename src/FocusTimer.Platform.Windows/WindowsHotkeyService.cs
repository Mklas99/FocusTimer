namespace FocusTimer.Platform.Windows
{
    using System.Runtime.InteropServices;
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;

    /// <summary>
    /// Windows implementation of global hotkey service using Win32 RegisterHotKey API.
    /// </summary>
    public class WindowsHotkeyService : IGlobalHotkeyService, IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private const int GWL_WNDPROC = -4;
        private readonly Dictionary<int, HotkeyDefinition> _hotkeyDefinitions = new();
        private readonly IAppLogger? _logger;

        private int _nextHotkeyId = 1;
        private IntPtr _hwnd;
        private IntPtr _originalWndProc;
        private WndProcDelegate? _subclassWndProc;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsHotkeyService"/> class with no logger.
        /// </summary>
        public WindowsHotkeyService()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsHotkeyService"/> class with an optional logger.
        /// </summary>
        /// <param name="logger">The application logger instance, or null if logging is not required.</param>
        public WindowsHotkeyService(IAppLogger? logger)
        {
            this._logger = logger;
        }

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        /// <inheritdoc/>
        public event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this._disposed)
            {
                return;
            }

            this.UnregisterAll();
            this.RestoreWndProc();
            this._hwnd = IntPtr.Zero;
            this._disposed = true;
        }

        /// <summary>
        /// Sets the window handle for receiving hotkey messages.
        /// Must be called before registering hotkeys.
        /// </summary>
        /// <param name="hwnd">The window handle to receive hotkey messages.</param>
        public void SetWindowHandle(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            if (this._hwnd == hwnd)
            {
                return;
            }

            if (this._hwnd != IntPtr.Zero)
            {
                this.UnregisterAll();
                this.RestoreWndProc();
            }

            this._hwnd = hwnd;
            this.HookWndProc();
        }

        /// <summary>
        /// Registers a hotkey using a legacy string definition format.
        /// </summary>
        /// <param name="hotkeyDefinition">The hotkey definition string to parse.</param>
        /// <param name="callback">The callback action to invoke when the hotkey is pressed.</param>
        // Legacy registration for compatibility
        public void RegisterHotkey(string hotkeyDefinition, Action callback)
        {
            if (this._hwnd == IntPtr.Zero)
            {
                this._logger?.LogWarning("Window handle not set. Cannot register hotkeys.");
                return;
            }

            var hotkey = HotkeyDefinition.Parse(hotkeyDefinition);
            if (hotkey == null)
            {
                this._logger?.LogWarning($"Invalid hotkey definition: {hotkeyDefinition}");
                return;
            }

            this.Register(hotkey.Value);
        }

        // New interface method

        /// <inheritdoc/>
        public void Register(HotkeyDefinition definition)
        {
            if (this._hwnd == IntPtr.Zero)
            {
                this._logger?.LogWarning("Window handle not set. Cannot register hotkeys.");
                return;
            }

            try
            {
                var id = this._nextHotkeyId++;
                var modifiers = ConvertModifiers(definition.Modifiers);
                if (RegisterHotKey(this._hwnd, id, modifiers, (uint)definition.KeyCode))
                {
                    this._hotkeyDefinitions[id] = definition;
                    this._logger?.LogInformation($"Registered hotkey {id}: {definition}");
                }
                else
                {
                    var error = Marshal.GetLastWin32Error();
                    this._logger?.LogWarning($"Failed to register hotkey {definition}: Win32 error {error}");
                }
            }
            catch (Exception ex)
            {
                this._logger?.LogError("Exception registering hotkey.", ex);
            }
        }

        /// <inheritdoc/>
        public void UnregisterAll()
        {
            if (this._hwnd == IntPtr.Zero)
            {
                return;
            }

            foreach (var id in this._hotkeyDefinitions.Keys.ToList())
            {
                try
                {
                    UnregisterHotKey(this._hwnd, id);
                    this._logger?.LogDebug($"Unregistered hotkey {id}");
                }
                catch (Exception ex)
                {
                    this._logger?.LogWarning($"Failed to unregister hotkey {id}: {ex.Message}");
                }
            }

            this._hotkeyDefinitions.Clear();
            this._nextHotkeyId = 1;
        }

        /// <summary>
        /// Processes WM_HOTKEY messages. Call this from your window's message handler.
        /// </summary>
        /// <param name="hotkeyId">The ID of the hotkey that was pressed.</param>
        public void ProcessHotkeyMessage(int hotkeyId)
        {
            if (this._hotkeyDefinitions.TryGetValue(hotkeyId, out var definition))
            {
                this.HotkeyPressed?.Invoke(this, new HotkeyPressedEventArgs(definition));
            }
        }

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

        private static uint ConvertModifiers(HotkeyModifiers modifiers)
        {
            uint result = 0;

            if (modifiers.HasFlag(HotkeyModifiers.Alt))
            {
                result |= 0x0001; // MOD_ALT
            }

            if (modifiers.HasFlag(HotkeyModifiers.Control))
            {
                result |= 0x0002; // MOD_CONTROL
            }

            if (modifiers.HasFlag(HotkeyModifiers.Shift))
            {
                result |= 0x0004; // MOD_SHIFT
            }

            if (modifiers.HasFlag(HotkeyModifiers.Win))
            {
                result |= 0x0008; // MOD_WIN
            }

            return result;
        }

        private void HookWndProc()
        {
            if (this._hwnd == IntPtr.Zero || this._subclassWndProc != null)
            {
                return;
            }

            this._originalWndProc = GetWindowLongPtr(this._hwnd, GWL_WNDPROC);
            if (this._originalWndProc == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                this._logger?.LogWarning($"Failed to get current WndProc. Win32 error {error}");
                return;
            }

            this._subclassWndProc = this.SubclassWndProc;
            var newWndProcPtr = Marshal.GetFunctionPointerForDelegate(this._subclassWndProc);
            var previousWndProc = SetWindowLongPtr(this._hwnd, GWL_WNDPROC, newWndProcPtr);
            if (previousWndProc == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                this._logger?.LogWarning($"Failed to subclass WndProc. Win32 error {error}");
                this._subclassWndProc = null;
                this._originalWndProc = IntPtr.Zero;
                return;
            }

            this._originalWndProc = previousWndProc;
            this._logger?.LogDebug("Hotkey message hook installed.");
        }

        private void RestoreWndProc()
        {
            if (this._hwnd == IntPtr.Zero || this._originalWndProc == IntPtr.Zero)
            {
                this._subclassWndProc = null;
                return;
            }

            var result = SetWindowLongPtr(this._hwnd, GWL_WNDPROC, this._originalWndProc);
            if (result == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                this._logger?.LogWarning($"Failed to restore WndProc. Win32 error {error}");
            }

            this._originalWndProc = IntPtr.Zero;
            this._subclassWndProc = null;
        }

        private IntPtr SubclassWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_HOTKEY)
            {
                this.ProcessHotkeyMessage(wParam.ToInt32());
                return IntPtr.Zero;
            }

            return CallWindowProc(this._originalWndProc, hWnd, msg, wParam, lParam);
        }
    }
}
