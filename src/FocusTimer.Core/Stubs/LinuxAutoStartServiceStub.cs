namespace FocusTimer.Core.Stubs
{
    using FocusTimer.Core.Interfaces;

    /// <summary>
    /// Linux stub for auto-start service.
    /// TODO: Implement using .desktop file in ~/.config/autostart/.
    /// </summary>
    public class LinuxAutoStartServiceStub : IAutoStartService
    {
        private readonly IAppLogger? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinuxAutoStartServiceStub"/> class.
        /// </summary>
        public LinuxAutoStartServiceStub()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LinuxAutoStartServiceStub"/> class.
        /// </summary>
        /// <param name="logger">The application logger instance, or null if logging is not required.</param>
        public LinuxAutoStartServiceStub(IAppLogger? logger)
        {
            this._logger = logger;
        }

        /// <inheritdoc/>
        public void SetAutoStart(bool enabled)
        {
            this._logger?.LogInformation($"[Linux Stub] SetAutoStart: {enabled}");

            // TODO: Implement by creating/removing .desktop file
            // Example path: ~/.config/autostart/FocusTimer.desktop
            // Desktop file should contain:
            // [Desktop Entry]
            // Type=Application
            // Name=FocusTimer
            // Exec=/path/to/FocusTimer
            // X-GNOME-Autostart-enabled=true
        }

        /// <inheritdoc/>
        public bool IsAutoStartEnabled()
        {
            this._logger?.LogDebug("[Linux Stub] IsAutoStartEnabled called.");

            // TODO: Check if .desktop file exists in ~/.config/autostart/
            return false;
        }
    }
}
