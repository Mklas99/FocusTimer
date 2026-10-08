namespace FocusTimer.App.HeadlessTests;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FocusTimer.App.Services;
using FocusTimer.App.Views;
using FocusTimer.Core.Models;
using FocusTimer.Tests;

/// <summary>Regressions for the actual glyphs, watermarks, and transient surfaces in the user screenshots.</summary>
public sealed class ThemeScreenshotRegressionTests
{
    public ThemeScreenshotRegressionTests() => HeadlessAvaloniaFixture.EnsureInitialized();

    [Fact]
    public void WidgetProjectRow_IsReadableWithSavedLightColorsAndFactoryPalettes()
    {
        Theme legacyLight = ThemeFixtures.All.Single(t => t.ThemeName == "Light").Clone();
        legacyLight.SecondaryText = "#CCCCCC";
        foreach (Theme theme in ThemeFixtures.All.Append(legacyLight))
        {
            new ThemeManager().ApplyTheme(theme);
            var view = new FullModeView
            {
                DataContext = new
                {
                    IsProjectInputVisible = true,
                    EffectiveControlsOpacity = theme.ButtonOpacity,
                    EffectiveClockOpacity = 1.0,
                    ProjectFontSize = 14.0,
                    MainTimerFontSize = 28.0,
                    MainTimerTextWidth = 160.0,
                    ProjectTag = string.Empty,
                },
            };
            var window = new Window { Content = view, Background = new SolidColorBrush(Color.Parse(theme.WindowBackground)), Width = 500, Height = 200 };
            window.Show();
            Flush(window);
            TextBlock label = view.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == "Project:");
            Text(theme, "widget/project-label", label);
            TextBox field = view.GetVisualDescendants().OfType<TextBox>().Single();
            TextBlock watermark = field.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "PART_Watermark");
            Text(theme, "widget/project-watermark", watermark);
            window.Close();
        }
    }

    [Fact]
    public void OpenTooltips_RenderTheirActualTextAndSurfaceInBothFluentVariants()
    {
        foreach (ThemeVariant variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            foreach (Theme theme in ThemeFixtures.All)
            {
                new ThemeManager().ApplyTheme(theme);
                var button = new Button { Content = "Color" };
                var tip = new ToolTip { Content = "Open color picker" };
                ToolTip.SetTip(button, tip);
                var window = Open(variant, button);
                ToolTip.SetIsOpen(button, true);
                Flush(window);
                Thread.Sleep(250);
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                Flush(window);
                TextBlock label = Assert.Single(tip.GetVisualDescendants().OfType<TextBlock>());
                Text(theme, $"{variant}/tooltip", label);
                Assert.Equal(Resource(window, "DesktopShellBrush"), Background(label));
                ToolTip.SetIsOpen(button, false);
                window.Close();
            }
        }
    }

    [Fact]
    public void EmptyReadOnlyFields_RenderReadableWatermarksInBothFluentVariants()
    {
        foreach (ThemeVariant variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            foreach (Theme theme in ThemeFixtures.All)
            {
                new ThemeManager().ApplyTheme(theme);
                var field = new TextBox { Watermark = "e.g. Ctrl+Alt+T", IsReadOnly = true };
                var window = Open(variant, field);
                TextBlock watermark = field.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "PART_Watermark");
                Text(theme, $"{variant}/readonly-watermark", watermark);
                window.Close();
            }
        }
    }

    [Fact]
    public void NumericSpinners_RenderVisibleEnabledGlyphsInBothFluentVariants()
    {
        foreach (ThemeVariant variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            foreach (Theme theme in ThemeFixtures.All)
            {
                new ThemeManager().ApplyTheme(theme);
                var numeric = new NumericUpDown { Value = 1, Minimum = 0, Maximum = 2 };
                var window = Open(variant, numeric);
                RepeatButton[] buttons = numeric.GetVisualDescendants().OfType<RepeatButton>().ToArray();
                Assert.Equal(2, buttons.Length);
                Dump(numeric, "numeric");
                foreach (RepeatButton button in buttons)
                {
                    Assert.True(button.IsEffectivelyEnabled);
                    Path glyph = Assert.Single(button.GetVisualDescendants().OfType<Path>());
                    Assert.True(glyph.IsEffectivelyVisible && glyph.Bounds.Width > 0 && glyph.Bounds.Height > 0,
                        $"{theme.ThemeName}/{variant}/{button.Name}: missing spinner glyph ({glyph.Bounds})");
                    foreach (string state in new[] { "normal", ":pointerover", ":pressed" })
                    {
                        RenderedThemeContrastTests.State(button, state, true);
                        Contrast(theme, $"{variant}/{button.Name}/{state}/glyph", glyph, glyph.Fill, 3);
                        RenderedThemeContrastTests.State(button, state, false);
                    }
                }

                window.Close();
            }
        }
    }

    [Fact]
    public void AccordionAndSwatches_PreserveReadableHeadersAndSourceColors()
    {
        foreach (ThemeVariant variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            foreach (Theme theme in ThemeFixtures.All)
            {
                new ThemeManager().ApplyTheme(theme);
                var accordion = new Expander { Classes = { "settings-accordion" }, Header = "Opacity diagnostics", Content = new TextBlock { Text = "Requested: Off" } };
                Color source = Color.Parse(theme.ButtonHover);
                var swatch = new Button { Classes = { "color-swatch" }, Background = new SolidColorBrush(source) };
                var window = Open(variant, new StackPanel { Children = { accordion, swatch } });
                var header = accordion.GetVisualDescendants().OfType<ToggleButton>().Single();
                foreach (bool expanded in new[] { false, true })
                {
                    accordion.IsExpanded = expanded;
                    foreach (string state in new[] { "normal", ":pointerover", ":pressed", ":focus-visible" })
                    {
                        RenderedThemeContrastTests.State(header, state, true);
                        Flush(window);
                        foreach (TextBlock text in header.GetVisualDescendants().OfType<TextBlock>().Where(t => !string.IsNullOrEmpty(t.Text)))
                        {
                            Text(theme, $"{variant}/accordion/{expanded}/{state}", text);
                        }

                        RenderedThemeContrastTests.State(header, state, false);
                    }
                }

                foreach (string state in new[] { "normal", ":pointerover", ":pressed", ":focus-visible" })
                {
                    RenderedThemeContrastTests.State(swatch, state, true);
                    Flush(window);
                    var surface = swatch.GetVisualDescendants().OfType<ContentPresenter>().Single(p => p.Name == "SwatchSurface");
                    Assert.Equal(source, Assert.IsAssignableFrom<ISolidColorBrush>(surface.Background).Color);
                    RenderedThemeContrastTests.State(swatch, state, false);
                }

                window.Close();
            }
        }
    }

    [Fact]
    public void ProjectSuggestions_UseTheAppPaletteForSurfaceAndItems()
    {
        foreach (ThemeVariant variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            foreach (Theme theme in ThemeFixtures.All)
            {
                new ThemeManager().ApplyTheme(theme);
                var projects = new AutoCompleteBox { ItemsSource = new[] { "Testing", "Theme review" }, MinimumPrefixLength = 0, Watermark = "Optional (type or pick a project)" };
                Popup? popup = null;
                projects.TemplateApplied += (_, e) => popup = e.NameScope.Find<Popup>("PART_Popup");
                var window = Open(variant, projects);
                foreach (TextBlock text in projects.GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible && !string.IsNullOrEmpty(t.Text)))
                {
                    Text(theme, $"{variant}/suggestion-field/placeholder", text);
                }

                projects.Text = "T";
                projects.IsDropDownOpen = true;
                Flush(window);
                Assert.NotNull(popup?.Child);
                var surface = popup.Child.GetVisualDescendants().Prepend(popup.Child).OfType<Border>().Single(b => b.Name == "PART_SuggestionsContainer");
                Assert.Equal(Resource(window, "DesktopShellBrush"), Assert.IsAssignableFrom<ISolidColorBrush>(surface.Background).Color);
                var list = popup.Child.GetVisualDescendants().OfType<ListBox>().Single();
                Assert.NotEmpty(list.GetVisualDescendants().OfType<ListBoxItem>());
                foreach (ListBoxItem item in list.GetVisualDescendants().OfType<ListBoxItem>())
                {
                    foreach (string state in new[] { "normal", ":pointerover", ":pressed", ":selected" })
                    {
                        RenderedThemeContrastTests.State(item, state, true);
                        Flush(window);
                        var itemSurface = item.GetVisualDescendants().OfType<ContentPresenter>().Single(p => p.Name == "PART_ContentPresenter");
                        string surfaceRole = state switch
                        {
                            ":pointerover" => "DesktopControlHoverBrush",
                            ":pressed" => "DesktopControlPressedBrush",
                            ":selected" => "DesktopSelectionBrush",
                            _ => "DesktopShellBrush",
                        };
                        Assert.Equal(Resource(window, surfaceRole), Assert.IsAssignableFrom<ISolidColorBrush>(itemSurface.Background).Color);
                        foreach (TextBlock text in item.GetVisualDescendants().OfType<TextBlock>())
                        {
                            Text(theme, $"{variant}/suggestion/{state}/{text.Text}", text);
                        }

                        RenderedThemeContrastTests.State(item, state, false);
                    }
                }

                Theme next = ThemeFixtures.All.First(t => t.ThemeName == (theme.ThemeName == "Light" ? "Dark" : "Light"));
                new ThemeManager().ApplyTheme(next);
                Flush(window);
                Assert.True(projects.IsDropDownOpen);
                Assert.Equal("T", projects.Text);
                Assert.Equal(Resource(window, "DesktopShellBrush"), Assert.IsAssignableFrom<ISolidColorBrush>(surface.Background).Color);
                foreach (TextBlock text in popup.Child.GetVisualDescendants().OfType<TextBlock>())
                {
                    Text(next, $"{variant}/suggestion/live/{text.Text}", text);
                }

                window.Close();
            }
        }
    }

    [Fact]
    public void DateField_UsesOneSurfaceAndReadableSelectionInEveryState()
    {
        foreach (ThemeVariant variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            foreach (Theme theme in ThemeFixtures.All)
            {
                new ThemeManager().ApplyTheme(theme);
                var date = new CalendarDatePicker { SelectedDate = new DateTime(2026, 10, 7) };
                var window = Open(variant, date);
                var box = date.GetVisualDescendants().OfType<TextBox>().Single();
                var frame = date.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "Background");
                var field = box.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_BorderElement");
                var presenter = box.GetVisualDescendants().OfType<TextPresenter>().Single();
                Color surface = Resource(window, "DesktopFieldBrush");
                foreach (string state in new[] { ":pointerover", ":pressed", ":focus-within" })
                {
                    RenderedThemeContrastTests.State(date, state, true);
                    RenderedThemeContrastTests.State(box, state == ":focus-within" ? ":focus" : ":pointerover", true);
                    box.SelectionStart = 0;
                    box.SelectionEnd = box.Text!.Length;
                    Flush(window);
                    Assert.Equal(surface, Assert.IsAssignableFrom<ISolidColorBrush>(frame.Background).Color);
                    Assert.Equal(Colors.Transparent, Assert.IsAssignableFrom<ISolidColorBrush>(field.Background).Color);
                    Color selection = RenderedThemeContrastTests.ColorOf(presenter.SelectionBrush, surface);
                    Color selectedText = RenderedThemeContrastTests.ColorOf(presenter.SelectionForegroundBrush, selection);
                    Assert.True(ThemeContrast.Ratio(selection, surface) >= 3, $"{theme.ThemeName}/{variant}/{state}: selection fill");
                    Assert.True(ThemeContrast.Ratio(selectedText, selection) >= 4.5, $"{theme.ThemeName}/{variant}/{state}: selected text");
                    Contrast(theme, $"{variant}/date/{state}/text", presenter, presenter.Foreground, 4.5);
                    RenderedThemeContrastTests.State(date, state, false);
                    RenderedThemeContrastTests.State(box, state == ":focus-within" ? ":focus" : ":pointerover", false);
                }

                window.Close();
            }
        }
    }

    [Fact]
    public void ColdDropdownAndCalendar_UseThePaletteInBothFluentVariants()
    {
        foreach (ThemeVariant variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            foreach (Theme theme in ThemeFixtures.All)
            {
                new ThemeManager().ApplyTheme(theme);
                var combo = new ComboBox { ItemsSource = new[] { "Off", "Solid" }, SelectedIndex = 0 };
                var date = new CalendarDatePicker { SelectedDate = new DateTime(2026, 10, 8), DisplayDateEnd = new DateTime(2026, 10, 8) };
                Popup? dropdown = null;
                Popup? calendar = null;
                combo.TemplateApplied += (_, e) => dropdown = e.NameScope.Find<Popup>("PART_Popup");
                date.TemplateApplied += (_, e) => calendar = e.NameScope.Find<Popup>("PART_Popup");
                var window = Open(variant, new StackPanel { Children = { combo, date } });
                combo.IsDropDownOpen = true;
                date.IsDropDownOpen = true;
                Flush(window);
                Assert.NotNull(dropdown?.Child);
                Assert.NotNull(calendar?.Child);
                Dump(dropdown.Child, "dropdown");
                Dump(calendar.Child, "calendar");
                Color shell = Resource(window, "DesktopShellBrush");
                var calendarItem = Assert.Single(calendar.Child.GetVisualDescendants().OfType<CalendarItem>());
                var calendarFrame = calendarItem.GetVisualDescendants().OfType<Border>().First();
                Assert.True(calendarFrame.CornerRadius.TopLeft > 0 && calendarFrame.CornerRadius.BottomRight > 0);
                Assert.True(calendarFrame.ClipToBounds);
                Assert.Equal(new Thickness(1), calendarFrame.BorderThickness);
                Contrast(theme, $"{variant}/calendar/outline", calendarFrame, calendarFrame.BorderBrush, 3);
                var separator = calendarItem.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "MonthHeaderSeparator");
                Assert.Equal(new Thickness(0, 0, 0, 1), separator.BorderThickness);
                Assert.Equal(new Thickness(0), separator.Margin);
                Assert.True(separator.Bounds.Width > 0 && separator.Bounds.Height > 0);
                Assert.Equal(shell, Assert.IsAssignableFrom<ISolidColorBrush>(separator.Background).Color);
                Contrast(theme, $"{variant}/calendar/header-separator", separator, separator.BorderBrush, 3);
                var popupSurface = dropdown.Child.GetVisualDescendants().Prepend(dropdown.Child).OfType<Border>()
                    .First(b => b.Background is ISolidColorBrush brush && brush.Color.A == 255);
                Assert.True(RenderedThemeContrastTests.ColorOf(popupSurface.Background, shell) == shell,
                    $"{theme.ThemeName}/{variant}: dropdown surface {popupSurface.Name} uses {popupSurface.Background} instead of {shell}");
                foreach (TextBlock text in dropdown.Child.GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible))
                {
                    Text(theme, $"{variant}/dropdown/{text.Text}", text);
                }
                foreach (ComboBoxItem item in dropdown.Child.GetVisualDescendants().OfType<ComboBoxItem>())
                {
                    RenderedThemeContrastTests.State(item, ":pointerover", true);
                    foreach (TextBlock text in item.GetVisualDescendants().OfType<TextBlock>())
                    {
                        Text(theme, $"{variant}/dropdown/hover/{text.Text}", text);
                    }

                    RenderedThemeContrastTests.State(item, ":pointerover", false);
                }

                foreach (TextBlock text in calendar.Child.GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible && !string.IsNullOrWhiteSpace(t.Text)))
                {
                    if (text.GetVisualAncestors().OfType<Control>().All(c => c.IsEffectivelyEnabled))
                    {
                        Text(theme, $"{variant}/calendar/{text.Text}", text);
                    }
                }

                foreach (Button button in calendar.Child.GetVisualDescendants().OfType<Button>().Where(b => b.Name is "PART_PreviousButton" or "PART_NextButton"))
                {
                    Path arrow = Assert.Single(button.GetVisualDescendants().OfType<Path>());
                    Contrast(theme, $"{variant}/calendar/{button.Name}", arrow, arrow.Stroke ?? arrow.Fill, button.IsEffectivelyEnabled ? 3 : 1.5);
                    Assert.Equal(Colors.Transparent, Assert.IsAssignableFrom<ISolidColorBrush>(arrow.Fill).Color);
                }

                var actualCalendar = Assert.IsType<Calendar>(calendar.Child);
                var monthGrid = calendarItem.GetVisualDescendants().OfType<Grid>().Single(g => g.Name == "PART_MonthView");
                Assert.Equal(0, monthGrid.MinHeight);
                var lastDay = monthGrid.GetVisualDescendants().OfType<CalendarDayButton>().OrderBy(d => d.Bounds.Bottom).Last();
                Assert.InRange(monthGrid.Bounds.Height - lastDay.Bounds.Bottom, 0, 2);
                var lastColumn = monthGrid.GetVisualDescendants().OfType<CalendarDayButton>().OrderBy(d => d.Bounds.Right).Last();
                Assert.InRange(monthGrid.Bounds.Width - lastColumn.Bounds.Right, 0, 2);
                Assert.InRange(calendarFrame.Bounds.Width - monthGrid.Bounds.Width, 0, 12);
                Assert.Equal(separator.Bounds.Bottom, monthGrid.Bounds.Top);
                Assert.Contains(monthGrid.GetVisualDescendants().OfType<CalendarDayButton>().Where(d => d.IsEffectivelyEnabled),
                    day => day.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "Root").Background is ISolidColorBrush brush
                        && brush.Color == Resource(window, "DesktopCardBrush"));
                var shadedDay = monthGrid.GetVisualDescendants().OfType<CalendarDayButton>().First(day => day.IsEffectivelyEnabled
                    && day.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "Root").Background is ISolidColorBrush brush
                    && brush.Color == Resource(window, "DesktopCardBrush"));
                var shadedSurface = shadedDay.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "Root");
                RenderedThemeContrastTests.State(shadedDay, ":pointerover", true);
                Assert.Equal(Resource(window, "DesktopControlHoverBrush"), Assert.IsAssignableFrom<ISolidColorBrush>(shadedSurface.Background).Color);
                RenderedThemeContrastTests.State(shadedDay, ":selected", true);
                Assert.Equal(Resource(window, "DesktopSelectionBrush"), Assert.IsAssignableFrom<ISolidColorBrush>(shadedSurface.Background).Color);
                foreach (var text in shadedDay.GetVisualDescendants().OfType<TextBlock>())
                {
                    Text(theme, $"{variant}/calendar/shaded-selected-hover", text);
                }

                RenderedThemeContrastTests.State(shadedDay, ":selected", false);
                RenderedThemeContrastTests.State(shadedDay, ":pointerover", false);
                DateTime displayedMonth = actualCalendar.DisplayDate;
                calendarItem.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_PreviousButton")
                    .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                Flush(window);
                Assert.Equal(displayedMonth.AddMonths(-1).Month, actualCalendar.DisplayDate.Month);
                calendarItem.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_HeaderButton")
                    .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                Flush(window);
                Assert.Equal(CalendarMode.Year, actualCalendar.DisplayMode);
                var yearGrid = calendarItem.GetVisualDescendants().OfType<Grid>().Single(g => g.Name == "PART_YearView");
                Assert.True(yearGrid.IsEffectivelyVisible);
                Assert.Equal(shell, Assert.IsAssignableFrom<ISolidColorBrush>(yearGrid.Background).Color);
                actualCalendar.DisplayMode = CalendarMode.Month;
                Flush(window);
                Assert.Equal(new DateTime(2026, 10, 8), actualCalendar.DisplayDateEnd);
                Assert.Contains(calendar.Child.GetVisualDescendants().OfType<CalendarDayButton>(), d => !d.IsEffectivelyEnabled);

                Theme next = ThemeFixtures.All.First();
                new ThemeManager().ApplyTheme(next);
                Flush(window);
                Assert.True(dropdown.IsOpen && calendar.IsOpen);
                Assert.Equal(0, combo.SelectedIndex);
                Assert.Equal(new DateTime(2026, 10, 8), date.SelectedDate);
                foreach (TextBlock text in dropdown.Child.GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible))
                {
                    Text(next, $"{variant}/dropdown/live/{text.Text}", text);
                }

                window.Close();
            }
        }
    }

    private static Window Open(ThemeVariant variant, Control control)
    {
        var window = new Window { RequestedThemeVariant = variant, Classes = { "settings-window" }, Content = control, Width = 640, Height = 540 };
        window.Show();
        Flush(window);
        return window;
    }

    private static void Flush(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }

    private static Color Resource(Control control, string key)
    {
        Assert.True(control.TryFindResource(key, out object? resource));
        return Assert.IsAssignableFrom<ISolidColorBrush>(resource).Color;
    }

    internal static Color Background(Visual visual) => RenderedColors(visual, null).Background;

    private static (Color Background, Color Foreground) RenderedColors(Visual visual, IBrush? foreground)
    {
        Color color = Colors.Black;
        Visual[] path = visual.GetVisualAncestors().Reverse().Append(visual).ToArray();
        var behind = new List<Color>();
        foreach (Visual ancestor in path)
        {
            behind.Add(color);
            IBrush? brush = ancestor switch
            {
                Border border => border.Background,
                Panel panel => panel.Background,
                ContentPresenter presenter => presenter.Background,
                Window window => window.Background,
                _ => null,
            };
            color = RenderedThemeContrastTests.ColorOf(brush, color);
        }

        Color front = RenderedThemeContrastTests.ColorOf(foreground, color);
        for (int index = path.Length - 1; index >= 0; index--)
        {
            // Composite the completed child group once at each ancestor's opacity.
            // Fading text over a field first, then fading the row over the shell, differs from fading its brush alone.
            front = RenderedThemeContrastTests.ColorOf(new SolidColorBrush(front, path[index].Opacity), behind[index]);
            color = RenderedThemeContrastTests.ColorOf(new SolidColorBrush(color, path[index].Opacity), behind[index]);
        }

        return (color, front);
    }

    internal static void Text(Theme theme, string state, TextBlock text) => Contrast(theme, state, text, text.Foreground, 4.5);

    internal static void Contrast(Theme theme, string state, Visual visual, IBrush? brush, double threshold)
    {
        Assert.IsAssignableFrom<ISolidColorBrush>(brush);
        (Color back, Color front) = RenderedColors(visual, brush);
        double opacity = visual.GetVisualAncestors().Prepend(visual).Aggregate(1.0, (value, ancestor) => value * ancestor.Opacity);
        double ratio = ThemeContrast.Ratio(front, back);
        Assert.True(ratio >= threshold, $"{theme.ThemeName}/{state}: {front} on {back}, opacity {opacity:F2}, {ratio:F3}:1 needs {threshold}:1");
    }

    private static void Dump(Control root, string name)
    {
        string? folder = Environment.GetEnvironmentVariable("FOCUSTIMER_THEME_EVIDENCE_DIR");
        if (folder == null)
        {
            return;
        }

        Directory.CreateDirectory(folder);
        File.WriteAllLines(System.IO.Path.Combine(folder, name + ".txt"), root.GetVisualDescendants().Prepend(root).Select(v =>
            $"{v.GetType().Name} #{(v as Control)?.Name} {v.Bounds} opacity={v.Opacity} " +
            (v is TextBlock t ? $"text={t.Text} fg={t.Foreground} font={t.FontFamily}" : v is Path p ? $"fill={p.Fill} data={p.Data}" : v is TemplatedControl c ? $"bg={c.Background} fg={c.Foreground}" : v is Border b ? $"bg={b.Background}" : string.Empty)));
    }
}

