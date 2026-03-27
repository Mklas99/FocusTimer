#pragma warning disable CS0067

namespace FocusTimer.Core.Stubs
{
    using FocusTimer.Core.Interfaces;

    /// <summary>
    /// Linux placeholder for idle detection.
    /// TODO: Implement using X11/Wayland idle APIs.
    /// </summary>
    public class LinuxIdleDetectionServiceStub : IIdleDetectionService
    {
        /// <inheritdoc/>
        public event EventHandler<UserIdleEventArgs>? UserBecameIdle;

        /// <inheritdoc/>
        public event EventHandler<UserIdleEventArgs>? UserReturned;
    }
}

#pragma warning restore CS0067
