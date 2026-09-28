namespace FocusTimer.Platform.Windows
{
    using System;

    /// <summary>Owns one process lifetime independently of PID reuse.</summary>
    internal interface IProcessLifetime : IDisposable
    {
        /// <summary>Gets a value indicating whether the owned process is still alive.</summary>
        bool IsAlive { get; }
    }
}
