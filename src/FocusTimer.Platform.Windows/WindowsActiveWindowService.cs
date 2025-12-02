using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

namespace FocusTimer.Platform.Windows;

/// <summary>
/// Windows implementation of IActiveWindowService using Win32 APIs.
/// </summary>
public class WindowsActiveWindowService : IActiveWindowService
{
    private const int MaxTitleLength = 256;

    public Task<ActiveWindowInfo?> GetForegroundWindowAsync()
    {
        // Perform synchronous Win32 call wrapped in Task for interface compatibility
        var info = GetActiveWindow();
        return Task.FromResult(info);
    }

    private ActiveWindowInfo? GetActiveWindow()
    {
        try
        {
            // Get foreground window handle
            IntPtr hwnd = NativeMethods.GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
                return null;

            // Get window title
            var titleBuilder = new StringBuilder(MaxTitleLength);
            int titleLength = NativeMethods.GetWindowText(hwnd, titleBuilder, MaxTitleLength);
            string windowTitle = titleLength > 0 ? titleBuilder.ToString() : string.Empty;

            // Get process ID
            NativeMethods.GetWindowThreadProcessId(hwnd, out uint processId);
            string processName = "Unknown";

            if (processId != 0)
            {
                try
                {
                    using var process = Process.GetProcessById((int)processId);
                    // Some system processes may deny access to ProcessName
                    try
                    {
                        processName = process.ProcessName;
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
                return null;

            return new ActiveWindowInfo
            {
                ProcessName = processName,
                WindowTitle = windowTitle
            };
        }
        catch (Exception ex)
        {
            // Don't crash on Win32 errors; just return null
            System.Diagnostics.Debug.WriteLine($"Error getting active window: {ex.Message}");
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
