namespace FocusTimer.App.Tests;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using Avalonia.Styling;
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
                    Theme theme = themeService.BuiltInThemes.First(t => t.ThemeName == name);
                    foreach (ThemeVariant variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
                    {
                        manager.ApplyTheme(theme);
                        string label = name + "-OS-" + variant;
                        if (Environment.GetEnvironmentVariable("FOCUSTIMER_EVIDENCE_THEME_SURFACES_ONLY") != "1")
                        {
                            CaptureCalendar(label, folder, variant);
                            CaptureDropdown(label, folder, variant, theme);
                        }
                        CaptureAccordionAndSuggestions(label, folder, variant, theme);
                    }
                }
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(240)), "Popup capture timed out.");
        if (failure != null)
        {
            throw new Xunit.Sdk.XunitException(failure.ToString());
        }
    }

    private static void CaptureCalendar(string theme, string folder, ThemeVariant variant)
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
        var window = new WorklogWindow { RequestedThemeVariant = variant, DataContext = viewModel, Position = new PixelPoint(120, 80) };
        var picker = window.GetLogicalDescendants().OfType<CalendarDatePicker>().First();
        Popup? popup = null;
        picker.TemplateApplied += (_, e) => popup = e.NameScope.Find<Popup>("PART_Popup");
        window.Show();
        window.Activate();
        Settle();
        var dateBox = picker.GetVisualDescendants().OfType<TextBox>().Single();
        dateBox.Focus();
        dateBox.SelectionStart = 0;
        dateBox.SelectionEnd = dateBox.Text!.Length;
        Settle();
        SavePopup(picker, Path.Combine(folder, $"date-field-selected-{theme}.png"));
        picker.IsDropDownOpen = true;
        Settle();
        Assert.NotNull(popup?.Child);
        var calendarItem = Assert.Single(popup.Child.GetVisualDescendants().OfType<CalendarItem>());
        var monthGrid = calendarItem.GetVisualDescendants().OfType<Grid>().Single(g => g.Name == "PART_MonthView");
        Assert.Equal(0, monthGrid.MinHeight);
        var lastDay = monthGrid.GetVisualDescendants().OfType<CalendarDayButton>().OrderBy(d => d.Bounds.Bottom).Last();
        Assert.InRange(monthGrid.Bounds.Height - lastDay.Bounds.Bottom, 0, 2);
        var frame = calendarItem.GetVisualDescendants().OfType<Border>().First();
        Assert.True(frame.CornerRadius.TopLeft > 0 && frame.CornerRadius.BottomRight > 0);
        Assert.True(frame.ClipToBounds);
        Assert.Equal(new Thickness(1), frame.BorderThickness);
        Color surface = ResourceColor(window, "DesktopShellBrush");
        Assert.True(ThemeContrast.Ratio(Assert.IsAssignableFrom<ISolidColorBrush>(frame.BorderBrush).Color, surface) >= 3);
        var separator = calendarItem.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "MonthHeaderSeparator");
        Assert.Equal(new Thickness(0, 0, 0, 1), separator.BorderThickness);
        var lastColumn = monthGrid.GetVisualDescendants().OfType<CalendarDayButton>().OrderBy(d => d.Bounds.Right).Last();
        Assert.InRange(monthGrid.Bounds.Width - lastColumn.Bounds.Right, 0, 2);
        Assert.Equal(surface, Assert.IsAssignableFrom<ISolidColorBrush>(separator.Background).Color);
        Assert.True(ThemeContrast.Ratio(Assert.IsAssignableFrom<ISolidColorBrush>(separator.BorderBrush).Color, surface) >= 3);
        foreach (Button button in popup.Child.GetVisualDescendants().OfType<Button>().Where(b => b.Name is "PART_PreviousButton" or "PART_NextButton"))
        {
            var arrow = Assert.Single(button.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>());
            var stroke = Assert.IsAssignableFrom<ISolidColorBrush>(arrow.Stroke);
            Color shell = ResourceColor(window, "DesktopShellBrush");
            double alpha = stroke.Color.A / 255.0 * stroke.Opacity * arrow.GetVisualAncestors().Prepend(arrow).Aggregate(1.0, (opacity, visual) => opacity * visual.Opacity);
            byte Blend(byte front, byte back) => (byte)Math.Round(front * alpha + back * (1 - alpha));
            Color rendered = Color.FromRgb(Blend(stroke.Color.R, shell.R), Blend(stroke.Color.G, shell.G), Blend(stroke.Color.B, shell.B));
            Assert.True(ThemeContrast.Ratio(rendered, shell) >= (button.IsEffectivelyEnabled ? 3 : 1.5), $"{theme}: {button.Name} arrow");
            Assert.Equal(Colors.Transparent, Assert.IsAssignableFrom<ISolidColorBrush>(arrow.Fill).Color);
        }
        SavePopup(popup.Child, Path.Combine(folder, $"popup-calendar-{theme}.png"));
        var calendar = Assert.IsType<Calendar>(popup.Child);
        calendar.DisplayDate = new DateTime(2026, 3, 15);
        calendar.SelectedDate = new DateTime(2026, 3, 15);
        Settle();
        SavePopup(popup.Child, Path.Combine(folder, $"popup-calendar-full-month-{theme}.png"));
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

    private static void CaptureDropdown(string theme, string folder, ThemeVariant variant, Theme palette)
    {
        SettingsWindowViewModel editor = SettingsWindowViewModelTests.CreateAppearanceEditor(palette);
        editor.SelectedTabIndex = 2;
        var window = new SettingsWindow { RequestedThemeVariant = variant, DataContext = editor, Position = new PixelPoint(120, 80) };
        var popups = new Dictionary<ComboBox, Popup?>();
        foreach (ComboBox candidate in window.GetLogicalDescendants().OfType<ComboBox>())
        {
            candidate.TemplateApplied += (_, e) => popups[candidate] = e.NameScope.Find<Popup>("PART_Popup");
        }
        window.Show();
        window.Activate();
        Settle();
        ComboBox combo = window.GetVisualDescendants().OfType<ComboBox>().First(c => c.Items.Cast<object>().Any(item => item?.ToString() == "Solid"));
        combo.BringIntoView();
        Settle();
        combo.IsDropDownOpen = true;
        Settle();
        Assert.True(popups.TryGetValue(combo, out Popup? popup));
        Assert.NotNull(popup?.Child);
        Border surface = popup.Child.GetVisualDescendants().Prepend(popup.Child).OfType<Border>().First(b => b.Name == "PopupBorder");
        Assert.Equal(ResourceColor(window, "DesktopShellBrush"), Assert.IsAssignableFrom<ISolidColorBrush>(surface.Background).Color);
        SavePopup(popup.Child, Path.Combine(folder, $"popup-dropdown-{theme}.png"));
        combo.IsDropDownOpen = false;
        window.Close();
    }

    private static void CaptureAccordionAndSuggestions(string label, string folder, ThemeVariant variant, Theme theme)
    {
        var accordion = new Expander
        {
            Classes = { "settings-accordion" },
            Header = "Opacity diagnostics",
            IsExpanded = true,
            Content = new TextBlock { Text = "Requested: Off | Active: Transparent", Margin = new Thickness(0, 8) },
        };
        Color source = Color.Parse(theme.ButtonHover);
        var swatch = new Button { Classes = { "color-swatch" }, Background = new SolidColorBrush(source) };
        var projects = new AutoCompleteBox { ItemsSource = new[] { "Testing", "Theme review" }, MinimumPrefixLength = 0 };
        Popup? popup = null;
        projects.TemplateApplied += (_, e) => popup = e.NameScope.Find<Popup>("PART_Popup");
        var content = new StackPanel { Margin = new Thickness(12), Spacing = 12, Children = { accordion, swatch, projects } };
        var window = new Window { Classes = { "settings-window" }, RequestedThemeVariant = variant, Content = content, Width = 480, Height = 300, Topmost = true, Position = new PixelPoint(120, 80) };
        window.Show();
        window.Background = new SolidColorBrush(ResourceColor(window, "DesktopShellBrush"));
        window.Activate();
        Settle();
        var states = (IPseudoClasses)typeof(StyledElement).GetProperty("PseudoClasses", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(swatch)!;
        states.Set(":pointerover", true);
        Settle();
        var swatchSurface = swatch.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ContentPresenter>().Single(p => p.Name == "SwatchSurface");
        Assert.Equal(source, Assert.IsAssignableFrom<ISolidColorBrush>(swatchSurface.Background).Color);
        SavePopup(content, Path.Combine(folder, $"accordion-expanded-swatch-hover-{label}.png"));
        projects.Text = "T";
        projects.IsDropDownOpen = true;
        Settle();
        Assert.NotNull(popup?.Child);
        var surface = popup.Child.GetVisualDescendants().Prepend(popup.Child).OfType<Border>().Single(b => b.Name == "PART_SuggestionsContainer");
        Assert.Equal(ResourceColor(window, "DesktopShellBrush"), Assert.IsAssignableFrom<ISolidColorBrush>(surface.Background).Color);
        SavePopup(popup.Child, Path.Combine(folder, $"project-suggestions-{label}.png"));
        popup.Child.GetVisualDescendants().OfType<ListBox>().Single().SelectedIndex = 0;
        Settle();
        SavePopup(popup.Child, Path.Combine(folder, $"project-suggestions-selected-{label}.png"));
        projects.IsDropDownOpen = false;
        window.Close();
    }

    private static Color ResourceColor(Control control, string key)
    {
        Assert.True(control.TryFindResource(key, out object? brush));
        return Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;
    }

    private static void SavePopup(Control popup, string path)
    {
        PixelPoint origin = popup.PointToScreen(new Point(-8, -8));
        double scale = TopLevel.GetTopLevel(popup)!.RenderScaling;
        int width = (int)Math.Ceiling((popup.Bounds.Width + 16) * scale);
        int height = (int)Math.Ceiling((popup.Bounds.Height + 16) * scale);
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
