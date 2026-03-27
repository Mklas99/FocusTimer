#pragma warning disable CS0067

namespace FocusTimer.Core.Stubs
{
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;

    /// <summary>
    /// Linux stub for hotkey service.
    /// TODO: Global hotkeys on Linux require X11/Wayland-specific implementation.
    /// </summary>
    public class LinuxHotkeyServiceStub : IGlobalHotkeyService
    {
        private readonly IAppLogger? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinuxHotkeyServiceStub"/> class with no logger.
        /// </summary>
        public LinuxHotkeyServiceStub()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LinuxHotkeyServiceStub"/> class.
        /// </summary>
        /// <param name="logger">Optional logger for debugging purposes.</param>
        public LinuxHotkeyServiceStub(IAppLogger? logger)
        {
            this._logger = logger;
        }

        /// <inheritdoc/>
        public event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

        /// <inheritdoc/>
        public void Register(HotkeyDefinition definition)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Registers a hotkey with the specified definition and callback.
        /// </summary>
        /// <param name="hotkeyDefinition">The hotkey definition string.</param>
        /// <param name="callback">The action to invoke when the hotkey is pressed.</param>
        public void RegisterHotkey(string hotkeyDefinition, Action callback)
        {
            this._logger?.LogInformation($"[Linux Stub] RegisterHotkey: {hotkeyDefinition}");

            // TODO: Implement using X11 XGrabKey or other platform-specific APIs
            // Note: This is complex on Linux and may require different approaches
            // for X11 vs Wayland vs other display servers.
        }

        /// <inheritdoc/>
        public void UnregisterAll()
        {
            this._logger?.LogInformation("[Linux Stub] UnregisterAll");

            // TODO: Implement hotkey cleanup
        }
    }
}

#pragma warning restore CS0067
