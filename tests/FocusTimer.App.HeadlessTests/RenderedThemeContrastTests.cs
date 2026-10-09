namespace FocusTimer.App.HeadlessTests;

using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FocusTimer.App.Services;
using FocusTimer.App.ViewModels;
using FocusTimer.App.Views;
using FocusTimer.Core.Models;
using FocusTimer.Tests;

/// <summary>Measures resolved template brushes on actual surfaces across factory and imported palettes.</summary>
public sealed class RenderedThemeContrastTests
{
    private static readonly string[] ActionRoles = ["secondary", "primary", "destructive"];

    public RenderedThemeContrastTests() => HeadlessAvaloniaFixture.EnsureInitialized();

    [Fact]
    public void ButtonsFieldsChecksSlidersAndTabs_MeetContrastAcrossEveryPaletteAndState()
    {
        var manager = new ThemeManager();
        foreach (Theme theme in ThemeFixtures.All)
        {
            manager.ApplyTheme(theme);
            var box = new TextBox { Text = "Selected text", SelectionStart = 0, SelectionEnd = 8 };
            var numeric = new NumericUpDown { Value = 5 };
            var check = new CheckBox { Content = "Checked", IsChecked = true };
            var slider = new Slider { Value = 40 };
            var tabs = new TabControl { Items = { new TabItem { Header = "First" }, new TabItem { Header = "Second" } } };
            var panel = new StackPanel { Spacing = 8, Children = { box, numeric, check, slider, tabs } };
            var swatch = new Button { Classes = { "color-swatch" }, Background = new SolidColorBrush(Color.Parse(theme.PrimaryText)) };
            panel.Children.Add(swatch);
            var buttons = ActionRoles
                .Select(role => new Button { Content = role, Classes = { $"action-{role}" } }).ToArray();
            foreach (Button button in buttons)
            {
                panel.Children.Add(button);
            }

            var window = new Window { Classes = { "settings-window" }, Content = panel, Width = 500, Height = 600 };
            window.Show();
            Flush(window);
            Color shell = ColorOf(window.Background, Colors.Black);
            foreach (Button button in buttons)
            {
                Size size = button.Bounds.Size;
                ContentPresenter surface = Part<ContentPresenter>(button, "PART_ContentPresenter");
                foreach (string state in new[] { "normal", ":pointerover", ":pressed", ":focus-visible" })
                {
                    State(button, state, true);
                    Pair(theme, $"button/{button.Content}/{state}/text", surface.Foreground, surface.Background, shell, 4.5);
                    if (state == ":focus-visible")
                    {
                        Pair(theme, $"button/{button.Content}/{state}/ring-inner", surface.BorderBrush, surface.Background, shell, 3);
                        Pair(theme, $"button/{button.Content}/{state}/ring-outer", surface.BorderBrush, window.Background, shell, 3);
                    }

                    Flush(window);
                    Assert.Equal(size, button.Bounds.Size);
                    State(button, state, false);
                }

                button.IsEnabled = false;
                Flush(window);
                Assert.False(button.IsEffectivelyEnabled);
                Assert.NotNull(button.Foreground);
            }

            Border field = Part<Border>(box, "PART_BorderElement");
            foreach (string state in new[] { "normal", ":pointerover", ":focus", ":error" })
            {
                State(box, state, true);
                Pair(theme, $"textbox/{state}/text", box.Foreground, field.Background, shell, 4.5);
                Pair(theme, $"textbox/{state}/caret", box.CaretBrush, field.Background, shell, 3);
                Pair(theme, $"textbox/{state}/selection-text", box.SelectionForegroundBrush, box.SelectionBrush, shell, 4.5);
                Pair(theme, $"textbox/{state}/selection-fill", box.SelectionBrush, field.Background, shell, 3);
                Pair(theme, $"textbox/{state}/border", field.BorderBrush, field.Background, shell, 3);
                State(box, state, false);
            }

            box.IsReadOnly = true;
            Flush(window);
            Pair(theme, "textbox/readonly/text", box.Foreground, field.Background, shell, 4.5);
            Pair(theme, "textbox/readonly/selection", box.SelectionBrush, field.Background, shell, 3);
            TextBox numericBox = Part<TextBox>(numeric, "PART_TextBox");
            Pair(theme, "numeric/text", numericBox.Foreground, numeric.Background, shell, 4.5);
            State(numeric, ":focus-within", true);
            Pair(theme, "numeric/focus", numeric.BorderBrush, numeric.Background, shell, 3);
            Border rectangle = Part<Border>(check, "NormalRectangle");
            State(swatch, ":focus-visible", true);
            Pair(theme, "color-swatch/focus", swatch.BorderBrush, window.Background, shell, 3);
            Path glyph = Part<Path>(check, "CheckGlyph");
            Pair(theme, "checkbox/checked/indicator", rectangle.Background, window.Background, shell, 3);
            Pair(theme, "checkbox/checked/glyph", glyph.Fill, rectangle.Background, shell, 3);
            Pair(theme, "checkbox/label", check.Foreground, window.Background, shell, 4.5);
            foreach (bool? checkedState in new bool?[] { false, true, null })
            {
                check.IsThreeState = true;
                check.IsChecked = checkedState;
                foreach (string state in new[] { "normal", ":pointerover", ":pressed", ":focus-visible" })
                {
                    State(check, state, true);
                    Pair(theme, $"checkbox/{checkedState}/{state}/border", rectangle.BorderBrush, window.Background, shell, 3);
                    if (checkedState == true)
                    {
                        Pair(theme, $"checkbox/checked/{state}/glyph", glyph.Fill, rectangle.Background, shell, 3);
                    }

                    State(check, state, false);
                }
            }

            Border[] tracks = slider.GetVisualDescendants().OfType<Border>()
                .Where(b => b.Name == "TrackBackground").ToArray();
            Assert.Equal(2, tracks.Length);
            foreach (Border border in tracks)
            {
                Pair(theme, $"slider/{border.Name}", border.Background, window.Background, shell, 3);
            }

            Thumb thumb = slider.GetVisualDescendants().OfType<Thumb>().Single();
            Border thumbSurface = thumb.GetVisualDescendants().OfType<Border>().Single();
            Assert.True(window.TryFindResource("DesktopSliderThumbBrush", out object? thumbBrush));
            Assert.Equal(ColorOf((IBrush)thumbBrush!, shell), ColorOf(thumbSurface.Background, shell));
            Pair(theme, "slider/thumb-on-active-track", thumbSurface.Background, tracks[0].Background, shell, 3);
            Pair(theme, "slider/thumb-outline", thumbSurface.BorderBrush, window.Background, shell, 3);
            foreach (string state in new[] { ":pointerover", ":pressed", ":focus-visible" })
            {
                State(thumb, state, true);
                Assert.Equal(ColorOf((IBrush)thumbBrush!, shell), ColorOf(thumbSurface.Background, shell));
                Pair(theme, $"slider/thumb/{state}", thumbSurface.Background, tracks[0].Background, shell, 3);
                Pair(theme, $"slider/outline/{state}", thumbSurface.BorderBrush, window.Background, shell, 3);
                State(thumb, state, false);
            }

            foreach (TabItem tab in tabs.Items.OfType<TabItem>())
            {
                Border root = Part<Border>(tab, "PART_LayoutRoot");
                Pair(theme, $"tab/{tab.Header}/text", tab.Foreground, root.Background, shell, 4.5);
                if (tab.IsSelected)
                {
                    Pair(theme, "tab/selected/underline", root.BorderBrush, root.Background, shell, 3);
                }
                else
                {
                    State(tab, ":pointerover", true);
                    Pair(theme, "tab/hover/text", tab.Foreground, root.Background, shell, 4.5);
                    State(tab, ":pointerover", false);
                }

                State(tab, ":focus-visible", true);
                Pair(theme, "tab/focus", Part<Border>(tab, "PART_FocusRing").BorderBrush, root.Background, shell, 3);
                State(tab, ":pointerover", true);
                Pair(theme, "tab/focus-with-hover", Part<Border>(tab, "PART_FocusRing").BorderBrush, root.Background, shell, 3);
                State(tab, ":pointerover", false);
            }

            // Theme switches preserve actual selection and keyboard focus while resolved brushes update.
            box.IsReadOnly = false;
            box.Focus();
            box.SelectionStart = 0;
            box.SelectionEnd = 8;
            foreach (Theme next in new[] { theme, ThemeFixtures.Custom.First() })
            {
                manager.ApplyTheme(next);
                Flush(window);
                Pair(next, "live/text-selection", box.SelectionForegroundBrush, box.SelectionBrush, ColorOf(window.Background, Colors.Black), 4.5);
                Assert.Equal(8, box.SelectionEnd);
                Assert.Same(box, window.FocusManager!.GetFocusedElement());
            }

            window.Close();
        }
    }

    [Fact]
    public void ColorPicker_FieldsLabelsActionsAndPreviewStayReadable()
    {
        var vm = new ColorPickerWindowViewModel("#808080");
        var window = new ColorPickerWindow { DataContext = vm };
        window.Show();
        Flush(window);
        var manager = new ThemeManager();
        foreach (Theme theme in ThemeFixtures.All)
        {
            manager.ApplyTheme(theme);
            Flush(window);
            Color shell = ColorOf(window.Background, Colors.Black);
            foreach (TextBox field in window.GetVisualDescendants().OfType<TextBox>())
            {
                Border root = Part<Border>(field, "PART_BorderElement");
                Pair(theme, "picker/field", field.Foreground, root.Background, shell, 4.5);
            }

            foreach (TextBlock label in window.GetVisualDescendants().OfType<TextBlock>().Where(t => t.Classes.Contains("SettingLabel")))
            {
                Pair(theme, "picker/label", label.Foreground, window.Background, shell, 4.5);
            }

            foreach (string color in new[] { "#808080", "#80000000", "#80FFFFFF", "#FFFFFF", "#000000" })
            {
                vm.ColorHex = color;
                Flush(window);
                TextBlock hex = window.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == vm.SelectedColorHex);
                foreach (Color tile in new[] { Colors.White, Color.Parse("#CCCCCC") })
                {
                    Color swatch = ColorOf(vm.PreviewBrush, tile);
                    Pair(theme, $"picker/preview/{color}", hex.Foreground, hex.Background, swatch, 4.5);
                }
            }
        }

        window.Close();
    }

    [Fact]
    public void OpenDropdownAndCalendar_ResolveReadablePopupChildrenDuringLiveSwitch()
    {
        var manager = new ThemeManager();
        manager.ApplyTheme(ThemeFixtures.All.First());
        var combo = new ComboBox { ItemsSource = new[] { "First", "Second" }, SelectedIndex = 0 };
        var picker = new CalendarDatePicker { SelectedDate = new DateTime(2026, 10, 7) };
        Popup? comboPopup = null;
        Popup? datePopup = null;
        combo.TemplateApplied += (_, e) => comboPopup = e.NameScope.Find<Popup>("PART_Popup");
        picker.TemplateApplied += (_, e) => datePopup = e.NameScope.Find<Popup>("PART_Popup");
        var window = new Window
        {
            Classes = { "settings-window" },
            Width = 500,
            Height = 600,
            Content = new StackPanel { Children = { combo, picker } },
        };
        window.Show();
        Flush(window);
        combo.IsDropDownOpen = true;
        picker.IsDropDownOpen = true;
        Flush(window);
        Assert.NotNull(comboPopup);
        Assert.NotNull(datePopup);
        Assert.True(comboPopup.IsOpen);
        Assert.True(datePopup.IsOpen);
        foreach (Theme theme in ThemeFixtures.All)
        {
            manager.ApplyTheme(theme);
            Flush(window);
            Color shell = ColorOf(window.Background, Colors.Black);
            Pair(theme, "dropdown/closed-text", combo.Foreground, combo.Background, shell, 4.5);
            var items = comboPopup.Child!.GetVisualDescendants().OfType<ComboBoxItem>().ToArray();
            Assert.Equal(2, items.Length);
            foreach (ComboBoxItem item in items)
            {
                ContentPresenter surface = Part<ContentPresenter>(item, "PART_ContentPresenter");
                Pair(theme, $"dropdown/{item.Content}/text", surface.Foreground, surface.Background, shell, 4.5);
                State(item, ":pointerover", true);
                Pair(theme, $"dropdown/{item.Content}/hover-text", surface.Foreground, surface.Background, shell, 4.5);
                State(item, ":pointerover", false);
            }

            var days = datePopup.Child!.GetVisualDescendants().OfType<CalendarDayButton>().Where(d => d.IsEnabled).ToArray();
            Assert.NotEmpty(days);
            foreach (CalendarDayButton day in days)
            {
                Border root = Part<Border>(day, "Root");
                ContentPresenter label = Part<ContentPresenter>(day, "PART_ContentPresenter");
                TextBlock text = label.GetVisualDescendants().OfType<TextBlock>().First();
                Pair(theme, $"calendar/{day.Content}/text (day={day.Foreground}; presenter={label.Foreground})", text.Foreground, root.Background, shell, 4.5);
                State(day, ":pointerover", true);
                Pair(theme, $"calendar/{day.Content}/hover", text.Foreground, root.Background, shell, 4.5);
                State(day, ":pointerover", false);
            }
        }

        window.Close();
    }

    internal static void Pair(Theme theme, string state, IBrush? foreground, IBrush? background, Color parent, double threshold)
    {
        Color back = ColorOf(background, parent);
        Color front = ColorOf(foreground, back);
        double ratio = ThemeContrast.Ratio(front, back);
        Assert.True(ratio >= threshold, $"{theme.ThemeName}/{theme.Author}: {state}: {front} on {back} = {ratio:F3}:1, needs {threshold}:1");
    }

    internal static Color ColorOf(IBrush? brush, Color background)
    {
        if (brush == null)
        {
            return background;
        }

        ISolidColorBrush solid = Assert.IsAssignableFrom<ISolidColorBrush>(brush);
        double alpha = solid.Color.A / 255.0 * solid.Opacity;
        byte Blend(byte front, byte back) => (byte)Math.Round((front * alpha) + (back * (1 - alpha)));
        return Color.FromRgb(Blend(solid.Color.R, background.R), Blend(solid.Color.G, background.G), Blend(solid.Color.B, background.B));
    }

    private static T Part<T>(Control control, string name)
        where T : Control => control.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);

    private static void Flush(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }

    internal static void State(Control control, string name, bool enabled)
    {
        if (name != "normal")
        {
            var states = (IPseudoClasses)typeof(StyledElement).GetProperty("PseudoClasses", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(control)!;
            states.Set(name, enabled);
            Dispatcher.UIThread.RunJobs();
        }
    }
}
