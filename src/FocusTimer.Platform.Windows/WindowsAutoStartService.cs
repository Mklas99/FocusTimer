namespace FocusTimer.Platform.Windows
{
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;
    using Microsoft.Win32;

    /// <summary>
    /// Windows implementation of auto-start service using registry.
    /// </summary>
    public class WindowsAutoStartService : IAutoStartService
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "FocusTimer";
        private readonly IAppLogger? _logger;
        private readonly string _runKeyPath;

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
            this._runKeyPath = RunKey;
        }

        /// <summary>Initializes a new instance of the <see cref="WindowsAutoStartService"/> class with a specified Run key path.</summary>
        /// <param name="runKeyPath">Registry subkey path.</param>
        /// <param name="logger">Optional diagnostics logger.</param>
        public WindowsAutoStartService(string runKeyPath, IAppLogger? logger)
        {
            this._runKeyPath = runKeyPath;
            this._logger = logger;
        }

        /// <inheritdoc/>
        public void SetAutoStart(bool enabled)
        {
            try
            {
                using RegistryKey? key = this.OpenRunKey(writable: true);
                if (key == null)
                {
                    throw new IOException("The Windows Run registry key could not be opened for writing.");
                }

                if (enabled)
                {
                    // Get the path to the current executable
                    string exePath = GetExecutablePath();
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue(AppName, $"\"{exePath}\"");
                        this._logger?.LogInformation($"Auto-start enabled: {exePath}");
                    }
                    else
                    {
                        throw new IOException("The application executable path could not be determined.");
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
                throw;
            }
        }

        /// <inheritdoc/>
        public bool IsAutoStartEnabled()
        {
            try
            {
                using RegistryKey? key = this.OpenRunKey(writable: false);
                if (key == null)
                {
                    throw new IOException("The Windows Run registry key could not be opened for reading.");
                }

                string? value = key.GetValue(AppName) as string;
                return !string.IsNullOrEmpty(value);
            }
            catch (Exception ex)
            {
                this._logger?.LogError("Failed to check auto-start status.", ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public AutoStartRegistration CaptureRegistration()
        {
            using RegistryKey? key = this.OpenRunKey(writable: false);
            if (key == null)
            {
                throw new IOException("The Windows Run registry key could not be opened for reading.");
            }

            if (!key.GetValueNames().Contains(AppName, StringComparer.OrdinalIgnoreCase))
            {
                return new AutoStartRegistration(false);
            }

            object? value = key.GetValue(AppName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            if (value is not string command)
            {
                throw new IOException("The FocusTimer Run value is not a command string.");
            }

            return new AutoStartRegistration(
                !string.IsNullOrEmpty(command), command, key.GetValueKind(AppName).ToString());
        }

        /// <inheritdoc/>
        public void RestoreRegistration(AutoStartRegistration registration)
        {
            if (registration.Command == null || registration.ValueKind == null)
            {
                this.SetAutoStart(registration.Enabled);
                return;
            }

            using RegistryKey? key = this.OpenRunKey(writable: true);
            if (key == null)
            {
                throw new IOException("The Windows Run registry key could not be opened for writing.");
            }

            if (!Enum.TryParse(registration.ValueKind, out RegistryValueKind kind) ||
                kind is not (RegistryValueKind.String or RegistryValueKind.ExpandString))
            {
                throw new IOException("The saved FocusTimer Run value kind is invalid.");
            }

            key.SetValue(AppName, registration.Command, kind);
        }

        /// <summary>Opens the Windows Run key for reading or writing.</summary>
        /// <param name="writable">Whether write access is required.</param>
        /// <returns>The key, or null when unavailable.</returns>
        protected virtual RegistryKey? OpenRunKey(bool writable) =>
            Registry.CurrentUser.OpenSubKey(this._runKeyPath, writable);

        private static string GetExecutablePath()
        {
            // Preferred in modern .NET: process path works for both normal and single-file deployments.
            string? processPath = Environment.ProcessPath;

            if (!string.IsNullOrEmpty(processPath))
            {
                return processPath;
            }

            // Fallback for older runtimes/platform quirks.
            string? mainModulePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(mainModulePath))
            {
                return mainModulePath;
            }

            // Last resort in single-file scenarios where assembly locations are empty.
            string? entryAssemblyName = Assembly.GetEntryAssembly()?.GetName().Name;
            return !string.IsNullOrEmpty(entryAssemblyName)
                ? Path.Combine(AppContext.BaseDirectory, entryAssemblyName + ".exe")
                : Path.Combine(AppContext.BaseDirectory, "FocusTimer.Host.exe");
        }
    }
}
