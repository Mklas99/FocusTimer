using FocusTimer.Core.Interfaces;

namespace FocusTimer.Core.Stubs;

/// <summary>
/// Linux placeholder for idle detection.
/// TODO: Implement using X11/Wayland idle APIs.
/// </summary>
public class LinuxIdleDetectionServiceStub : IIdleDetectionService
{
    public event EventHandler<UserIdleEventArgs>? UserBecameIdle;
    public event EventHandler<UserIdleEventArgs>? UserReturned;
}
