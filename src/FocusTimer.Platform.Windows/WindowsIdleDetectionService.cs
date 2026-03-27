using System;
using System.Runtime.InteropServices;
using FocusTimer.Core.Interfaces;

namespace FocusTimer.Platform.Windows
{
    public class WindowsIdleDetectionService : IIdleDetectionService, IDisposable
    {
        private readonly System.Timers.Timer _pollTimer;
        private readonly TimeSpan _idleThreshold = TimeSpan.FromMinutes(5);
        private bool _isIdle;
        private bool _disposed;

        public event EventHandler<UserIdleEventArgs>? UserBecameIdle;
        public event EventHandler<UserIdleEventArgs>? UserReturned;

        [StructLayout(LayoutKind.Sequential)]
        private struct LastInputInfo
        {
            public uint CbSize;
            public uint DwTime;
        }

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LastInputInfo plii);

        [DllImport("kernel32.dll")]
        private static extern uint GetTickCount();

        public WindowsIdleDetectionService()
        {
            _pollTimer = new System.Timers.Timer(5000);
            _pollTimer.AutoReset = true;
            _pollTimer.Elapsed += OnPollElapsed;
            _pollTimer.Start();
        }

        private void OnPollElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            var idleDuration = GetIdleDuration();
            if (idleDuration >= _idleThreshold)
            {
                if (_isIdle)
                {
                    return;
                }

                _isIdle = true;
                UserBecameIdle?.Invoke(this, new UserIdleEventArgs(DateTime.Now));
                return;
            }

            if (!_isIdle)
            {
                return;
            }

            _isIdle = false;
            UserReturned?.Invoke(this, new UserIdleEventArgs(DateTime.Now));
        }

        private static TimeSpan GetIdleDuration()
        {
            var lastInputInfo = new LastInputInfo
            {
                CbSize = (uint)Marshal.SizeOf<LastInputInfo>()
            };

            if (!GetLastInputInfo(ref lastInputInfo))
            {
                return TimeSpan.Zero;
            }

            var currentTick = GetTickCount();
            var elapsedMilliseconds = unchecked(currentTick - lastInputInfo.DwTime);
            return TimeSpan.FromMilliseconds(elapsedMilliseconds);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _pollTimer.Stop();
            _pollTimer.Elapsed -= OnPollElapsed;
            _pollTimer.Dispose();
            _disposed = true;
        }
    }
}
