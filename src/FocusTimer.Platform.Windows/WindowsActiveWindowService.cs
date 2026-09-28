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
    public partial class WindowsActiveWindowService : IActiveWindowService, IDisposable
    {
        private const int MaxTitleLength = 256;
        private readonly IAppLogger? _logger;
        private readonly Func<IntPtr> _getForegroundWindow;
        private readonly Func<IntPtr, string> _getWindowTitle;
        private readonly Func<IntPtr, uint> _getProcessId;
        private readonly Func<int, string> _getProcessName;
        private readonly Func<int, IProcessLifetime?> _openLifetime;
        private readonly object _cacheLock = new();
        private IProcessLifetime? _lifetime;
        private uint _cachedPid;
        private string? _cachedName;
        private bool _disposed;

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
            : this(logger, NativeMethods.GetForegroundWindow, GetNativeWindowTitle, GetNativeProcessId, GetNativeProcessName, ProcessLifetime.TryOpen)
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
        /// <param name="openLifetime">Opens an optional process lifetime for cache validation.</param>
        internal WindowsActiveWindowService(
            IAppLogger? logger,
            Func<IntPtr> getForegroundWindow,
            Func<IntPtr, string> getWindowTitle,
            Func<IntPtr, uint> getProcessId,
            Func<int, string> getProcessName,
            Func<int, IProcessLifetime?>? openLifetime = null)
        {
            this._logger = logger;
            this._getForegroundWindow = getForegroundWindow;
            this._getWindowTitle = getWindowTitle;
            this._getProcessId = getProcessId;
            this._getProcessName = getProcessName;
            this._openLifetime = openLifetime ?? (_ => null);
        }

        /// <inheritdoc/>
        public Task<ActiveWindowInfo?> GetForegroundWindowAsync()
        {
            // Perform synchronous Win32 call wrapped in Task for interface compatibility
            lock (this._cacheLock)
            {
                return Task.FromResult(this._disposed ? null : this.GetActiveWindow());
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            lock (this._cacheLock)
            {
                this.ClearCache();
                this._disposed = true;
            }
        }

        private static string GetNativeWindowTitle(IntPtr hwnd)
        {
            var titleBuilder = new StringBuilder(MaxTitleLength);
            int titleLength = NativeMethods.GetWindowText(hwnd, titleBuilder, MaxTitleLength);
            return titleLength > 0 ? titleBuilder.ToString() : string.Empty;
        }

        private static uint GetNativeProcessId(IntPtr hwnd)
        {
            uint threadId = NativeMethods.GetWindowThreadProcessId(hwnd, out uint processId);
            Debug.WriteLine($"GetWindowThreadProcessId returned thread id {threadId} for process id {processId}.");
            return processId;
        }

        private static string GetNativeProcessName(int processId)
        {
            using var process = Process.GetProcessById(processId);
            return process.ProcessName;
        }

        private void ClearCache()
        {
            this._lifetime?.Dispose();
            this._lifetime = null;
            this._cachedName = null;
            this._cachedPid = 0;
        }

        private string ResolveProcessName(uint pid)
        {
            if (pid == this._cachedPid && this._lifetime?.IsAlive == true)
            {
                return this._cachedName!;
            }

            this.ClearCache();
            IProcessLifetime? lifetime;
            try
            {
                lifetime = this._openLifetime((int)pid);
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or ArgumentException)
            {
                lifetime = null;
            }

            try
            {
                var name = this._getProcessName((int)pid);
                if (lifetime?.IsAlive == true)
                {
                    this._lifetime = lifetime;
                    this._cachedPid = pid;
                    this._cachedName = name;
                    lifetime = null;
                }

                return name;
            }
            finally
            {
                lifetime?.Dispose();
            }
        }

        private ActiveWindowInfo? GetActiveWindow()
        {
            try
            {
                // Get foreground window handle
                IntPtr hwnd = this._getForegroundWindow();
                if (hwnd == IntPtr.Zero)
                {
                    this.ClearCache();
                    return null;
                }

                // Get window title
                string windowTitle = this._getWindowTitle(hwnd);

                // Get process ID
                uint processId = this._getProcessId(hwnd);
                string processName = "Unknown";
                if (processId == 0)
                {
                    this.ClearCache();
                }

                if (processId != 0)
                {
                    try
                    {
                        // Some system processes may deny access to ProcessName
                        try
                        {
                            processName = this.ResolveProcessName(processId);
                        }
                        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
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
                return string.IsNullOrWhiteSpace(windowTitle) && processName == "Unknown"
                    ? null
                    : new ActiveWindowInfo
                    {
                        ProcessName = processName,
                        WindowTitle = windowTitle,
                    };
            }
            catch (Exception ex)
            {
                this.ClearCache();

                // Don't crash on Win32 errors; just return null
                this._logger?.LogWarning($"Error getting active window: {ex.Message}");
                return null;
            }
        }

        private static partial class NativeMethods
        {
            [LibraryImport("user32.dll")]
            public static partial IntPtr GetForegroundWindow();

            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

            [LibraryImport("user32.dll", SetLastError = true)]
            public static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        }
    }
}
