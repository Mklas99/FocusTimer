namespace FocusTimer.App.Services
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>Conservative session checks in addition to the backend's renderer capability.</summary>
    internal static class WindowsWidgetComposition
    {
        /// <summary>Checks host-backdrop OS support, local session and DWM composition.</summary>
        /// <returns>Whether attachment may be attempted; this is not visual qualification.</returns>
        internal static bool IsSupported() => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000) &&
            GetSystemMetrics(0x1000) == 0 && DwmIsCompositionEnabled(out int enabled) == 0 && enabled != 0;

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern int GetSystemMetrics(int index);

        [DllImport("dwmapi.dll", ExactSpelling = true)]
        private static extern int DwmIsCompositionEnabled(out int enabled);
    }
}
