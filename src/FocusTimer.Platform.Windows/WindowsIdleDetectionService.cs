using System;
using FocusTimer.Core.Interfaces;

namespace FocusTimer.Platform.Windows
{
    public class WindowsIdleDetectionService : IIdleDetectionService
    {
        public event EventHandler<UserIdleEventArgs> UserBecameIdle;
        public event EventHandler<UserIdleEventArgs> UserReturned;

        // TODO: Implement Win32 idle detection and raise events
    }
}
