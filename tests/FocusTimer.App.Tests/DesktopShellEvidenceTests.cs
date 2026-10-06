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
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;
using ReactiveUI;

/// <summary>
/// Renders every Settings and Worklog page at the supported window sizes with the real Windows
/// Skia backend and stores the captures as review evidence. Layout assertions run alongside the captures.
/// Run separately: FOCUSTIMER_NATIVE_APPEARANCE_TESTS=1 (native Avalonia state is process-wide).
/// Set FOCUSTIMER_EVIDENCE_DIR to choose where the PNG files are written.
/// </summary>
[Collection("Native appearance")]
public class DesktopShellEvidenceTests
{
    private static readonly (string Label, double Width, double Height)[] SettingsSizes =
    [
        ("default", 640, 540),
        ("minimum", 500, 400),
        ("full-narrow", 500, 1500),
        ("full-wide", 760, 1500),
    ];

    private static readonly (string Label, double Width, double Height)[] WorklogSizes =
    [
        ("default", 900, 620),
        ("minimum", 640, 560),
    ];

    // FOCUSTIMER_EVIDENCE_CAPTURE_ONLY=1 renders without layout assertions, and FOCUSTIMER_EVIDENCE_TAB_ORDER
    // names the tabs of an older build: both exist to capture "before" images from a baseline checkout.
    private static readonly bool CaptureOnly = Environment.GetEnvironmentVariable("FOCUSTIMER_EVIDENCE_CAPTURE_ONLY") == "1";

    private static readonly string[] SettingsTabs =
        (Environment.GetEnvironmentVariable("FOCUSTIMER_EVIDENCE_TAB_ORDER") ?? "General,Logging,Appearance,Hotkeys,About")
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static readonly string[] WorklogTabs = ["Entries", "Timeline", "Summary"];

    [AppearanceNativeTests.NativeAppearanceFact]
    public void EveryDesktopPage_RendersWithoutClippingAtSupportedSizes()
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
                var themeService = new ThemeService();
                string[] themeNames = (Environment.GetEnvironmentVariable("FOCUSTIMER_EVIDENCE_THEMES") ?? "Dark")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (string themeName in themeNames)
                {
                    Theme theme = themeService.BuiltInThemes.First(t => t.ThemeName == themeName);
                    manager.ApplyTheme(theme);
                    CaptureSettings(manager, themeName, folder);
                    CaptureWorklogAsync(themeName, folder).GetAwaiter().GetResult();
                    CaptureColorPicker(themeName, folder);
                }

                if (!CaptureOnly)
                {
                    AssertThemeSwitchKeepsLayout(manager, themeService, folder);
                }
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(120)), "Desktop evidence capture timed out.");
        if (failure != null)
        {
            throw new Xunit.Sdk.XunitException(failure.ToString());
        }
    }

    private static void CaptureSettings(ThemeManager manager, string themeName, string folder)
    {
        foreach (var size in SettingsSizes)
        {
            for (int tab = 0; tab < SettingsTabs.Length; tab++)
            {
                SettingsWindowViewModel editor = SettingsWindowViewModelTests.CreateAppearanceEditor();
                editor.SelectedTabIndex = tab;
                var window = new SettingsWindow
                {
                    DataContext = editor,
                    Width = size.Width,
                    Height = size.Height,
                    ShowActivated = false,
                    Position = new PixelPoint(-3000, -3000),
                };
                if (tab == 1)
                {
                    // One valid rule, one blank (warning, dropped on apply), one half-filled (danger, blocks apply).
                    editor.ProjectList.Load([new ProjectRule("code", "*repo*", "Alpha")]);
                    editor.ProjectList.Rules.Add(new ProjectRuleItemViewModel());
                    editor.ProjectList.Rules.Add(new ProjectRuleItemViewModel(new ProjectRule("mail", null, null)));
                }

                window.Show();
                Settle();
                Capture(window, Path.Combine(folder, $"settings-{themeName}-{SettingsTabs[tab]}-{size.Label}.png"));
                if (tab == 4 && size.Label == "full-narrow")
                {
                    for (int click = 0; click < 7; click++)
                    {
                        editor.RegisterVersionInfoClick();
                    }

                    Settle();
                    foreach (Expander expander in window.GetVisualDescendants().OfType<Expander>())
                    {
                        expander.IsExpanded = true;
                    }

                    Settle();
                    Capture(window, Path.Combine(folder, $"settings-{themeName}-{SettingsTabs[tab]}-{size.Label}-developer.png"));
                }

                AssertCommitActionsReachable(window);
                AssertTabLabelsFit(window);
                AssertNoHorizontalClipping(window, $"{SettingsTabs[tab]} {size.Label}");
                if (tab == 2 && size.Label.StartsWith("full", StringComparison.Ordinal))
                {
                    foreach (Expander expander in window.GetVisualDescendants().OfType<Expander>())
                    {
                        expander.IsExpanded = true;
                    }

                    Settle();
                    Capture(window, Path.Combine(folder, $"settings-{themeName}-{SettingsTabs[tab]}-{size.Label}-expanded.png"));
                }
                window.Close();
            }
        }
    }

    private static async Task CaptureWorklogAsync(string themeName, string folder)
    {
        var clock = new MutableClock(new DateTimeOffset(2026, 5, 4, 12, 0, 0, TimeSpan.Zero));
        var store = new MemoryWorklogStore();
        store.Add(WorklogTestData.Tracked("a", new DateTimeOffset(2026, 5, 4, 8, 0, 0, TimeSpan.Zero), 30, app: "code"));
        store.Add(WorklogTestData.Tracked("b", new DateTimeOffset(2026, 5, 4, 9, 0, 0, TimeSpan.Zero), 10, app: "mail"));
        var resolver = new RuleProjectResolver(new ProjectRuleStore());
        var groupings = new WorklogGroupingRegistry([new ApplicationGrouping(), new ProjectGrouping(), new WindowGrouping()]);
        foreach (var size in WorklogSizes)
        {
            for (int tab = 0; tab < WorklogTabs.Length; tab++)
            {
                var viewModel = new WorklogWindowViewModel(
                    new WorklogEntriesViewModel(store, clock, null, resolver),
                    new WorklogSummaryViewModel(new WorklogSummaryService(store, groupings, resolver, new QuietLogger()), groupings, clock),
                    new SettingsStub(),
                    clock,
                    null,
                    groupings,
                    resolver);
                var window = new WorklogWindow
                {
                    DataContext = viewModel,
                    Width = size.Width,
                    Height = size.Height,
                    ShowActivated = false,
                    Position = new PixelPoint(-3000, -3000),
                };
                window.Show();
                await viewModel.OpenAsync();
                window.FindControl<TabControl>("Tabs")!.SelectedIndex = tab;
                await viewModel.SelectTabAsync((WorklogTab)tab);
                Settle();
                Capture(window, Path.Combine(folder, $"worklog-{themeName}-{WorklogTabs[tab]}-{size.Label}.png"));
                window.Close();
            }
        }
    }

    private static void CaptureColorPicker(string themeName, string folder)
    {
        var picker = new ColorPickerWindow
        {
            DataContext = new ColorPickerWindowViewModel("#0078D7"),
            ShowActivated = false,
            Position = new PixelPoint(-3000, -3000),
        };
        picker.Show();
        Settle();
        Capture(picker, Path.Combine(folder, $"color-picker-{themeName}.png"));
        picker.Close();
    }

    /// <summary>
    /// Switches every built-in theme while Settings stays open: the window must keep its size and every
    /// surface must follow the new Settings background.
    /// </summary>
    private static void AssertThemeSwitchKeepsLayout(ThemeManager manager, ThemeService themeService, string folder)
    {
        SettingsWindowViewModel editor = SettingsWindowViewModelTests.CreateAppearanceEditor();
        var window = new SettingsWindow
        {
            DataContext = editor,
            Width = 640,
            Height = 540,
            ShowActivated = false,
            Position = new PixelPoint(-3000, -3000),
        };
        window.Show();
        Settle();
        Size size = window.Bounds.Size;
        foreach (Theme theme in themeService.BuiltInThemes)
        {
            manager.ApplyTheme(theme);
            Settle();
            var background = Assert.IsAssignableFrom<Avalonia.Media.ISolidColorBrush>(window.Background);
            Assert.Equal(Avalonia.Media.Color.Parse(theme.SettingsBackground), background.Color);
            Assert.Equal(size, window.Bounds.Size);
            AssertCommitActionsReachable(window);
            Capture(window, Path.Combine(folder, $"theme-switch-{theme.ThemeName.Replace(' ', '-')}.png"));
        }

        window.Close();
    }

    private static void AssertCommitActionsReachable(Window window)
    {
        if (CaptureOnly)
        {
            return;
        }

        window.UpdateLayout();
        foreach (string label in new[] { "OK", "Apply", "Cancel" })
        {
            Button button = window.GetVisualDescendantsOfType<Button>().Single(b => Equals(b.Content, label));
            Point origin = button.TranslatePoint(default, window)!.Value;
            var bounds = new Rect(origin, button.Bounds.Size);
            Assert.True(bounds.Left >= 0 && bounds.Right <= window.Bounds.Width, $"{label} horizontally clipped in {window.Width}x{window.Height}");
            Assert.True(bounds.Top >= 0 && bounds.Bottom <= window.Bounds.Height, $"{label} vertically clipped in {window.Width}x{window.Height}");
        }
    }

    private static void AssertTabLabelsFit(Window window)
    {
        if (CaptureOnly)
        {
            return;
        }

        TabControl tabs = window.FindControl<TabControl>("DraftPanel")!;
        ScrollViewer strip = tabs.GetVisualDescendants().OfType<ScrollViewer>().First();
        Assert.True(
            strip.Extent.Width <= strip.Viewport.Width + 0.5,
            $"Settings tab labels overflow at {window.Width}x{window.Height}: {strip.Extent.Width} > {strip.Viewport.Width}");
    }

    private static void AssertNoHorizontalClipping(Window window, string context)
    {
        if (CaptureOnly)
        {
            return;
        }

        window.UpdateLayout();
        ScrollViewer page = window.GetVisualDescendants().OfType<ScrollViewer>()
            .First(viewer => viewer.Classes.Contains("settings-scroll") && viewer.IsEffectivelyVisible);
        var content = (Control)page.Content!;
        double limit = content.TranslatePoint(default, page)!.Value.X + content.Bounds.Width + 0.5;
        foreach (Control control in content.GetVisualDescendants().OfType<Control>())
        {
            if (!control.IsEffectivelyVisible || control.Bounds.Width <= 0 ||
                control is Avalonia.Controls.Shapes.Path || control.RenderTransform != null || control.GetVisualAncestors().OfType<Control>().Any(a => a.RenderTransform != null) || control.GetVisualAncestors().OfType<Viewbox>().Any())
            {
                continue;
            }

            Point origin = control.TranslatePoint(default, page)!.Value;
            Assert.True(
                origin.X + control.Bounds.Width <= limit,
                $"{context}: {control.GetType().Name} '{control.Name}' {control.Bounds} (parent {control.Parent?.GetType().Name}, children {(control as Panel == null ? string.Empty : string.Join(" ", ((Panel)control).Children.Select(c => c.GetType().Name + c.Bounds)))}) ends at {origin.X + control.Bounds.Width} beyond {limit}");
        }
    }

    private static void Settle()
    {
        for (int i = 0; i < 6; i++)
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

internal sealed class QuietLogger : IAppLogger
{
    public void LogCritical(string message, Exception? ex = null)
    {
    }

    public void LogDebug(string message)
    {
    }

    public void LogError(string message, Exception? ex = null)
    {
    }

    public void LogInformation(string message)
    {
    }

    public void LogWarning(string message)
    {
    }
}

internal static class VisualExtensions
{
    public static IEnumerable<T> GetVisualDescendantsOfType<T>(this Avalonia.Visual visual)
        where T : class =>
        Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(visual).OfType<T>();
}
