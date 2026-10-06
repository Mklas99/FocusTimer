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
                manager.ApplyTheme(new ThemeService().BuiltInThemes.First(t => t.ThemeName == "Dark"));

                var provider = new SettingsProviderStub();
                var logger = new QuietLogger();
                var notifications = new LinuxNotificationServiceStub();
                var tracker = new SessionTracker(new LinuxActiveWindowServiceStub(), logger);
                using var timer = new TimerService(tracker);
                using var reminders = new BreakReminderService(notifications, provider);
                using var vm = new TimerWidgetViewModel(
                    provider, logger, new CsvSessionRepository(provider), notifications, tracker, reminders, timer, null, new EventBus());

                var report = new List<string>();
                foreach ((bool compact, double scale) in new[] { (false, 1.0), (true, 1.0), (true, 1.5) })
                {
                    vm.Settings.WidgetScale = scale;
                    vm.Settings.UseCompactMode = compact;
                    vm.IsProjectInputVisible = !compact;
                    vm.ProjectTag = string.Empty;
                    var window = new TimerWidgetWindow(manager) { DataContext = vm, ShowActivated = false, Position = new PixelPoint(-3000, -3000) };
                    window.Show();
                    Settle();
                    string mode = compact ? $"compact-{scale:0.0}" : "full";
                    report.Add($"{mode}: window {window.Bounds.Width:F0}x{window.Bounds.Height:F0}");
                    foreach (Button button in window.GetVisualDescendants().OfType<Button>().Where(b => b.IsEffectivelyVisible))
                    {
                        report.Add($"  button {button.Bounds.Width:F0}x{button.Bounds.Height:F0} at {button.TranslatePoint(default, window)}");
                    }

                    Capture(window, Path.Combine(folder, $"widget-{mode}.png"));
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
