namespace FocusTimer.App.Tests;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FocusTimer.App.Services;
using FocusTimer.App.ViewModels;
using FocusTimer.App.Views;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;
using FocusTimer.Core.Stubs;
using FocusTimer.Persistence;
using ReactiveUI;

/// <summary>
/// Renders the timer widget in full mode (project row open) and compact mode with the real Windows Skia backend
/// and stores the captures plus the measured sizes. Run separately: FOCUSTIMER_NATIVE_APPEARANCE_TESTS=1.
/// </summary>
[Collection("Native appearance")]
public class DesktopWidgetEvidenceTests
{
    [AppearanceNativeTests.NativeAppearanceFact]
    public void Widget_RendersFullAndCompactModes()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string folder = Environment.GetEnvironmentVariable("FOCUSTIMER_EVIDENCE_DIR")
            ?? Path.Combine(AppContext.BaseDirectory, "desktop-evidence");
        Directory.CreateDirectory(folder);
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                AppBuilder.Configure<AppearanceNativeTests.AppearanceTestApp>().UsePlatformDetect().SetupWithoutStarting();
                SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
                RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;
                var manager = new ThemeManager();
                manager.InitializeThemeResources();
                string themeName = Environment.GetEnvironmentVariable("FOCUSTIMER_EVIDENCE_THEME") ?? "Dark";
                manager.ApplyTheme(new ThemeService().GetBuiltInTheme(themeName)!);

                var provider = new SettingsProviderStub();
                var logger = new QuietLogger();
                var notifications = new LinuxNotificationServiceStub();
                var tracker = new SessionTracker(new LinuxActiveWindowServiceStub(), logger);
                using var timer = new TimerService(tracker);
                using var reminders = new BreakReminderService(notifications, provider);
                using var vm = new TimerWidgetViewModel(
                    provider, logger, new CsvSessionRepository(provider), notifications, tracker, reminders, timer, null, new EventBus());

                var report = new List<string>();
                double[] scales = [0.5, 0.6, 0.7, 0.8, 0.9, 1.0, 1.1, 1.2, 1.3, 1.5, 2.0, 2.5, 3.0];
                foreach ((bool compact, double scale) in scales.SelectMany(scale => new[] { (false, scale), (true, scale) }))
                {
                    vm.Settings.WidgetScale = scale;
                    vm.Settings.UseCompactMode = compact;
                    vm.IsProjectInputVisible = false;
                    vm.ProjectTag = string.Empty;
                    var window = new TimerWidgetWindow(manager) { DataContext = vm, ShowActivated = false, Position = new PixelPoint(-3000, -3000) };
                    window.Show();
                    Settle();
                    Assert.False(window.ExtendClientAreaToDecorationsHint);
                    Assert.False(window.IsExtendedIntoWindowDecorations);
                    string mode = $"{(compact ? "compact" : "full")}-{scale:0.##}";
                    Border shell = window.FindControl<Border>("WidgetShell")!;
                    Assert.Equal(window.Bounds.Size, shell.Bounds.Size);
                    Assert.Equal(default, shell.TranslatePoint(default, window)!.Value);
                    Assert.Equal(new CornerRadius((compact ? 8 : 12) * scale), shell.CornerRadius);
                    if (compact)
                    {
                        var view = window.GetVisualDescendants().OfType<CompactModeView>().Single();
                        var panel = view.FindControl<StackPanel>("ControlsLayer")!;
                        Assert.Equal(Avalonia.Layout.Orientation.Vertical, panel.Orientation);
                        var clock = view.FindControl<Grid>("ClockLayer")!;
                        double topGap = clock.TranslatePoint(default, window)!.Value.Y;
                        double bottomGap = window.Bounds.Height - topGap - clock.Bounds.Height;
                        Assert.InRange(Math.Abs(topGap - bottomGap), 0, 1.1);
                        Assert.True(window.Bounds.Height - clock.Bounds.Height <= 1.1,
                            $"Compact {scale}: window {window.Bounds.Height}, clock {clock.Bounds.Height}.");
                        var icon = panel.GetVisualDescendants().OfType<Material.Icons.Avalonia.MaterialIcon>().First();
                        var icons = panel.GetVisualDescendants().OfType<Material.Icons.Avalonia.MaterialIcon>().ToArray();
                        double iconGap = icons[1].TranslatePoint(default, window)!.Value.Y
                            - icons[0].TranslatePoint(default, window)!.Value.Y - icons[0].Bounds.Height;
                        Assert.InRange(iconGap, -0.1, 1.1);
                        var buttons = panel.Children.OfType<Button>().ToArray();
                        Assert.All(buttons, button => Assert.InRange(Math.Abs(button.Bounds.Height - vm.CompactIconSize), 0, 1.1));
                        Assert.All(buttons, button =>
                        {
                            double x = button.TranslatePoint(default, window)!.Value.X;
                            Assert.True(x >= 0 && x + button.Bounds.Width <= window.Bounds.Width + 1.1);
                        });
                        Assert.True(buttons[1].Bounds.Top >= buttons[0].Bounds.Bottom);
                        double leftGap = clock.TranslatePoint(default, window)!.Value.X;
                        double clockToIcon = icon.TranslatePoint(default, window)!.Value.X - leftGap - clock.Bounds.Width;
                        Assert.InRange(Math.Abs(clockToIcon - (3 * scale)), 0, 1.1);
                        double rightGap = window.Bounds.Width - icon.TranslatePoint(default, window)!.Value.X - icon.Bounds.Width;
                        Assert.InRange(Math.Abs(leftGap - rightGap), 0, 1.1);
                    }
                    else
                    {
                        var view = window.GetVisualDescendants().OfType<FullModeView>().Single();
                        var panel = view.FindControl<Grid>("ControlsLayer")!;
                        var clock = view.FindControl<Grid>("ClockLayer")!;
                        Assert.InRange(Math.Abs(clock.Bounds.Width - vm.MainTimerTextWidth), 0, 1.1);
                        Assert.InRange(Math.Abs(panel.Bounds.Width - (4 * (vm.ButtonSize + (4 * scale)))), 0, 4.1);
                        Assert.InRange(Math.Abs((clock.Bounds.Width / panel.Bounds.Width) - (175.0 / 112)), 0, 0.05);
                        var icons = panel.GetVisualDescendants().OfType<Material.Icons.Avalonia.MaterialIcon>().ToArray();
                        var buttons = panel.Children.OfType<Button>().ToArray();
                        for (int i = 1; i < icons.Length; i++)
                        {
                            double gap = icons[i].TranslatePoint(default, window)!.Value.X
                                - icons[i - 1].TranslatePoint(default, window)!.Value.X - icons[i - 1].Bounds.Width;
                            double expected = 10 * scale;
                            Assert.True(Math.Abs(gap - expected) <= 1.1, $"Full scale {scale}: icon gap {gap}, expected {expected}; icon width {icons[i - 1].Bounds.Width}, button size {vm.ButtonSize}.");
                            Assert.InRange(Math.Abs(icons[i].Bounds.Width - vm.IconSize), 0, 1.1);
                        }
                    }
                    report.Add($"{mode}: window {window.Bounds.Width:F0}x{window.Bounds.Height:F0}");
                    foreach (Button button in window.GetVisualDescendants().OfType<Button>().Where(b => b.IsEffectivelyVisible))
                    {
                        report.Add($"  button {button.Bounds.Width:F0}x{button.Bounds.Height:F0} at {button.TranslatePoint(default, window)}");
                    }

                    Capture(window, Path.Combine(folder, $"widget-{mode}.png"));
                    vm.Settings.WidgetScale = scale + 0.1;
                    Settle();
                    Assert.Equal(window.Bounds.Size, shell.Bounds.Size);
                    Assert.Equal(new CornerRadius((compact ? 8 : 12) * vm.Settings.WidgetScale), shell.CornerRadius);
                    Assert.InRange(Math.Abs(window.Bounds.Height - window.DesiredSize.Height), 0, 1.1);
                    vm.Settings.UseCompactMode = !compact;
                    Settle();
                    Assert.Equal(new CornerRadius((compact ? 12 : 8) * vm.Settings.WidgetScale), shell.CornerRadius);
                    Assert.InRange(Math.Abs(window.Bounds.Height - window.DesiredSize.Height), 0, 1.1);
                    Assert.Equal(window.Bounds.Size, shell.Bounds.Size);
                    vm.Settings.UseCompactMode = compact;
                    Settle();
                    window.Close();
                }

                File.WriteAllLines(Path.Combine(folder, "widget-sizes.txt"), report);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(120)), "Widget capture timed out.");
        if (failure != null)
        {
            throw new Xunit.Sdk.XunitException(failure.ToString());
        }
    }

    private static void Settle()
    {
        for (int i = 0; i < 8; i++)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(15);
        }
    }

    private static void Capture(Window window, string path)
    {
        window.UpdateLayout();
        using var image = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(window.Bounds.Width), (int)Math.Ceiling(window.Bounds.Height)));
        image.Render(window);
        image.Save(path);
    }
}
