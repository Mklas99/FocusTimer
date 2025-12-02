using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

namespace FocusTimer.Core.Stubs;

/// <summary>
/// Stub implementation of IActiveWindowService for initial testing.
/// </summary>
public class ActiveWindowServiceStub : IActiveWindowService
{
    public Task<ActiveWindowInfo?> GetForegroundWindowAsync()
    {
        // Return dummy data for now
        return Task.FromResult<ActiveWindowInfo?>(new ActiveWindowInfo
        {
            ProcessName = "StubApp",
            WindowTitle = "Stub Window Title"
        });
    }
}
