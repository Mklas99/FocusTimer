namespace FocusTimer.Platform.Windows
{
    using System;
    using System.Runtime.InteropServices;
    using FocusTimer.Core.Interfaces;

    /// <summary>
    /// Provides idle detection functionality for Windows platforms.
    /// </summary>
    public partial class WindowsIdleDetectionService : IIdleDetectionService, IDisposable
    {
        private readonly System.Timers.Timer _pollTimer;
        private readonly TimeSpan _idleThreshold = TimeSpan.FromMinutes(5);
        private bool _isIdle;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsIdleDetectionService"/> class.
        /// </summary>
        public WindowsIdleDetectionService()
        {
            this._pollTimer = new System.Timers.Timer(5000) { AutoReset = true };
            this._pollTimer.Elapsed += this.OnPollElapsed;
            this._pollTimer.Start();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="WindowsIdleDetectionService"/> class.
        /// </summary>
        ~WindowsIdleDetectionService()
        {
            this.Dispose(false);
        }

        /// <inheritdoc/>
        public event EventHandler<UserIdleEventArgs>? UserBecameIdle;

        /// <inheritdoc/>
        public event EventHandler<UserIdleEventArgs>? UserReturned;

        /// <summary>
        /// Disposes the idle detection service and releases all resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the idle detection service and releases all resources.
        /// </summary>
        /// <param name="disposing">Whether the method is being called from Dispose (true) or the finalizer (false).</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this._disposed)
            {
                return;
            }

            if (disposing)
            {
                this._pollTimer.Stop();
                this._pollTimer.Elapsed -= this.OnPollElapsed;
                this._pollTimer.Dispose();
            }

            this._disposed = true;
        }

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetLastInputInfo(ref LastInputInfo plii);

        [LibraryImport("kernel32.dll")]
        private static partial uint GetTickCount();

        private static TimeSpan GetIdleDuration()
        {
            var lastInputInfo = new LastInputInfo
            {
                CbSize = (uint)Marshal.SizeOf<LastInputInfo>(),
            };

            if (!GetLastInputInfo(ref lastInputInfo))
            {
                return TimeSpan.Zero;
            }

            uint currentTick = GetTickCount();
            uint elapsedMilliseconds = unchecked(currentTick - lastInputInfo.DwTime);
            return TimeSpan.FromMilliseconds(elapsedMilliseconds);
        }

        private void OnPollElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            TimeSpan idleDuration = GetIdleDuration();
            if (idleDuration >= this._idleThreshold)
            {
                if (this._isIdle)
                {
                    return;
                }

                this._isIdle = true;
                this.UserBecameIdle?.Invoke(this, new UserIdleEventArgs(DateTime.Now));
                return;
            }

            if (!this._isIdle)
            {
                return;
            }

            this._isIdle = false;
            this.UserReturned?.Invoke(this, new UserIdleEventArgs(DateTime.Now));
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LastInputInfo
        {
            public uint CbSize;
            public uint DwTime;
        }
    }
}
