#pragma warning disable
namespace FocusTimer.Platform.Windows.Tests;

using System.Diagnostics;
using System.Text.Json;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

/// <summary>Opt-in isolated workload driver; never queries real window titles or settings.</summary>
public class ActivityPollingMeasurements
{
    [Fact]
    public async Task RecordIsolatedCaptureWorkloads()
    {
        var output = Environment.GetEnvironmentVariable("FOCUSTIMER_POLLING_REPORT");
        if (string.IsNullOrEmpty(output)) return;
        var rows = new List<object>();
        var interval = int.Parse(Environment.GetEnvironmentVariable("FOCUSTIMER_POLLING_INTERVAL") ?? "1");
        foreach (var scenario in new[] { "stopped", "disabled", "stable", "titles", "switches" })
        {
            await Run(scenario, interval, 100);
            for (var run = 1; run <= 3; run++) rows.Add(await Run(scenario, interval, 10000));
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
        {
            Runtime = Environment.Version.ToString(), OS = Environment.OSVersion.ToString(),
            Machine = Environment.MachineName, CPUs = Environment.ProcessorCount, Interval = interval,
            Configuration = "Release", TimestampUtc = DateTimeOffset.UtcNow, Rows = rows,
        }, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static async Task<object> Run(string scenario, int interval, int ticks)
    {
        var clock = new Clock();
        var calls = 0;
        var names = 0;
        long lookupTicks = 0;
        var sample = 0;
        var ownPid = Environment.ProcessId;
        using var own = Process.GetCurrentProcess();
        var otherPid = Process.GetProcesses().Select(p => { using (p) return p.Id; })
            .FirstOrDefault(pid => pid > 0 && pid != ownPid && CanRead(pid), ownPid);
        var windows = new WindowsActiveWindowService(null,
            () => { calls++; sample++; return new IntPtr(1); },
            _ => scenario == "titles" ? $"Synthetic {sample}" : "Synthetic",
            _ => (uint)(scenario == "switches" && sample % 2 == 0 ? otherPid : ownPid),
            pid =>
            {
                names++;
                var start = Stopwatch.GetTimestamp();
                using var process = Process.GetProcessById(pid);
                var name = process.ProcessName;
                lookupTicks += Stopwatch.GetTimestamp() - start;
                return name;
            }, ProcessLifetime.TryOpen);
        var tracker = new SessionTracker(windows, new Logger(), clock, new SourcePlatformProvider(), () => "synthetic-device");
        typeof(SessionTracker).GetMethod("SetPollingInterval")?.Invoke(tracker, [interval]);
        tracker.SetTrackingEnabled(scenario != "disabled");
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        own.Refresh();
        var handlesBefore = own.HandleCount;
        var cpu = own.TotalProcessorTime;
        var allocated = GC.GetTotalAllocatedBytes(true);
        var stopwatch = Stopwatch.StartNew();
        if (scenario != "stopped") await tracker.StartAsync(null);
        for (var i = 0; i < ticks; i++)
        {
            clock.Advance();
            await tracker.OnTimerTickAsync();
            tracker.DrainCompletedSegments();
        }
        tracker.StopTracking(EndReason.ApplicationExit);
        (windows as IDisposable)?.Dispose();
        stopwatch.Stop(); own.Refresh();
        return new { Scenario = scenario, Ticks = ticks, SimulatedSeconds = ticks,
            DurationMs = stopwatch.Elapsed.TotalMilliseconds, CpuSeconds = (own.TotalProcessorTime - cpu).TotalSeconds,
            AllocatedBytes = GC.GetTotalAllocatedBytes(true) - allocated, ForegroundLookups = calls,
            NameLookups = names, NameLookupMs = lookupTicks * 1000.0 / Stopwatch.Frequency,
            HandlesBefore = handlesBefore, HandlesAfter = own.HandleCount };
    }

    private static bool CanRead(int pid)
    {
        try { using var p = Process.GetProcessById(pid); _ = p.ProcessName; return true; }
        catch { return false; }
    }

    private sealed class Clock : TimeProvider
    {
        private long seconds;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public override long TimestampFrequency => 1;
        public override long GetTimestamp() => seconds;
        public override DateTimeOffset GetUtcNow() => new DateTimeOffset(2026, 9, 27, 9, 0, 0, TimeSpan.Zero).AddSeconds(seconds);
        public void Advance() => seconds++;
    }

    private sealed class Logger : IAppLogger
    {
        public void LogCritical(string m, Exception? e = null) { }
        public void LogError(string m, Exception? e = null) { }
        public void LogWarning(string m) { }
        public void LogInformation(string m) { }
        public void LogDebug(string m) { }
    }
}
