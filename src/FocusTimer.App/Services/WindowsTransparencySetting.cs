namespace FocusTimer.App.Services
{
    using System;
    using System.IO;
    using System.Security;
    using Microsoft.Win32;

    /// <summary>
    /// Reads the Windows transparency-effects preference. Windows applies it without changing the
    /// reported window transparency level, so the level alone cannot reveal it.
    /// </summary>
    internal static class WindowsTransparencySetting
    {
        private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        /// <summary>
        /// Returns whether transparency effects are switched off for the current user.
        /// </summary>
        /// <returns><see langword="true"/> when Windows draws acrylic surfaces as solid colors.</returns>
        public static bool IsDisabled()
        {
            if (!OperatingSystem.IsWindows())
            {
                return false;
            }

            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
                return key?.GetValue("EnableTransparency") is int enabled && enabled == 0;
            }
            catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
            {
                return false;
            }
        }
    }
}
