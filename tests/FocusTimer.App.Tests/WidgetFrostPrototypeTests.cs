namespace FocusTimer.App.Tests;

using System.Reflection;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using Avalonia.Rendering;
using Avalonia.Threading;
using FocusTimer.App.Services;
using FocusTimer.App.ViewModels;
using FocusTimer.App.Views;
using FocusTimer.Core.Services;
using FocusTimer.Core.Models;
using FocusTimer.Core.Stubs;
using FocusTimer.Persistence;
using ReactiveUI;
using Rectangle = Avalonia.Controls.Shapes.Rectangle;

/// <summary>Test-only first feasibility gate on the actual widget. No saved material is added.</summary>
[Collection("Native appearance")]
public class WidgetFrostPrototypeTests
{
    [AppearanceNativeTests.NativeAppearanceFact]
    public void AvaloniaCandidates_RetainDetailAndWidenNormalizedEdges()
        => RunNativePrototype(dpiOnly: false);

    [AppearanceNativeTests.NativeAppearanceFact]
    public void DpiTransitions_UpdatePhysicalClipOnLiveHandle()
        => RunNativePrototype(dpiOnly: true);

    [AppearanceNativeTests.NativeAppearanceFact]
    public void ProductionPolicy_QualifiesSingleShellAndRepeatedLifetime()
        => RunProduction(performanceOnly: false);

    [AppearanceNativeTests.NativeAppearanceFact]
    public void ProductionPolicy_MeasuresReleaseWorkloads()
        => RunProduction(performanceOnly: true);

    private static void RunProduction(bool performanceOnly)
    {
        if (!OperatingSystem.IsWindows()) return;
        string folder = Environment.GetEnvironmentVariable("FOCUSTIMER_EVIDENCE_DIR")
            ?? Path.Combine(AppContext.BaseDirectory, "widget-frost-integration");
        Directory.CreateDirectory(folder);
        var report = new StringBuilder();
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            Window? backdrop = null;
            TimerWidgetWindow? widget = null;
            try
            {
                AppBuilder.Configure<AppearanceNativeTests.AppearanceTestApp>().UsePlatformDetect().SetupWithoutStarting();
                SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
                RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;
                var manager = new ThemeManager();
                manager.InitializeThemeResources();
                var theme = new ThemeService().GetBuiltInTheme("Dark")!;
                theme.WindowBackground = "#202020";
                manager.ApplyTheme(theme);
                var provider = new SettingsProviderStub();
                var logger = new QuietLogger();
                var notifications = new LinuxNotificationServiceStub();
                var tracker = new SessionTracker(new LinuxActiveWindowServiceStub(), logger);
                using var timer = new TimerService(tracker);
                using var reminders = new BreakReminderService(notifications, provider);
                using var vm = new TimerWidgetViewModel(provider, logger, new CsvSessionRepository(provider),
                    notifications, tracker, reminders, timer, null, new EventBus());
                vm.Settings.WidgetScale = 3;
                vm.Settings.AlwaysOnTop = true;
                backdrop = new Window
                {
                    Width = 1200, Height = 600, Position = new PixelPoint(80, 80),
                    SystemDecorations = SystemDecorations.None, ShowInTaskbar = false, Topmost = true,
                    Content = BuildPattern(128),
                };
                backdrop.Show();
                widget = new TimerWidgetWindow(manager)
                {
                    DataContext = vm, Position = new PixelPoint(200, 160), IsAppShuttingDown = true,
                };
                PlaceOnRequestedDisplay(backdrop, widget);
                widget.Show();
                widget.Activate();
                Settle();
                Assert.True(new WidgetBackdropCapability().IsAvailable);
                report.AppendLine($"Production policy, OS {Environment.OSVersion}, scale {widget.RenderScaling}, backend {typeof(Avalonia.Controls.Window).Assembly.GetName().Version}, HWND {widget.TryGetPlatformHandle()!.Handle}.");
                report.AppendLine($"Windows transparency disabled: {WindowsTransparencySetting.IsDisabled()}; local composition supported: {WindowsWidgetComposition.IsSupported()}.");
                Border shell = widget.FindControl<Border>("WidgetShell")!;
                Assert.Same(Application.Current!.Resources["WidgetShellActiveBrush"], shell.Background);
                void WaitMaterial(WidgetMaterialState state)
                {
                    var wait = Stopwatch.StartNew();
                    while (manager.EffectiveWidgetMaterial.State != state && wait.Elapsed < TimeSpan.FromSeconds(4))
                    {
                        Dispatcher.UIThread.RunJobs();
                        Thread.Sleep(20);
                    }

                    Assert.Equal(state, manager.EffectiveWidgetMaterial.State);
                }

                void Apply(string mode, double tint)
                {
                    theme.WidgetBlurMode = mode;
                    theme.BackgroundOpacity = tint;
                    manager.ApplyTheme(theme);
                    WaitMaterial(mode switch
                    {
                        WidgetBlurModes.Frosted => WidgetMaterialState.Frosted,
                        WidgetBlurModes.Solid => WidgetMaterialState.Solid,
                        _ => WidgetMaterialState.Clear,
                    });
                    Settle();
                }

                if (performanceOnly)
                {
                    MeasureWorkloads(widget, backdrop, Apply, folder, report);
                    return;
                }

                foreach (int width in new[] { 96, 128 })
                {
                    backdrop.Content = BuildPattern(width);
                    foreach (double tint in new[] { 0.0, 0.25, 0.5 })
                    {
                        string id = $"production-{width}-{tint:F2}";
                        Apply(WidgetBlurModes.Off, tint);
                        Profile clear = Capture(widget, folder, id + "-off", width, backdrop);
                        double corner = CornerLuma(widget);
                        Apply(WidgetBlurModes.Frosted, tint);
                        Profile frost = Capture(widget, folder, id + "-frost", width, backdrop);
                        Assert.True(frost.EdgeCount >= 4 && frost.Detail >= 10 && frost.Detail >= clear.Detail * 0.1);
                        Assert.True(frost.Width >= clear.Width * 2 && frost.Width >= clear.Width + 2);
                        Assert.InRange(Math.Abs(CornerLuma(widget) - corner), 0, 2);
                        Apply(WidgetBlurModes.Off, 1 - frost.Detail / 255);
                        Profile negative = Capture(widget, folder, id + "-negative", width, backdrop);
                        Assert.False(negative.Width >= clear.Width * 2 && negative.Width >= clear.Width + 2);
                        Apply(WidgetBlurModes.Solid, tint);
                        Assert.True(Capture(widget, folder, id + "-solid", width, backdrop).Detail < 2);
                        report.AppendLine($"{id}: clear {clear}; frost {frost}; opacity-only {negative}; corners/solid passed.");
                    }
                }

                Apply(WidgetBlurModes.Frosted, 1);
                Assert.True(Capture(widget, folder, "production-full-tint", 128, backdrop).Detail < 2);
                backdrop.Content = new Border { Background = Brushes.Black };
                Apply(WidgetBlurModes.Off, 0);
                byte[] clearForeground = CaptureForeground(widget);
                Apply(WidgetBlurModes.Frosted, 0);
                Assert.Equal(clearForeground, CaptureForeground(widget));
                report.AppendLine("Foreground crop exactly matches Off; 100% tint hides desktop detail.");
                backdrop.Content = BuildPattern(128);
                Apply(WidgetBlurModes.Off, 0);
                Profile sharp = Capture(widget, folder, "production-fade-clear", 128, backdrop);
                Apply(WidgetBlurModes.Frosted, 0);
                Profile full = Capture(widget, folder, "production-fade-full", 128, backdrop);
                widget.Opacity = 0.5;
                Settle();
                Profile half = Capture(widget, folder, "production-fade-half", 128, backdrop);
                widget.Opacity = 0;
                Settle();
                Profile zero = Capture(widget, folder, "production-fade-zero", 128, backdrop);
                double Error(Profile value) => sharp.Scan.Zip(value.Scan, (a, b) => Math.Abs(a - b)).Average();
                Assert.True(Error(half) > 2 && Error(half) < Error(full));
                Assert.InRange(Error(zero), 0, 2);
                report.AppendLine($"Fade residuals full {Error(full):F3}, half {Error(half):F3}, zero {Error(zero):F3}.");
                widget.Opacity = 1;
                foreach (bool compact in new[] { false, true })
                foreach (bool below in new[] { false, true })
                foreach (double scale in new[] { 0.5, 1.0, 3.0 })
                {
                    vm.Settings.UseCompactMode = compact;
                    vm.Settings.WidgetButtonsBelowClock = below;
                    vm.Settings.WidgetScale = scale;
                    Apply(WidgetBlurModes.Off, 0.5);
                    double corner = CornerLuma(widget, outermost: true);
                    Apply(WidgetBlurModes.Frosted, 0.5);
                    Assert.InRange(Math.Abs(CornerLuma(widget, outermost: true) - corner), 0, 2);
                    report.AppendLine($"Geometry compact {compact}, below {below}, scale {scale}, bounds {widget.Bounds}, radius {shell.CornerRadius} passed.");
                }

                theme.ThemeName = "High Contrast";
                manager.ApplyTheme(theme);
                WaitMaterial(WidgetMaterialState.SolidFallback);
                Assert.Equal(WidgetBlurModes.Frosted, manager.ActiveTheme!.WidgetBlurMode);
                Assert.Equal(0.5, manager.ActiveTheme.BackgroundOpacity);
                Assert.Equal(WindowTransparencyLevel.Transparent, widget.ActualTransparencyLevel);
                theme.ThemeName = "Dark";
                Apply(WidgetBlurModes.Frosted, 0.5);
                IntPtr handle = widget.TryGetPlatformHandle()!.Handle;
                void Cycle()
                {
                    theme.WidgetBlurMode = WidgetBlurModes.Frosted;
                    manager.ApplyTheme(theme);
                    WaitMaterial(WidgetMaterialState.Frosted);
                    widget.Hide();
                    widget.Show();
                    WaitMaterial(WidgetMaterialState.Frosted);
                    theme.WidgetBlurMode = WidgetBlurModes.Off;
                    manager.ApplyTheme(theme);
                    WaitMaterial(WidgetMaterialState.Clear);
                    Assert.Equal(handle, widget.TryGetPlatformHandle()!.Handle);
                }

                for (int i = 0; i < 10; i++) Cycle();
                Settle();
                using var process = Process.GetCurrentProcess();
                process.Refresh();
                int baseline = process.HandleCount;
                for (int i = 1; i <= 100; i++)
                {
                    Cycle();
                    if (i % 25 == 0)
                    {
                        Settle();
                        process.Refresh();
                        report.AppendLine($"Lifetime cycle {i}: handles {process.HandleCount}, baseline {baseline}.");
                    }
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                Settle();
                process.Refresh();
                Assert.True(process.HandleCount <= baseline, $"Native handle growth: {baseline} -> {process.HandleCount}");
                report.AppendLine("100 material/hide-show cycles retained one HWND and returned process handles to baseline.");
                widget.Close();
                Assert.Null(new WidgetBackdropCapability().TryAttach(handle));
                report.AppendLine("Closed HWND rejected. This is integration evidence, not the full release workload or 200% matrix.");
            }
            catch (Exception ex)
            {
                failure = ex;
                report.AppendLine(ex.ToString());
            }
            finally
            {
                widget?.Close();
                backdrop?.Close();
                File.WriteAllText(Path.Combine(folder, "production.txt"), report.ToString());
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromMinutes(performanceOnly ? 12 : 8)), "Production widget qualification timed out.");
        if (failure != null) throw new Xunit.Sdk.XunitException(failure.ToString());
    }

    private static void MeasureWorkloads(Window widget, Window backdrop, Action<string, double> apply, string folder, StringBuilder report)
    {
        using var process = Process.GetCurrentProcess();
        // The consumer facade hides this runtime-public event. Test instrumentation observes it without changing rendering.
        object renderer = ((IRenderRoot)widget).Renderer;
        EventInfo sceneEvent = renderer.GetType().GetEvent("SceneInvalidated", BindingFlags.Public | BindingFlags.Instance)!;
        var sceneCounter = new SceneCounter();
        Delegate sceneHandler = Delegate.CreateDelegate(
            sceneEvent.EventHandlerType!, sceneCounter, typeof(SceneCounter).GetMethod(nameof(SceneCounter.OnSceneInvalidated))!);
        sceneEvent.AddEventHandler(renderer, sceneHandler);
        void Pump(TimeSpan duration)
        {
            var watch = Stopwatch.StartNew();
            while (watch.Elapsed < duration)
            {
                Dispatcher.UIThread.RunJobs();
                Thread.Sleep(20);
            }
        }

        for (int i = 0; i < 5; i++)
        {
            apply(WidgetBlurModes.Off, 0.5);
            apply(WidgetBlurModes.Frosted, 0.5);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        Pump(TimeSpan.FromSeconds(3));
        var samples = new List<(string Mode, double Cpu, long PrivateBytes, int Scenes)>();
        for (int repeat = 0; repeat < 3; repeat++)
        foreach (string mode in new[] { WidgetBlurModes.Off, WidgetBlurModes.Frosted })
        {
            apply(mode, 0.5);
            Pump(TimeSpan.FromSeconds(2));
            report.AppendLine($"Idle sample {repeat + 1}, {mode}, process {process.Id}, scale {widget.RenderScaling} starting.");
            File.WriteAllText(Path.Combine(folder, "workloads.txt"), report.ToString());
            process.Refresh();
            TimeSpan cpuBefore = process.TotalProcessorTime;
            int scenesBefore = Volatile.Read(ref sceneCounter.Value);
            var watch = Stopwatch.StartNew();
            Pump(TimeSpan.FromSeconds(60));
            process.Refresh();
            double cpu = (process.TotalProcessorTime - cpuBefore).TotalMilliseconds /
                watch.Elapsed.TotalMilliseconds / Environment.ProcessorCount * 100;
            int scenes = Volatile.Read(ref sceneCounter.Value) - scenesBefore;
            samples.Add((mode, cpu, process.PrivateMemorySize64, scenes));
            report.AppendLine($"Idle sample {repeat + 1}, {mode}: normalized CPU {cpu:F4}%, private bytes {process.PrivateMemorySize64}, widget scene invalidations {scenes}.");
            File.WriteAllText(Path.Combine(folder, "workloads.txt"), report.ToString());
        }

        double MedianCpu(string mode) => samples.Where(s => s.Mode == mode).Select(s => s.Cpu).Order().ElementAt(1);
        long MedianMemory(string mode) => samples.Where(s => s.Mode == mode).Select(s => s.PrivateBytes).Order().ElementAt(1);
        double extraCpu = MedianCpu(WidgetBlurModes.Frosted) - MedianCpu(WidgetBlurModes.Off);
        long extraMemory = MedianMemory(WidgetBlurModes.Frosted) - MedianMemory(WidgetBlurModes.Off);
        Assert.True(extraCpu <= 1, $"Idle CPU regression: {extraCpu:F4} percentage points");
        Assert.True(extraMemory <= 25 * 1024 * 1024, $"Private bytes regression: {extraMemory}");
        Assert.True(samples.Where(s => s.Mode == WidgetBlurModes.Frosted).Max(s => s.Scenes) <=
            samples.Where(s => s.Mode == WidgetBlurModes.Off).Max(s => s.Scenes), "Frost introduced idle widget redraw.");
        report.AppendLine($"Idle deltas: CPU {extraCpu:F4} percentage points (budget <=1), private bytes {extraMemory} (budget <=26214400); no additional idle scene invalidations.");
        foreach (string mode in new[] { WidgetBlurModes.Off, WidgetBlurModes.Frosted })
        {
            apply(mode, 0.5);
            PixelPoint origin = widget.Position;
            process.Refresh();
            TimeSpan cpuBefore = process.TotalProcessorTime;
            var watch = Stopwatch.StartNew();
            double maximumIteration = 0;
            int frames = 0;
            while (watch.Elapsed < TimeSpan.FromSeconds(30))
            {
                var iteration = Stopwatch.StartNew();
                widget.Position = new PixelPoint(origin.X + (frames % 120), origin.Y + (frames % 60));
                if (frames % 10 == 0)
                {
                    backdrop.Content = new Border { Background = frames % 20 == 0 ? Brushes.White : Brushes.Black };
                }

                Dispatcher.UIThread.RunJobs();
                maximumIteration = Math.Max(maximumIteration, iteration.Elapsed.TotalMilliseconds);
                frames++;
                Thread.Sleep(16);
            }

            process.Refresh();
            double cpu = (process.TotalProcessorTime - cpuBefore).TotalMilliseconds /
                watch.Elapsed.TotalMilliseconds / Environment.ProcessorCount * 100;
            report.AppendLine($"Automated move workload {mode}, 30s: CPU {cpu:F4}%, private bytes {process.PrivateMemorySize64}, {frames} iterations, maximum UI iteration {maximumIteration:F3}ms (budget <=100ms).");
            Assert.True(maximumIteration <= 100, $"UI move workload stall: {maximumIteration:F3}ms");
            widget.Position = origin;
            Settle();
        }

        report.AppendLine("The move workload updates native window positions over changing content. Physical pointer dragging and presented-frame timing require the user walkthrough; this test does not claim those gates.");
        File.WriteAllText(Path.Combine(folder, "workloads.txt"), report.ToString());
        sceneEvent.RemoveEventHandler(renderer, sceneHandler);
    }

    private sealed class SceneCounter
    {
        public int Value;

        public void OnSceneInvalidated(object? sender, object? args) => Interlocked.Increment(ref this.Value);
    }

    private static void RunNativePrototype(bool dpiOnly)
    {
#if !FOCUSTIMER_BACKEND_PROTOTYPE
        if (dpiOnly) throw new InvalidOperationException("The DPI probe requires FocusTimerBackendPrototype=true.");
#endif
        if (!OperatingSystem.IsWindows()) return;
        string folder = Environment.GetEnvironmentVariable("FOCUSTIMER_EVIDENCE_DIR")
            ?? Path.Combine(AppContext.BaseDirectory, "widget-frost-prototype");
        Directory.CreateDirectory(folder);
        var report = new StringBuilder();
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            Window? backdrop = null;
            TimerWidgetWindow? widget = null;
#if FOCUSTIMER_BACKEND_PROTOTYPE
            Avalonia.Win32.Win32BackdropControllerV1? controller = null;
#endif
            try
            {
                AppBuilder.Configure<AppearanceNativeTests.AppearanceTestApp>().UsePlatformDetect().SetupWithoutStarting();
                SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
                RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;
                var manager = new ThemeManager();
                manager.InitializeThemeResources();
                manager.ApplyTheme(new ThemeService().GetBuiltInTheme("Dark")!);
                var provider = new SettingsProviderStub();
                var logger = new QuietLogger();
                var notifications = new LinuxNotificationServiceStub();
                var tracker = new SessionTracker(new LinuxActiveWindowServiceStub(), logger);
                using var timer = new TimerService(tracker);
                using var reminders = new BreakReminderService(notifications, provider);
                using var vm = new TimerWidgetViewModel(provider, logger, new CsvSessionRepository(provider),
                    notifications, tracker, reminders, timer, null, new EventBus());
                vm.Settings.WidgetScale = 3;
                vm.Settings.AlwaysOnTop = true;
                backdrop = new Window
                {
                    Width = 1200, Height = 600, Position = new PixelPoint(80, 80),
                    SystemDecorations = SystemDecorations.None, ShowInTaskbar = false, Topmost = true,
                };
                backdrop.Show();
                widget = new TimerWidgetWindow(manager, manageMaterial: false)
                {
                    DataContext = vm, Position = new PixelPoint(200, 160), IsAppShuttingDown = true,
                };
                PlaceOnRequestedDisplay(backdrop, widget);
                widget.Show();
                widget.Activate();
                Settle();
                object? platform = typeof(Window).GetProperty("PlatformImpl", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)?.GetValue(widget);
                object? surface = platform?.GetType().GetField("_glSurface", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(platform);
                report.AppendLine($"Native platform: {platform?.GetType().FullName}; graphics/composition surface: {surface?.GetType().FullName}; topmost: {widget.Topmost}");
                report.AppendLine($"OS: {Environment.OSVersion}; Avalonia: {typeof(Window).Assembly.GetName().Version}; DPI scale: {widget.RenderScaling}; widget: {widget.Bounds}; opacity: {widget.Opacity}");
                foreach (var screen in widget.Screens.All)
                    report.AppendLine($"Display: bounds {screen.Bounds}; working area {screen.WorkingArea}; scale {screen.Scaling}; primary {screen.IsPrimary}");
                File.WriteAllText(Path.Combine(folder, "environment.txt"), report.ToString());
                if (dpiOnly) report.AppendLine("DPI-only probe: edge/detail and foreground matrix are not repeated.");
                report.AppendLine("Screen feasibility gate only. Passing it still requires foreground, corners and fade qualification.");
                Border shell = widget.FindControl<Border>("WidgetShell")!;
#if FOCUSTIMER_BACKEND_PROTOTYPE
                bool attached = true;
                int completedUpdates = 0;
                Assert.True(Avalonia.Win32.Win32BackdropControllerV1.TryCreate(widget.TryGetPlatformHandle()!.Handle, out controller));
                void UpdateBackdrop()
                {
                    if (!attached) return;
                    Assert.True(controller!.TryUpdateWithCompletion((float)(shell.CornerRadius.TopLeft * widget.RenderScaling),
                        (float)widget.Opacity, widget.ActualTransparencyLevel == WindowTransparencyLevel.AcrylicBlur, applied =>
                        {
                            Assert.True(Dispatcher.UIThread.CheckAccess());
                            Assert.True(applied);
                            completedUpdates++;
                        }));
                }
                widget.PropertyChanged += (_, e) =>
                {
                    if (e.Property == Visual.OpacityProperty || e.Property == TopLevel.ActualTransparencyLevelProperty
                        || e.Property == Visual.BoundsProperty) UpdateBackdrop();
                };
                shell.PropertyChanged += (_, e) => { if (e.Property == Border.CornerRadiusProperty) UpdateBackdrop(); };
                widget.ScalingChanged += (_, _) => UpdateBackdrop();
                UpdateBackdrop();
                Settle();
                Assert.True(completedUpdates > 0, "The backend must report completed updates on the UI thread without polling.");
                report.AppendLine("Public per-window extension enabled: Win32BackdropControllerV1.");
#endif
                var candidatesPass = new Dictionary<WindowTransparencyLevel, bool>
                {
                    [WindowTransparencyLevel.Blur] = true,
                    [WindowTransparencyLevel.AcrylicBlur] = true,
                };
                backdrop.Content = BuildPattern(128);
                foreach (int stripeWidth in dpiOnly ? Array.Empty<int>() : new[] { 96, 128 })
                {
                    var pattern = new Canvas { Background = Brushes.White };
                    for (int x = 0; x < 1200; x += stripeWidth * 2)
                    {
                        pattern.Children.Add(new Rectangle
                        {
                            Width = stripeWidth, Height = 600, Fill = Brushes.Black,
                            [Canvas.LeftProperty] = (double)x,
                        });
                    }

                    backdrop.Content = pattern;
                    foreach (double tint in new[] { 0.0, 0.25, 0.5 })
                    {
                        widget.TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
                        shell.Background = new SolidColorBrush(Color.Parse("#202020"), tint);
                        Settle();
                        string id = $"stripes-{stripeWidth}-tint-{tint}";
                        Profile sharp = Capture(widget, folder, id + "-off", stripeWidth, backdrop);
                        double cornerOff = CornerLuma(widget);
                        report.AppendLine($"{id} Off: {sharp}");
                        Assert.True(sharp.Detail >= 40 && sharp.EdgeCount >= 4, "Off control lacks visible edges; environment unverified.");
                        PixelPoint controlOrigin = backdrop.Position;
                        backdrop.Position = new PixelPoint(controlOrigin.X + 16, controlOrigin.Y);
                        Settle();
                        Profile offMoved = Capture(widget, folder, id + "-off-moved", stripeWidth, backdrop);
                        Assert.InRange(MeasureShift(sharp.Scan, offMoved.Scan), 14, 18);
                        backdrop.Position = controlOrigin;
                        Settle();
                        report.AppendLine($"{id} Off tracks the 16px underlying-window movement.");

                        shell.Background = new SolidColorBrush(Color.Parse("#202020"));
                        Settle();
                        Profile solid = Capture(widget, folder, id + "-solid", stripeWidth, backdrop);
                        Assert.True(solid.Detail < 2, $"Solid control is obscured or contaminated: {solid}");

                        shell.Background = new SolidColorBrush(Color.Parse("#202020"), 0.85);
                        Settle();
                        Profile dim = Capture(widget, folder, id + "-opacity-only", stripeWidth, backdrop);
                        Assert.True(dim.Detail >= 10, "Opacity-only control must remain measurable.");
                        Assert.False(dim.Width >= sharp.Width * 2 && dim.Width >= sharp.Width + 2,
                            "Opacity-only negative control incorrectly qualifies as blur.");
                        report.AppendLine($"{id} opacity-only: {dim}");

                        foreach (WindowTransparencyLevel level in new[] { WindowTransparencyLevel.Blur, WindowTransparencyLevel.AcrylicBlur })
                        {
                            widget.TransparencyLevelHint = [level];
                            // Use the actual shell, overriding only its test tint after native state reporting.
                            shell.Background = new SolidColorBrush(Color.Parse("#202020"), tint);
                            Settle();
                            Profile candidate = Capture(widget, folder, id + "-" + level, stripeWidth, backdrop);
                            bool passes = widget.ActualTransparencyLevel == level && candidate.EdgeCount >= 4
                                && candidate.Detail >= 10 && candidate.Detail >= sharp.Detail * 0.1
                                && candidate.Width >= sharp.Width * 2 && candidate.Width >= sharp.Width + 2;
                            report.AppendLine($"{id} requested {level}, actual {widget.ActualTransparencyLevel}: {candidate}; initial gate {(passes ? "PASS" : "FAIL")}");
                            candidatesPass[level] &= passes;
                            if (passes)
                            {
                                widget.TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
                                shell.Background = new SolidColorBrush(Color.Parse("#202020"), 1 - candidate.Detail / 255);
                                Settle();
                                Profile matchedOpacity = Capture(widget, folder, id + "-matched-opacity", stripeWidth, backdrop);
                                Assert.InRange(Math.Abs(matchedOpacity.Detail - candidate.Detail), 0, 2);
                                Assert.False(matchedOpacity.Width >= sharp.Width * 2 && matchedOpacity.Width >= sharp.Width + 2,
                                    "Contrast-matched opacity-only control incorrectly qualifies as blur.");
                                report.AppendLine($"{id} contrast-matched opacity-only: {matchedOpacity}");
                                widget.TransparencyLevelHint = [level];
                                shell.Background = new SolidColorBrush(Color.Parse("#202020"), tint);
                                Settle();
                                if (stripeWidth == 96 && tint == 0)
                                {
                                    double cornerError = Math.Abs(CornerLuma(widget) - cornerOff);
                                    widget.Opacity = 0.5;
                                    Settle();
                                    Profile halfFaded = Capture(widget, folder, id + "-" + level + "-half-opacity", stripeWidth, backdrop);
                                    double fullDifference = sharp.Scan.Zip(candidate.Scan, (a, b) => Math.Abs(a - b)).Average();
                                    double halfDifference = sharp.Scan.Zip(halfFaded.Scan, (a, b) => Math.Abs(a - b)).Average();
                                    report.AppendLine($"{level}: half-opacity blur residual {halfDifference:F3}, full-opacity {fullDifference:F3} luma");
                                    Assert.InRange(halfDifference / fullDifference, 0.35, 0.65);
                                    widget.Opacity = 0;
                                    Settle();
                                    Profile faded = Capture(widget, folder, id + "-" + level + "-zero-opacity", stripeWidth, backdrop);
                                    double fadeError = sharp.Scan.Zip(faded.Scan, (a, b) => Math.Abs(a - b)).Average();
                                    widget.Opacity = 1;
                                    Settle();
                                    bool geometryPass = cornerError < 2 && fadeError < 2;
                                    candidatesPass[level] &= geometryPass;
                                    report.AppendLine($"{level}: outside rounded shell corner difference {cornerError:F3} luma; zero-opacity difference from clear desktop {fadeError:F3} luma; geometry/fade gate {(geometryPass ? "PASS" : "FAIL")}");
                                    Assert.True(geometryPass, "Avalonia blur does not preserve rounded shell clipping or whole-widget fade; backend qualification failed.");
#if FOCUSTIMER_BACKEND_PROTOTYPE
                                    Assert.True(controller!.IsApplied);
                                    CornerRadius originalRadius = shell.CornerRadius;
                                    shell.CornerRadius = new CornerRadius(60);
                                    Settle();
                                    Assert.InRange(Math.Abs(CornerLuma(widget) - cornerOff), 0, 2);
                                    shell.CornerRadius = new CornerRadius(0);
                                    Settle();
                                    double squareDifference = Math.Abs(CornerLuma(widget) - cornerOff);
                                    Assert.True(squareDifference >= 10, "Square clip must expose blur at the former rounded corner.");
                                    shell.CornerRadius = originalRadius;
                                    Settle();
                                    Assert.InRange(Math.Abs(CornerLuma(widget) - cornerOff), 0, 2);
                                    report.AppendLine($"Dynamic rounded/square/rounded clip passed; square corner difference {squareDifference:F3} luma.");
#endif
                                }

                                foreach (bool active in new[] { true, false })
                                {
                                    if (active) widget.Activate();
                                    else
                                    {
                                        backdrop.Activate();
                                        // Keep the active topmost pattern below the inactive widget without reactivating it.
                                        widget.Topmost = false;
                                        widget.Topmost = true;
                                        // Finish fixture z-order setup before measuring an independent movement.
                                        PixelPoint setupOrigin = backdrop.Position;
                                        backdrop.Position = new PixelPoint(setupOrigin.X + 1, setupOrigin.Y);
                                        Settle();
                                        backdrop.Position = setupOrigin;
                                    }
                                    Settle();
                                    Assert.Equal(active, widget.IsActive);
                                    string liveId = id + "-" + level + (active ? "-active" : "-inactive");
                                    Profile before = Capture(widget, folder, liveId + "-before", stripeWidth, backdrop);
                                    Assert.True(before.Detail >= 10 && before.EdgeCount >= 4,
                                        $"Live baseline lacks desktop detail: {before}");
                                    PixelPoint original = backdrop.Position;
                                    backdrop.Position = new PixelPoint(original.X + 16, original.Y);
                                    Settle();
                                    Profile moved = Capture(widget, folder, liveId + "-moved", stripeWidth, backdrop);
                                    int displacement = MeasureShift(before.Scan, moved.Scan);
                                    report.AppendLine($"{liveId}: before {before}; moved {moved}; measured displacement {displacement}px");
                                    Assert.InRange(displacement, 14, 18);
                                    backdrop.Position = original;
                                    Settle();
                                    Profile returned = Capture(widget, folder, liveId + "-returned", stripeWidth, backdrop);
                                    double returnError = before.Scan.Zip(returned.Scan, (a, b) => Math.Abs(a - b)).Average();
                                    Assert.True(returnError < 2, $"Backdrop did not return to its original pattern: {returnError}");
                                    report.AppendLine($"{liveId}: known movement 16px, measured {displacement}px, return error {returnError:F3} luma");
                                }

                                backdrop.Content = new Border { Background = Brushes.Black };
                                Settle();
                                Profile dark = Capture(widget, folder, id + "-" + level + "-dark-window", stripeWidth, backdrop);
                                if (stripeWidth == 96 && tint == 0)
                                {
                                    byte[] frostedForeground = CaptureForeground(widget);
                                    widget.TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
                                    Settle();
                                    byte[] clearForeground = CaptureForeground(widget);
                                    var brightPixels = Enumerable.Range(0, clearForeground.Length / 4)
                                        .Where(p => clearForeground[p * 4] > 180 && clearForeground[p * 4 + 1] > 180
                                            && clearForeground[p * 4 + 2] > 180).ToArray();
                                    Assert.True(brightPixels.Length >= 500, "Foreground control lacks bright clock pixels.");
                                    double glyphError = brightPixels.Average(p => Enumerable.Range(0, 3)
                                        .Average(c => Math.Abs(clearForeground[p * 4 + c] - frostedForeground[p * 4 + c])));
                                    Assert.InRange(glyphError, 0, 2);
                                    // Include glyph boundaries: a blurred glyph would change dark neighboring pixels too.
                                    double foregroundError = clearForeground.Zip(frostedForeground, (a, b) => Math.Abs(a - b)).Average();
                                    Assert.InRange(foregroundError, 0, 2);
                                    report.AppendLine($"Foreground clock crop: {brightPixels.Length} bright pixels, glyph error {glyphError:F3}, complete crop error {foregroundError:F3} channel levels.");
                                    widget.TransparencyLevelHint = [level];
                                    Settle();
                                }
                                backdrop.Content = new Border { Background = Brushes.White };
                                Settle();
                                Profile light = Capture(widget, folder, id + "-" + level + "-light-window", stripeWidth, backdrop);
                                double change = light.Scan.Average() - dark.Scan.Average();
                                Assert.True(change >= 10 && change >= sharp.Detail * 0.1,
                                    $"Replacement window content must remain visible: {change}");
                                report.AppendLine($"{id}-{level}: replacing dark/light underlying content changed mean luma {change:F3}");
                                backdrop.Content = pattern;
                                widget.Activate();
                                Settle();
                            }
                        }
                    }
                }

                if (!dpiOnly) Assert.True(candidatesPass.Values.Any(v => v), "Neither Avalonia candidate passed the prototype gates. See prototype.txt; no release qualification claimed.");
#if FOCUSTIMER_BACKEND_PROTOTYPE
                foreach (double scale in dpiOnly ? Array.Empty<double>() : new[] { 0.5, 1.0, 3.0 })
                foreach (bool compact in new[] { false, true })
                foreach (bool below in new[] { false, true })
                {
                    vm.Settings.WidgetScale = scale;
                    vm.Settings.UseCompactMode = compact;
                    vm.Settings.WidgetButtonsBelowClock = below;
                    widget.TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
                    shell.Background = Brushes.Transparent;
                    Settle();
                    double clearCorner = CornerLuma(widget, outermost: true);
                    widget.TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur];
                    Settle();
                    Assert.True(controller!.IsApplied);
                    double cornerDifference = Math.Abs(CornerLuma(widget, outermost: true) - clearCorner);
                    Assert.InRange(cornerDifference, 0, 2);
                    PixelPoint origin = widget.PointToScreen(default);
                    int width = (int)Math.Ceiling(widget.Bounds.Width * widget.RenderScaling);
                    int height = (int)Math.Ceiling(widget.Bounds.Height * widget.RenderScaling);
                    string geometryId = $"geometry-{scale}-{compact}-{below}";
                    ScreenCapture.SavePng(ScreenCapture.Capture(origin.X, origin.Y, width, height), width, height,
                        Path.Combine(folder, geometryId + ".png"));
                    report.AppendLine($"{geometryId}: bounds {widget.Bounds}, radius {shell.CornerRadius}, display scale {widget.RenderScaling}, outside corner difference {cornerDifference:F3} luma.");
                }
                vm.Settings.WidgetScale = 3;
                vm.Settings.UseCompactMode = false;
                vm.Settings.WidgetButtonsBelowClock = false;
                shell.Background = new SolidColorBrush(Color.Parse("#202020"), 0.5);
                Settle();
                var otherScaleScreen = widget.Screens.All.FirstOrDefault(s => Math.Abs(s.Scaling - widget.RenderScaling) > 0.01);
                if (otherScaleScreen != null)
                {
                    object? originalContent = backdrop.Content;
                    PixelPoint widgetOrigin = widget.Position;
                    PixelPoint backdropOrigin = backdrop.Position;
                    double startingScale = widget.RenderScaling;
                    IntPtr startingHandle = widget.TryGetPlatformHandle()!.Handle;
                    int dpiEvents = 0;
                    widget.ScalingChanged += (_, _) => dpiEvents++;
                    // Use a compact 1x window so both monitor working areas can contain the native capture.
                    vm.Settings.WidgetScale = 1;
                    vm.Settings.UseCompactMode = true;
                    Settle();
                    foreach (bool moveBack in new[] { false, true })
                    {
                        PixelRect area = otherScaleScreen.WorkingArea;
                        backdrop.Position = moveBack ? backdropOrigin : new PixelPoint(area.X + 20, area.Y + 20);
                        widget.Position = moveBack ? widgetOrigin : new PixelPoint(area.X + 100, area.Y + 100);
                        widget.Activate();
                        Settle();
                        double expectedScale = moveBack ? startingScale : otherScaleScreen.Scaling;
                        Assert.InRange(Math.Abs(widget.RenderScaling - expectedScale), 0, 0.01);
                        Assert.Equal(startingHandle, widget.TryGetPlatformHandle()!.Handle);
                        Assert.True(controller!.IsApplied);
                        widget.TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
                        shell.Background = Brushes.Transparent;
                        Settle();
                        double clearCorner = CornerLuma(widget, outermost: true);
                        widget.TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur];
                        Settle();
                        Assert.True(controller.IsApplied);
                        double cornerError = Math.Abs(CornerLuma(widget, outermost: true) - clearCorner);
                        Assert.InRange(cornerError, 0, 2);
                        Assert.Equal(WindowTransparencyLevel.AcrylicBlur, widget.ActualTransparencyLevel);
                        double InteriorLuma()
                        {
                            PixelPoint sample = widget.PointToScreen(new Point(shell.CornerRadius.TopLeft + 2, 2));
                            byte[] pixels = ScreenCapture.Capture(sample.X, sample.Y, 1, 1);
                            return pixels[2] * 0.299 + pixels[1] * 0.587 + pixels[0] * 0.114;
                        }
                        backdrop.Content = new Border { Background = Brushes.Black };
                        Settle();
                        double darkLuma = InteriorLuma();
                        backdrop.Content = new Border { Background = Brushes.White };
                        Settle();
                        double liveDifference = InteriorLuma() - darkLuma;
                        Assert.True(liveDifference >= 10, "Desktop content must remain visible after a DPI transition.");
                        backdrop.Content = originalContent;
                        Settle();
                        // Fixed physical (2,3) lies inside an 8px clip and outside a 12px clip.
                        // Remove the foreground outline only for this native clip calibration.
                        Thickness outline = shell.BorderThickness;
                        PixelPoint patternOrigin = backdrop.Position;
                        shell.BorderThickness = default;
                        // Put the probe on the dark side; the narrow bright-side sample saturated near white.
                        backdrop.Position = new PixelPoint(widget.PointToScreen(default).X + 4 - (int)(128 * backdrop.RenderScaling), patternOrigin.Y);
                        widget.TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
                        Settle();
                        double ProbeLuma()
                        {
                            PixelPoint origin = widget.PointToScreen(default);
                            byte[] pixels = ScreenCapture.Capture(origin.X + 2, origin.Y + 3, 1, 1);
                            return pixels[2] * 0.299 + pixels[1] * 0.587 + pixels[0] * 0.114;
                        }
                        double clearProbe = ProbeLuma();
                        Assert.InRange(clearProbe, 0, 2);
                        PixelPoint probeOrigin = widget.PointToScreen(default);
                        ScreenCapture.SavePng(ScreenCapture.Capture(probeOrigin.X, probeOrigin.Y, 24, 24),
                            24, 24, Path.Combine(folder, $"dpi-clip-{expectedScale}-off.png"));
                        widget.TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur];
                        Settle();
                        double clipProbeDifference = Math.Abs(ProbeLuma() - clearProbe);
                        ScreenCapture.SavePng(ScreenCapture.Capture(probeOrigin.X, probeOrigin.Y, 24, 24),
                            24, 24, Path.Combine(folder, $"dpi-clip-{expectedScale}-frost.png"));
                        report.AppendLine($"DPI clip calibration at physical (2,3): display scale {expectedScale}, clear luma {clearProbe:F3}, difference from Off {clipProbeDifference:F3} luma.");
                        report.AppendLine($"Clip probe origins: widget requested {widget.Position}, actual {widget.PointToScreen(default)}; pattern requested {backdrop.Position}, actual {backdrop.PointToScreen(default)}, scale {backdrop.RenderScaling}; controller applied {controller.IsApplied}.");
                        if (Math.Abs(expectedScale - 1) < 0.01) Assert.True(clipProbeDifference >= 10,
                            "At 100% the physical clip probe must be inside the 8px rounded backdrop.");
                        else if (expectedScale >= 1.5) Assert.InRange(clipProbeDifference, 0, 2);
                        shell.BorderThickness = outline;
                        backdrop.Position = patternOrigin;
                        Settle();
                        PixelPoint frameOrigin = widget.PointToScreen(default);
                        int frameWidth = (int)Math.Ceiling(widget.Bounds.Width * widget.RenderScaling);
                        int frameHeight = (int)Math.Ceiling(widget.Bounds.Height * widget.RenderScaling);
                        ScreenCapture.SavePng(ScreenCapture.Capture(frameOrigin.X, frameOrigin.Y, frameWidth, frameHeight),
                            frameWidth, frameHeight, Path.Combine(folder, $"dpi-transition-{widget.RenderScaling}.png"));
                        report.AppendLine($"Live DPI transition: expected {expectedScale}, actual {widget.RenderScaling}, same HWND, {dpiEvents} scale events, physical clip radius {shell.CornerRadius.TopLeft * widget.RenderScaling}, outside corner error {cornerError:F3}, live desktop change {liveDifference:F3} luma.");
                    }
                    Assert.True(dpiEvents >= 2, "Expected real DPI events on both cross-monitor transitions.");
                    vm.Settings.WidgetScale = 3;
                    vm.Settings.UseCompactMode = false;
                    shell.Background = new SolidColorBrush(Color.Parse("#202020"), 0.5);
                    Settle();
                }
                else
                {
                    report.AppendLine("Live DPI transition UNVERIFIED: no monitor with a different display scale was available.");
                    Assert.False(dpiOnly, "DPI-only qualification requires monitors with different scales.");
                }
                for (int cycle = 0; cycle < 3; cycle++)
                {
                    widget.Hide();
                    Settle();
                    widget.Show();
                    widget.Activate();
                    UpdateBackdrop();
                    Settle();
                    Assert.True(controller!.IsApplied);
                    Profile restored = Capture(widget, folder, $"hide-show-{cycle}", 128, backdrop);
                    Assert.True(restored.Detail >= 10 && restored.Width >= 2.8);
                }
                IntPtr oldHandle = widget.TryGetPlatformHandle()!.Handle;
                attached = false;
                widget.Close();
                Settle();
                Assert.False(controller!.TryUpdate(36, 1, true));
                Assert.False(Avalonia.Win32.Win32BackdropControllerV1.TryCreate(oldHandle, out _));
                controller.Dispose();
                Assert.False(controller.IsApplied);
                widget = new TimerWidgetWindow(manager)
                {
                    Width = 909, Height = 182, Position = new PixelPoint(200, 160),
                    Topmost = true, IsAppShuttingDown = true,
                    TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur],
                };
                widget.Show();
                Settle();
                Assert.True(Avalonia.Win32.Win32BackdropControllerV1.TryCreate(widget.TryGetPlatformHandle()!.Handle, out controller));
                Assert.True(controller!.TryUpdate(36, 1, true));
                Settle();
                Assert.True(controller.IsApplied);
                controller.Dispose();
                Assert.False(controller.TryUpdate(36, 1, true));
                report.AppendLine("Three hide/show cycles, closed-handle rejection, new widget attachment and disposed-controller rejection passed. New widget has no ViewModel; this checks backend lifetime only.");
#endif
            }
            catch (Exception ex)
            {
                failure = ex;
                report.AppendLine(ex.ToString());
            }
            finally
            {
#if FOCUSTIMER_BACKEND_PROTOTYPE
                controller?.Dispose();
#endif
                widget?.Close();
                backdrop?.Close();
                File.WriteAllText(Path.Combine(folder, "prototype.txt"), report.ToString());
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(180)), "Widget prototype timed out.");
        if (failure != null) throw new Xunit.Sdk.XunitException(failure.ToString());
    }

    private static void PlaceOnRequestedDisplay(Window backdrop, Window widget)
    {
        string? requested = Environment.GetEnvironmentVariable("FOCUSTIMER_TEST_DISPLAY_SCALE");
        if (requested == null) return;
        double scale = double.Parse(requested, CultureInfo.InvariantCulture);
        var screen = widget.Screens.All.SingleOrDefault(s => Math.Abs(s.Scaling - scale) < 0.01);
        Assert.NotNull(screen);
        backdrop.Position = new PixelPoint(screen.Bounds.X + 80, screen.Bounds.Y + 80);
        widget.Position = new PixelPoint(screen.Bounds.X + 200, screen.Bounds.Y + 160);
        Settle();
    }

    private static Canvas BuildPattern(int stripeWidth)
    {
        var pattern = new Canvas { Background = Brushes.White };
        for (int x = 0; x < 1200; x += stripeWidth * 2)
            pattern.Children.Add(new Rectangle
            {
                Width = stripeWidth, Height = 600, Fill = Brushes.Black,
                [Canvas.LeftProperty] = (double)x,
            });
        return pattern;
    }

    private static void Settle()
    {
        for (int i = 0; i < 30; i++)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(20);
        }
    }

    private static Profile Capture(Window widget, string folder, string name, int stripeWidth, Window backdrop)
    {
        PixelPoint frameOrigin = widget.PointToScreen(default);
        int frameWidth = (int)Math.Ceiling(widget.Bounds.Width * widget.RenderScaling);
        int frameHeight = (int)Math.Ceiling(widget.Bounds.Height * widget.RenderScaling);
        ScreenCapture.SavePng(ScreenCapture.Capture(frameOrigin.X, frameOrigin.Y, frameWidth, frameHeight),
            frameWidth, frameHeight, Path.Combine(folder, name + "-widget.png"));
        // Top padding of the real full-mode widget at 3x: no clock or controls in this strip.
        PixelPoint start = widget.PointToScreen(new Point(40, 5));
        int width = (int)((widget.Bounds.Width - 80) * widget.RenderScaling);
        int height = Math.Max(1, (int)(3 * widget.RenderScaling));
        byte[] pixels = ScreenCapture.Capture(start.X, start.Y, width, height);
        ScreenCapture.SavePng(pixels, width, height, Path.Combine(folder, name + ".png"));
        var scan = new double[width];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int i = (y * width + x) * 4;
                scan[x] += (pixels[i + 2] * 0.299 + pixels[i + 1] * 0.587 + pixels[i] * 0.114) / height;
            }
        }

        File.WriteAllLines(Path.Combine(folder, name + ".csv"), scan.Select((v, x) => FormattableString.Invariant($"{start.X + x},{v:F3}")));
        double pitch = stripeWidth * backdrop.RenderScaling;
        int origin = backdrop.PointToScreen(default).X;
        var widths = new List<double>();
        var details = new List<double>();
        for (double edge = origin + pitch; edge < start.X + width; edge += pitch)
        {
            int left = (int)Math.Round(edge - start.X - pitch / 2);
            int right = (int)Math.Round(edge - start.X + pitch / 2);
            if (left < 0 || right >= width) continue;
            double a = scan.Skip(left).Take(3).Average();
            double b = scan.Skip(right - 2).Take(3).Average();
            double detail = Math.Abs(b - a);
            details.Add(detail);
            if (detail < 10) continue;
            double Crossing(double threshold)
            {
                for (int x = left; x < right; x++)
                {
                    double n0 = (scan[x] - a) / (b - a);
                    double n1 = (scan[x + 1] - a) / (b - a);
                    if (n0 <= threshold && n1 > threshold)
                        return x + (threshold - n0) / (n1 - n0);
                }

                return double.NaN;
            }

            double transition = Crossing(0.9) - Crossing(0.1);
            if (double.IsFinite(transition)) widths.Add(transition);
        }

        // A partly obscured strip cannot be normalized into valid evidence. All complete pattern edges must agree.
        if (details.Count >= 4 && details.Max() >= 10)
            Assert.True(details.Min() >= details.Max() * 0.6,
                $"Pattern plateau separation varies across the capture ({details.Min():F1}..{details.Max():F1}); environment unverified: {name}");
        double Median(List<double> values) => values.Count == 0 ? 0 : values.Order().ElementAt(values.Count / 2);
        return new Profile(Median(details), Median(widths), widths.Count, scan);
    }

    private static int MeasureShift(double[] before, double[] after)
    {
        double Mean(double[] values) => values.Average();
        double m0 = Mean(before);
        double m1 = Mean(after);
        double s0 = Math.Sqrt(before.Average(v => Math.Pow(v - m0, 2)));
        double s1 = Math.Sqrt(after.Average(v => Math.Pow(v - m1, 2)));
        Assert.True(s0 >= 5 && s1 >= 5, "Moving-window profiles lack visible detail.");
        return Enumerable.Range(-32, 65).MinBy(offset =>
            Enumerable.Range(32, before.Length - 64).Average(x =>
                Math.Pow((before[x] - m0) / s0 - (after[x + offset] - m1) / s1, 2)));
    }

    private static double CornerLuma(Window widget, bool outermost = false)
    {
        // This 3x full-mode shell has a 36px radius; (8,8)..(9,9) lies outside its rounded fill.
        PixelPoint point = widget.PointToScreen(outermost ? default : new Point(8, 8));
        int size = outermost ? 1 : 2;
        byte[] pixels = ScreenCapture.Capture(point.X, point.Y, size, size);
        return Enumerable.Range(0, size * size).Average(p =>
            pixels[p * 4 + 2] * 0.299 + pixels[p * 4 + 1] * 0.587 + pixels[p * 4] * 0.114);
    }

    private static byte[] CaptureForeground(Window widget)
    {
        PixelPoint point = widget.PointToScreen(new Point(30, 40));
        return ScreenCapture.Capture(point.X, point.Y, (int)(510 * widget.RenderScaling), (int)(100 * widget.RenderScaling));
    }

    private readonly record struct Profile(double Detail, double Width, int EdgeCount, double[] Scan)
    {
        public override string ToString() => FormattableString.Invariant($"detail {Detail:F3}, normalized 10%-90% width {Width:F3}px, valid edges {EdgeCount}");
    }
}
