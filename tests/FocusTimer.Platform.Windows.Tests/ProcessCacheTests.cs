#pragma warning disable
namespace FocusTimer.Platform.Windows.Tests;

using System.ComponentModel;
using FocusTimer.Core.Interfaces;

public class ProcessCacheTests
{
    [Fact]
    public async Task SameLifetimeCachesNameButStillReadsTitleAndPid()
    {
        var pid = 1u; var title = "one"; var names = 0; var queries = 0;
        var lifetime = new Lifetime();
        using var service = new WindowsActiveWindowService(null, () => new(1), _ => title,
            _ => { queries++; return pid; }, _ => { names++; return "app"; }, _ => lifetime);
        await service.GetForegroundWindowAsync();
        title = "two";
        Assert.Equal("two", (await service.GetForegroundWindowAsync())!.WindowTitle);
        Assert.Equal(1, names); Assert.Equal(2, queries);
        lifetime.Alive = false;
        pid = 1; // PID reused; the old lifetime is dead.
        await service.GetForegroundWindowAsync();
        Assert.Equal(2, names);
        Assert.Equal(2, lifetime.Disposals); // Dead reference also rejected on re-open.
    }

    [Fact]
    public async Task SwitchAbsentWindowAndShutdownReleaseReferencesExactlyOnce()
    {
        var references = new List<Lifetime>(); var pid = 1u; var hwnd = new IntPtr(1);
        var service = new WindowsActiveWindowService(null, () => hwnd, _ => "title", _ => pid, _ => "app",
            _ => { var life = new Lifetime(); references.Add(life); return life; });
        await service.GetForegroundWindowAsync(); pid = 2;
        await service.GetForegroundWindowAsync(); hwnd = IntPtr.Zero;
        Assert.Null(await service.GetForegroundWindowAsync()); hwnd = new(1);
        await service.GetForegroundWindowAsync(); pid = 0;
        await service.GetForegroundWindowAsync(); pid = 3;
        await service.GetForegroundWindowAsync(); service.Dispose(); service.Dispose();
        Assert.All(references, r => Assert.Equal(1, r.Disposals));
        Assert.Null(await service.GetForegroundWindowAsync());
    }

    [Fact]
    public async Task UnavailableLifetimeAndFailedNamesAreRetried()
    {
        var names = 0; var fail = true; var lifetime = new Lifetime();
        using var service = new WindowsActiveWindowService(null, () => new(1), _ => "title", _ => 42,
            _ => { names++; if (fail) throw new Win32Exception(5); return "app"; }, _ => lifetime);
        Assert.Equal("Process_42", (await service.GetForegroundWindowAsync())!.ProcessName);
        fail = false;
        await service.GetForegroundWindowAsync();
        Assert.Equal(2, names);
        Assert.Equal(1, lifetime.Disposals);
        var uncachedNames = 0;
        using var uncached = new WindowsActiveWindowService(null, () => new(1), _ => "title", _ => 42,
            _ => { uncachedNames++; return "app"; }, _ => null);
        await uncached.GetForegroundWindowAsync(); await uncached.GetForegroundWindowAsync();
        Assert.Equal(2, uncachedNames);
    }

    [Fact]
    public async Task ConcurrentCallsAndDisposalAreSerialized()
    {
        var lifetime = new Lifetime(); var names = 0;
        using (var service = new WindowsActiveWindowService(null, () => new(1),
            _ => "title", _ => 42, _ => { names++; return "app"; }, _ => lifetime))
        {
            await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => service.GetForegroundWindowAsync())));
            Assert.Equal(1, names);
        }
        Assert.Equal(1, lifetime.Disposals);
    }

    private sealed class Lifetime : IProcessLifetime
    {
        public bool Alive = true;
        public int Disposals;
        public bool IsAlive => Alive;
        public void Dispose() => Disposals++;
    }
}
