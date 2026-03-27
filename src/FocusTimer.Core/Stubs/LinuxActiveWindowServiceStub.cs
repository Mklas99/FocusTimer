namespace FocusTimer.Core.Stubs
{
    using System.Threading.Tasks;
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;

    /// <summary>
    /// Linux stub for IActiveWindowService.
    /// TODO: Implement X11/Wayland active window detection.
    /// </summary>
    public class LinuxActiveWindowServiceStub : IActiveWindowService
    {
        /// <inheritdoc/>
        public Task<ActiveWindowInfo?> GetForegroundWindowAsync()
        {
            // TODO: Implement using X11 (XGetInputFocus, XGetWindowProperty)
            // or Wayland compositor-specific APIs
            return Task.FromResult<ActiveWindowInfo?>(null);
        }
    }
}
