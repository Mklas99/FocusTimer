namespace FocusTimer.Core.Stubs
{
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;

    /// <summary>
    /// Stub implementation of IActiveWindowService for initial testing.
    /// </summary>
    public class ActiveWindowServiceStub : IActiveWindowService
    {
        /// <inheritdoc/>
        public Task<ActiveWindowInfo?> GetForegroundWindowAsync()
        {
            // Return dummy data for now
            return Task.FromResult<ActiveWindowInfo?>(new ActiveWindowInfo
            {
                ProcessName = "StubApp",
                WindowTitle = "Stub Window Title",
            });
        }
    }
}
