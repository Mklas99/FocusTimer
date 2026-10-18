namespace FocusTimer.App.Services
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>Conservative session checks in addition to the backend's renderer capability.</summary>
    internal static class WindowsWidgetComposition
    {
        /// <summary>Delegate signature for querying DWM composition state.</summary>
        /// <param name="enabled">Outputs 1 if composition is enabled, 0 otherwise.</param>
        /// <returns>HRESULT status code.</returns>
        internal delegate int DwmCompositionProbe(out int enabled);

        /// <summary>Gets or sets the hook used to probe Windows OS build requirements.</summary>
        internal static Func<bool> WindowsVersionProbe { get; set; } = () => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);

        /// <summary>Gets or sets the hook used to probe system metrics (e.g. remote session flag).</summary>
        internal static Func<int, int> SystemMetricsProbe { get; set; } = GetSystemMetrics;

        /// <summary>Gets or sets the hook used to query DWM composition.</summary>
        internal static DwmCompositionProbe DwmCompositionProbeHook { get; set; } = DwmIsCompositionEnabled;

        /// <summary>Checks host-backdrop OS support, local session and DWM composition.</summary>
        /// <returns>Whether attachment may be attempted; this is not visual qualification.</returns>
        internal static bool IsSupported()
        {
            if (!WindowsVersionProbe())
            {
                return false;
            }

            if (SystemMetricsProbe(0x1000) != 0)
            {
                return false;
            }

            return DwmCompositionProbeHook(out int enabled) == 0 && enabled != 0;
        }

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern int GetSystemMetrics(int index);

        [DllImport("dwmapi.dll", ExactSpelling = true)]
        private static extern int DwmIsCompositionEnabled(out int enabled);
    }
}
