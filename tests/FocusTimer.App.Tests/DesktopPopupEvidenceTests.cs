namespace FocusTimer.App.Tests;

using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FocusTimer.App.Services;
using FocusTimer.App.ViewModels;
using FocusTimer.App.Views;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;
using ReactiveUI;

/// <summary>
/// Opens the popups of the desktop windows (date-picker calendar, dropdown list) and captures the pixels on
/// screen, because popups are separate native windows that an offscreen render does not include.
/// Run separately with FOCUSTIMER_NATIVE_APPEARANCE_TESTS=1; windows appear on screen for a few seconds.
/// </summary>
[Collection("Native appearance")]
public class DesktopPopupEvidenceTests
{
    [AppearanceNativeTests.NativeAppearanceFact]
    public void Popups_AreCapturedOnScreen()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string folder = Environment.GetEnvironmentVariable("FOCUSTIMER_EVIDENCE_DIR")
            ?? Path.Combine(AppContext.BaseDirectory, "desktop-evidence");
        Directory.CreateDirectory(folder);
        string[] themes = (Environment.GetEnvironmentVariable("FOCUSTIMER_EVIDENCE_THEMES") ?? "Dark")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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
                var themeService = new ThemeService();
                foreach (string name in themes)
                {
                    manager.ApplyTheme(themeService.BuiltInThemes.First(t => t.ThemeName == name));
                    CaptureCalendar(name, folder);
                    CaptureDropdown(name, folder);
                }
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(120)), "Popup capture timed out.");
        if (failure != null)
        {
            throw new Xunit.Sdk.XunitException(failure.ToString());
        }
    }

    private static void CaptureCalendar(string theme, string folder)
    {
        var clock = new MutableClock(new DateTimeOffset(2026, 5, 4, 12, 0, 0, TimeSpan.Zero));
        var store = new MemoryWorklogStore();
        var resolver = new RuleProjectResolver(new ProjectRuleStore());
        var groupings = new WorklogGroupingRegistry([new ApplicationGrouping(), new ProjectGrouping(), new WindowGrouping()]);
        var viewModel = new WorklogWindowViewModel(
            new WorklogEntriesViewModel(store, clock, null, resolver),
            new WorklogSummaryViewModel(new WorklogSummaryService(store, groupings, resolver, new QuietLogger()), groupings, clock),
            new SettingsStub(),
            clock,
            null,
            groupings,
            resolver);
        var window = new WorklogWindow { DataContext = viewModel, Position = new PixelPoint(120, 80) };
        window.Show();
        window.Activate();
        Settle();
        CalendarDatePicker picker = window.GetVisualDescendants().OfType<CalendarDatePicker>().First();
        picker.IsDropDownOpen = true;
        Settle();
        SaveScreen(window, -40, 50, 640, 720, Path.Combine(folder, $"popup-calendar-{theme}.png"));
        if (Environment.GetEnvironmentVariable("FOCUSTIMER_EVIDENCE_DUMP") == "1")
        {
            DumpPopupTree(picker, Path.Combine(folder, $"popup-calendar-{theme}.txt"));
        }

        picker.IsDropDownOpen = false;
        window.Close();
    }

    private static void DumpPopupTree(CalendarDatePicker picker, string path)
    {
        var lines = new List<string>();
        Avalonia.Controls.Primitives.Popup? popup = picker.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.Popup>().FirstOrDefault();
        if (popup?.Child is Visual root)
        {
            foreach (Visual v in new[] { root }.Concat(root.GetVisualDescendants()))
            {
                string name = (v as Control)?.Name ?? string.Empty;
                string classes = v is StyledElement el ? string.Join(' ', el.Classes) : string.Empty;
                lines.Add($"{new string(' ', v.GetVisualAncestors().Count())}{v.GetType().Name} #{name} .{classes} {v.Bounds.Width:F0}x{v.Bounds.Height:F0}");
            }
        }

        lines.Add("--- picker");
        foreach (Visual v in picker.GetVisualDescendants())
        {
            lines.Add($"{new string(' ', v.GetVisualAncestors().Count())}{v.GetType().Name} #{(v as Control)?.Name} {v.Bounds.Width:F0}x{v.Bounds.Height:F0}");
        }

        File.WriteAllLines(path, lines);
    }

    private static void CaptureDropdown(string theme, string folder)
    {
        SettingsWindowViewModel editor = SettingsWindowViewModelTests.CreateAppearanceEditor();
        editor.SelectedTabIndex = 2;
        var window = new SettingsWindow { DataContext = editor, Position = new PixelPoint(120, 80) };
        window.Show();
        window.Activate();
        Settle();
        ComboBox combo = window.GetVisualDescendants().OfType<ComboBox>().First();
        combo.IsDropDownOpen = true;
        Settle();
        SaveScreen(window, 0, 0, 640, 540, Path.Combine(folder, $"popup-dropdown-{theme}.png"));
        combo.IsDropDownOpen = false;
        window.Close();
    }

    private static void SaveScreen(Window window, int x, int y, int width, int height, string path)
    {
        PixelPoint origin = window.PointToScreen(new Point(x, y));
        byte[] pixels = ScreenCapture.Capture(origin.X, origin.Y, width, height);
        ScreenCapture.SavePng(pixels, width, height, path);
    }

    private static void Settle()
    {
        var until = DateTime.UtcNow.AddMilliseconds(1800);
        while (DateTime.UtcNow < until)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(20);
        }
    }
}
