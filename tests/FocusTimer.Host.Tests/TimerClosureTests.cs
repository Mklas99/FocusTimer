namespace FocusTimer.Host.Tests;

using System.Reflection;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;
using FocusTimer.Persistence;
using Microsoft.Extensions.DependencyInjection;

public sealed class TimerClosureTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ClosingRunningSegment_DoesNotWaitForAnotherSettingsLoad(bool reset)
    {
        var provider = new DelayedSettingsProvider();
        var root = Path.Combine(Path.GetTempPath(), "FocusTimerClosure_" + Guid.NewGuid().ToString("N"));
        var build = Type.GetType("FocusTimer.Host.Program, FocusTimer.Host")!
            .GetMethod("BuildServiceProviderWithSettings", BindingFlags.Static | BindingFlags.NonPublic)!;
        var services = (IServiceProvider)build.Invoke(null, [Path.Combine(root, "logs"), false, Path.Combine(root, "worklogs"), provider])!;
        var settings = await provider.LoadAsync();
        services.GetRequiredService<InstallationIdentity>().Initialize(settings.DeviceId);
        var timer = services.GetRequiredService<ITimerService>();
        var tracker = services.GetRequiredService<SessionTracker>();

        timer.Start();
        await Task.Delay(1100);
        var closing = Task.Run(() =>
        {
            if (reset) timer.Reset();
            else timer.Pause();
        });

        try
        {
            Assert.Same(closing, await Task.WhenAny(closing, Task.Delay(TimeSpan.FromSeconds(2))));
            await closing;
            var entry = Assert.Single(tracker.DrainCompletedSegments());
            Assert.True(entry.Duration > TimeSpan.Zero);
            Assert.Equal(settings.DeviceId, entry.SourceDeviceId);
            Assert.Equal(reset ? TimerState.Idle : TimerState.Paused, timer.CurrentState);
            if (reset) Assert.Equal(TimeSpan.Zero, timer.Elapsed);
            else Assert.True(timer.Elapsed > TimeSpan.Zero);
            Assert.Equal(1, provider.LoadCount);

            services.GetRequiredService<InstallationIdentity>().Initialize("changed-on-reload");
            timer.Start();
            await Task.Delay(50);
            timer.Pause();
            var next = Assert.Single(tracker.DrainCompletedSegments());
            Assert.Equal(entry.SourceDeviceId, next.SourceDeviceId);
            Assert.Equal(1, provider.LoadCount);
        }
        finally
        {
            provider.CompletePending();
            await closing.WaitAsync(TimeSpan.FromSeconds(2));
            (services as IDisposable)?.Dispose();
        }
    }

    [Fact]
    public async Task DisabledWorkLogging_CreatesNoSegmentsOrSettingsReads()
    {
        var provider = new DelayedSettingsProvider();
        var root = Path.Combine(Path.GetTempPath(), "FocusTimerClosure_" + Guid.NewGuid().ToString("N"));
        var build = Type.GetType("FocusTimer.Host.Program, FocusTimer.Host")!
            .GetMethod("BuildServiceProviderWithSettings", BindingFlags.Static | BindingFlags.NonPublic)!;
        var services = (IServiceProvider)build.Invoke(null, [Path.Combine(root, "logs"), false, Path.Combine(root, "worklogs"), provider])!;
        var settings = await provider.LoadAsync();
        services.GetRequiredService<InstallationIdentity>().Initialize(settings.DeviceId);
        var tracker = services.GetRequiredService<SessionTracker>();
        tracker.SetTrackingEnabled(false);
        var timer = services.GetRequiredService<ITimerService>();

        timer.Start();
        await Task.Delay(50);
        timer.Pause();

        Assert.Empty(tracker.DrainCompletedSegments());
        Assert.Equal(1, provider.LoadCount);
        (services as IDisposable)?.Dispose();
    }

    [Fact]
    public async Task ClosedSegments_AcrossRestart_AppendOnceWithSameDeviceId()
    {
        var root = Path.Combine(Path.GetTempPath(), "FocusTimerRestart_" + Guid.NewGuid().ToString("N"));
        var settingsPath = Path.Combine(root, "settings.json");
        var worklogs = Path.Combine(root, "worklogs");
        var firstId = string.Empty;
        var build = Type.GetType("FocusTimer.Host.Program, FocusTimer.Host")!
            .GetMethod("BuildServiceProviderWithSettings", BindingFlags.Static | BindingFlags.NonPublic)!;

        for (var launch = 0; launch < 2; launch++)
        {
            var provider = new JsonSettingsProvider(settingsPath);
            var settings = await provider.LoadAsync();
            settings.WorklogDirectory = worklogs;
            await provider.SaveAsync(settings);
            firstId = launch == 0 ? settings.DeviceId : firstId;
            Assert.Equal(firstId, settings.DeviceId);

            var services = (IServiceProvider)build.Invoke(null, [Path.Combine(root, "logs"), false, worklogs, provider])!;
            services.GetRequiredService<InstallationIdentity>().Initialize(settings.DeviceId);
            var tracker = services.GetRequiredService<SessionTracker>();
            await tracker.StartAsync(null);
            await Task.Delay(50);
            tracker.StopTracking(EndReason.ManualPause);
            var entry = Assert.Single(tracker.DrainCompletedSegments());
            Assert.Equal(firstId, entry.SourceDeviceId);
            Assert.True((await services.GetRequiredService<IWorklogStore>().AppendAsync([entry])).IsSuccess);
            (services as IDisposable)?.Dispose();
        }

        var result = await new CsvSessionRepository(new JsonSettingsProvider(settingsPath))
            .QueryAsync(new WorklogQuery(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1)));
        Assert.True(result.Outcome.IsSuccess);
        Assert.Equal(2, result.Entries.Count);
        Assert.All(result.Entries, entry => Assert.Equal(firstId, entry.SourceDeviceId));
        Assert.Equal(2, result.Entries.Select(entry => entry.EntryId).Distinct().Count());
    }

    private sealed class DelayedSettingsProvider : ISettingsProvider
    {
        private readonly Settings _settings = new();
        private readonly TaskCompletionSource<Settings> _pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _loadCount;

        public int LoadCount => Volatile.Read(ref this._loadCount);

        public Task<Settings> LoadAsync() => Interlocked.Increment(ref this._loadCount) == 1
            ? Task.FromResult(this._settings)
            : this._pending.Task;

        public Task SaveAsync(Settings settings) => Task.CompletedTask;

        public void CompletePending() => this._pending.TrySetResult(this._settings);
    }
}
