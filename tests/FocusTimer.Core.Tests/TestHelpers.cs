namespace FocusTimer.Core.Tests;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

internal sealed class NullLogger : IAppLogger
{
    internal static readonly NullLogger Instance = new();

    public void LogCritical(string message, Exception? ex = null) { }

    public void LogDebug(string message) { }

    public void LogError(string message, Exception? ex = null) { }

    public void LogInformation(string message) { }

    public void LogWarning(string message) { }
}

internal sealed class FixedSettingsProvider : ISettingsProvider
{
    private readonly Settings _settings;

    internal FixedSettingsProvider(Settings settings)
    {
        _settings = settings;
    }

    public Task<Settings> LoadAsync() => Task.FromResult(_settings);

    public Task SaveAsync(Settings settings) => Task.CompletedTask;
}

internal sealed class RecordingNotificationService : INotificationService
{
    public int NotificationCount { get; private set; }

    public int BreakReminderCount { get; private set; }

    public Task ShowNotificationAsync(string title, string message)
    {
        NotificationCount++;
        return Task.CompletedTask;
    }

    public Task ShowBreakReminderAsync(string message, bool requireAcknowledgement)
    {
        BreakReminderCount++;
        return Task.CompletedTask;
    }
}
