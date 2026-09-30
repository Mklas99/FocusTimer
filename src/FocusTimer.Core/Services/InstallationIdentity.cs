namespace FocusTimer.Core.Services;

/// <summary>Holds the installation identity used by this process.</summary>
public sealed class InstallationIdentity
{
    private string? _deviceId;

    /// <summary>Gets the identity without accessing persistence.</summary>
    public string DeviceId => Volatile.Read(ref this._deviceId)
        ?? throw new InvalidOperationException("Installation identity has not been initialized.");

    /// <summary>Sets the identity once for the lifetime of this process.</summary>
    /// <param name="deviceId">The identity loaded at startup.</param>
    public void Initialize(string deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        Interlocked.CompareExchange(ref this._deviceId, deviceId, null);
    }
}
