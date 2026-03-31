namespace FocusTimer.Platform.Windows
{
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using FocusTimer.Core.Interfaces;
    using Microsoft.Win32;

    /// <summary>
    /// Windows implementation of auto-start service using registry.
    /// </summary>
    public class WindowsAutoStartService : IAutoStartService
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "FocusTimer";
        private readonly IAppLogger? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsAutoStartService"/> class without a logger.
        /// </summary>
        public WindowsAutoStartService()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsAutoStartService"/> class with an optional logger.
        /// </summary>
        /// <param name="logger">An optional logger for diagnostics.</param>
        public WindowsAutoStartService(IAppLogger? logger)
        {
            this._logger = logger;
        }

        /// <inheritdoc/>
        public void SetAutoStart(bool enabled)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
                if (key == null)
                {
                    this._logger?.LogWarning("Failed to open registry key for auto-start.");
                    return;
                }

                if (enabled)
                {
                    // Get the path to the current executable
                    var exePath = GetExecutablePath();
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue(AppName, $"\"{exePath}\"");
                        this._logger?.LogInformation($"Auto-start enabled: {exePath}");
                    }
                }
                else
                {
                    // Remove the registry value
                    if (key.GetValue(AppName) != null)
                    {
                        key.DeleteValue(AppName);
                        this._logger?.LogInformation("Auto-start disabled.");
                    }
                }
            }
            catch (Exception ex)
            {
                this._logger?.LogError("Failed to set auto-start.", ex);
            }
        }

        /// <inheritdoc/>
        public bool IsAutoStartEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
                if (key == null)
                {
                    return false;
                }

                var value = key.GetValue(AppName) as string;
                return !string.IsNullOrEmpty(value);
            }
            catch (Exception ex)
            {
                this._logger?.LogError("Failed to check auto-start status.", ex);
                return false;
            }
        }

        private static string GetExecutablePath()
        {
            // Preferred in modern .NET: process path works for both normal and single-file deployments.
            var processPath = Environment.ProcessPath;

            if (!string.IsNullOrEmpty(processPath))
            {
                return processPath;
            }

            // Fallback for older runtimes/platform quirks.
            var mainModulePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(mainModulePath))
            {
                return mainModulePath;
            }

            // Last resort in single-file scenarios where assembly locations are empty.
            var entryAssemblyName = Assembly.GetEntryAssembly()?.GetName().Name;
            if (!string.IsNullOrEmpty(entryAssemblyName))
            {
                return Path.Combine(AppContext.BaseDirectory, entryAssemblyName + ".exe");
            }

            return Path.Combine(AppContext.BaseDirectory, "FocusTimer.Host.exe");
        }
    }
}
