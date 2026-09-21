namespace FocusTimer.Host.Tests;

using System.Reflection;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Services;
using FocusTimer.Core.Stubs;
using Microsoft.Extensions.DependencyInjection;

public sealed class ProgramCompositionTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "FocusTimerHostTests_" + Guid.NewGuid().ToString("N"));

    private static readonly MethodInfo BuildMethod =
        Type.GetType("FocusTimer.Host.Program, FocusTimer.Host")!
            .GetMethod("BuildServiceProvider", BindingFlags.Static | BindingFlags.NonPublic)!;

    public void Dispose()
    {
        try
        {
            Directory.Delete(this._root, recursive: true);
        }
        catch (IOException)
        {
            // Serilog may still hold the log file; the temp folder is disposable anyway.
        }
    }

    private IServiceProvider Build(string? envLogDir, bool isWindows) =>
        (IServiceProvider)BuildMethod.Invoke(null, new object?[] { envLogDir, isWindows, Path.Combine(this._root, "worklogs") })!;

    [Fact]
    public void BuildServiceProvider_GivenLogDirOverride_CreatesThatDirectory()
    {
        var logDir = Path.Combine(this._root, "custom-logs");

        this.Build(logDir, isWindows: true);

        Assert.True(Directory.Exists(logDir));
        Assert.True(Directory.Exists(Path.Combine(this._root, "worklogs")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildServiceProvider_GivenBlankLogDirOverride_FallsBackToDefaultDirectory(string? blank)
    {
        var ex = Record.Exception(() => this.Build(blank, isWindows: true));

        Assert.Null(ex);
        Assert.True(Directory.Exists(FocusTimer.Core.Models.Settings.DefaultApplicationLogDirectory));
    }

    [Fact]
    public void BuildServiceProvider_GivenNonWindows_RegistersLinuxStubs()
    {
        var sp = this.Build(Path.Combine(this._root, "logs"), isWindows: false);

        Assert.IsType<LinuxActiveWindowServiceStub>(sp.GetRequiredService<IActiveWindowService>());
        Assert.IsType<LinuxNotificationServiceStub>(sp.GetRequiredService<INotificationService>());
        Assert.IsType<LinuxAutoStartServiceStub>(sp.GetRequiredService<IAutoStartService>());
        Assert.IsType<LinuxHotkeyServiceStub>(sp.GetRequiredService<IGlobalHotkeyService>());
        Assert.IsType<LinuxIdleDetectionServiceStub>(sp.GetRequiredService<IIdleDetectionService>());
    }

    [Fact]
    public void BuildServiceProvider_GivenWindows_RegistersWindowsServices()
    {
        var sp = this.Build(Path.Combine(this._root, "logs"), isWindows: true);

        Assert.IsType<FocusTimer.Platform.Windows.WindowsActiveWindowService>(sp.GetRequiredService<IActiveWindowService>());
        Assert.IsType<FocusTimer.Platform.Windows.WindowsAutoStartService>(sp.GetRequiredService<IAutoStartService>());
        Assert.IsType<FocusTimer.Platform.Windows.WindowsHotkeyService>(sp.GetRequiredService<IGlobalHotkeyService>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BuildServiceProvider_ResolvesCoreSingletonsAsSameInstance(bool isWindows)
    {
        var sp = this.Build(Path.Combine(this._root, "logs"), isWindows);

        Assert.Same(sp.GetRequiredService<SessionTracker>(), sp.GetRequiredService<SessionTracker>());
        Assert.Same(sp.GetRequiredService<IThemeService>(), sp.GetRequiredService<IThemeService>());
        Assert.NotNull(sp.GetRequiredService<IEventBus>());
        Assert.NotNull(sp.GetRequiredService<IAppLogger>());
    }

    [Fact]
    public void BuildServiceProvider_GivenUnusableLogDirectory_ThrowsIOException()
    {
        Directory.CreateDirectory(this._root);
        var blockingFile = Path.Combine(this._root, "not-a-directory");
        File.WriteAllText(blockingFile, "x");

        var ex = Assert.Throws<TargetInvocationException>(() => this.Build(Path.Combine(blockingFile, "logs"), isWindows: true));

        Assert.IsAssignableFrom<IOException>(ex.InnerException);
    }
}
