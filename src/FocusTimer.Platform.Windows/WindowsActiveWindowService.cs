namespace FocusTimer.Platform.Windows
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;

    /// <summary>
    /// Windows implementation of IActiveWindowService using Win32 APIs.
    /// </summary>
    public class WindowsActiveWindowService : IActiveWindowService
    {
        private const int MaxTitleLength = 256;
        private readonly IAppLogger? _logger;
        private readonly Func<IntPtr> _getForegroundWindow;
        private readonly Func<IntPtr, string> _getWindowTitle;
        private readonly Func<IntPtr, uint> _getProcessId;
        private readonly Func<int, string> _getProcessName;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsActiveWindowService"/> class without a logger.
        /// </summary>
        public WindowsActiveWindowService()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsActiveWindowService"/> class with an optional logger.
        /// </summary>
        /// <param name="logger">An optional logger for diagnostics.</param>
        public WindowsActiveWindowService(IAppLogger? logger)
            : this(logger, NativeMethods.GetForegroundWindow, GetNativeWindowTitle, GetNativeProcessId, GetNativeProcessName)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsActiveWindowService"/> class with replaceable Win32 lookups.
        /// </summary>
        /// <param name="logger">An optional logger for diagnostics.</param>
        /// <param name="getForegroundWindow">Returns the foreground window handle.</param>
        /// <param name="getWindowTitle">Returns the title of a window handle.</param>
        /// <param name="getProcessId">Returns the owning process ID of a window handle.</param>
        /// <param name="getProcessName">Returns the process name for a process ID.</param>
        internal WindowsActiveWindowService(
            IAppLogger? logger,
            Func<IntPtr> getForegroundWindow,
            Func<IntPtr, string> getWindowTitle,
            Func<IntPtr, uint> getProcessId,
            Func<int, string> getProcessName)
        {
            this._logger = logger;
            this._getForegroundWindow = getForegroundWindow;
            this._getWindowTitle = getWindowTitle;
            this._getProcessId = getProcessId;
            this._getProcessName = getProcessName;
        }

        /// <inheritdoc/>
        public Task<ActiveWindowInfo?> GetForegroundWindowAsync()
        {
            // Perform synchronous Win32 call wrapped in Task for interface compatibility
            var info = this.GetActiveWindow();
            return Task.FromResult(info);
        }

        private static string GetNativeWindowTitle(IntPtr hwnd)
        {
            var titleBuilder = new StringBuilder(MaxTitleLength);
            int titleLength = NativeMethods.GetWindowText(hwnd, titleBuilder, MaxTitleLength);
            return titleLength > 0 ? titleBuilder.ToString() : string.Empty;
        }

        private static uint GetNativeProcessId(IntPtr hwnd)
        {
            NativeMethods.GetWindowThreadProcessId(hwnd, out uint processId);
            return processId;
        }

        private static string GetNativeProcessName(int processId)
        {
            using var process = Process.GetProcessById(processId);
            return process.ProcessName;
        }

        private ActiveWindowInfo? GetActiveWindow()
        {
            try
            {
                // Get foreground window handle
                IntPtr hwnd = this._getForegroundWindow();
                if (hwnd == IntPtr.Zero)
                {
                    return null;
                }

                // Get window title
                string windowTitle = this._getWindowTitle(hwnd);

                // Get process ID
                uint processId = this._getProcessId(hwnd);
                string processName = "Unknown";

                if (processId != 0)
                {
                    try
                    {
                        // Some system processes may deny access to ProcessName
                        try
                        {
                            processName = this._getProcessName((int)processId);
                        }
                        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception ||
                                                   ex is InvalidOperationException)
                        {
                            // Access denied or process exited - use fallback
                            processName = $"Process_{processId}";
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Process not found (may have exited between calls)
                        processName = "Unknown";
                    }
                    catch (InvalidOperationException)
                    {
                        // Process access error
                        processName = $"Process_{processId}";
                    }
                }

                // Only return if we have at least window title or process name
                if (string.IsNullOrWhiteSpace(windowTitle) && processName == "Unknown")
                {
                    return null;
                }

                return new ActiveWindowInfo
                {
                    ProcessName = processName,
                    WindowTitle = windowTitle,
                };
            }
            catch (Exception ex)
            {
                // Don't crash on Win32 errors; just return null
                this._logger?.LogWarning($"Error getting active window: {ex.Message}");
                return null;
            }
        }

        private static class NativeMethods
        {
            [DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        }
    }
}
