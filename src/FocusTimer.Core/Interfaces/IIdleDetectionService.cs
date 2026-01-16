namespace FocusTimer.Core.Interfaces
{
    public interface IIdleDetectionService
    {
        event EventHandler<UserIdleEventArgs> UserBecameIdle;
        event EventHandler<UserIdleEventArgs> UserReturned;
    }

    public class UserIdleEventArgs : EventArgs
    {
        public DateTime Timestamp { get; set; }
        public UserIdleEventArgs(DateTime timestamp) => Timestamp = timestamp;
    }
}
