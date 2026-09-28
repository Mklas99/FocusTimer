namespace FocusTimer.Platform.Windows
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.Win32.SafeHandles;

    /// <summary>Retains a synchronization handle for cheap process-exit checks.</summary>
    internal sealed partial class ProcessLifetime : IProcessLifetime
    {
        private readonly SafeProcessHandle _handle;

        private ProcessLifetime(SafeProcessHandle handle)
        {
            this._handle = handle;
        }

        /// <inheritdoc/>
        public bool IsAlive => WaitForSingleObject(this._handle, 0) == 258;

        /// <inheritdoc/>
        public void Dispose()
        {
            this._handle.Dispose();
        }

        /// <summary>Opens a lifetime handle, or returns null when access is unavailable.</summary>
        /// <param name="pid">The process ID.</param>
        /// <returns>An owned lifetime reference.</returns>
        internal static IProcessLifetime? TryOpen(int pid)
        {
            var handle = OpenProcess(0x00100000, false, pid);
            if (!handle.IsInvalid)
            {
                return new ProcessLifetime(handle);
            }

            handle.Dispose();
            return null;
        }

        [LibraryImport("kernel32.dll", SetLastError = true)]
        private static partial SafeProcessHandle OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, int pid);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        private static partial uint WaitForSingleObject(SafeProcessHandle handle, uint milliseconds);
    }
}
