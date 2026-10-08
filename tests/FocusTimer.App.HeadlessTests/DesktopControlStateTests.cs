namespace FocusTimer.App.HeadlessTests;

using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FocusTimer.App.Services;
using FocusTimer.App.Views;
using FocusTimer.Core.Models;

/// <summary>
/// Shared desktop control states resolve to the theme roles in every state without moving layout, and the
/// desktop selectors never reach the timer widget.
/// </summary>
public sealed class DesktopControlStateTests
{
    private static readonly Theme Palette = new()
    {
        InputBackground = "#223344",
        InputBorder = "#445566",
        AccentPrimary = "#CC8800",
        InputText = "#EEEEEE",
        PrimaryText = "#FAFAFA",
        DisabledText = "#555555",
        TabSelectedBackground = "#AA5500",
        TabSelectedText = "#FFFFFF",
        DangerColor = "#CC0000",
    };

    public DesktopControlStateTests() => HeadlessAvaloniaFixture.EnsureInitialized();

    [Fact]
    public void Button_ResolvesEveryStateToThemeRolesWithoutMovingLayout()
    {
        var button = new Button { Content = "Sample" };
        Window window = Show(button);
        ContentPresenter surface = button.GetVisualDescendants().OfType<ContentPresenter>().First(p => p.Name == "PART_ContentPresenter");
        Rect normalBounds = button.Bounds;

        Assert.Equal(Color.Parse("#223344"), Brush(surface.Background));
        Assert.Equal(ResourceColor("DesktopBorderBrush"), Brush(surface.BorderBrush));
        Assert.Equal(new Thickness(1), surface.BorderThickness);
        Assert.Equal(new CornerRadius(8), button.CornerRadius);
        Assert.True(normalBounds.Height >= 25);

        IBrush? hover = Set(button, ":pointerover", () => surface.Background);
        IBrush? pressed = Set(button, ":pressed", () => surface.Background, keep: ":pointerover");
        Assert.NotEqual(Color.Parse("#223344"), Brush(hover));
        Assert.NotEqual(Brush(hover), Brush(pressed));

        Assert.Equal(Color.Parse("#CC8800"), Set(button, ":focus-visible", () => Brush(surface.BorderBrush)));
        Assert.Equal(new Thickness(2), surface.BorderThickness);
        Assert.Equal(normalBounds.Size, button.Bounds.Size);

        Clear(button, ":focus-visible");
        Clear(button, ":pointerover");
        Clear(button, ":pressed");
        button.IsEnabled = false;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Color.Parse("#555555"), Brush(button.Foreground));
        Assert.Equal(normalBounds.Size, button.Bounds.Size);
        window.Close();
    }

    [Fact]
    public void TextBox_ShowsFocusReadOnlyInvalidSelectionAndCaretRoles()
    {
        var box = new TextBox { Text = "value" };
        Window window = Show(box);
        Border border = box.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_BorderElement");
        Size normalSize = box.Bounds.Size;

        Assert.Equal(ResourceColor("DesktopBorderBrush"), Brush(border.BorderBrush));
        Assert.Equal(Color.Parse("#223344"), Brush(border.Background));
        Assert.Equal(new CornerRadius(8), box.CornerRadius);
        Assert.True(normalSize.Height >= 25);
        Assert.Equal(Color.Parse("#EEEEEE"), Brush(box.CaretBrush));
        Assert.Equal(Color.Parse("#AA5500"), Brush(box.SelectionBrush));
        Assert.Equal(Color.Parse("#FFFFFF"), Brush(box.SelectionForegroundBrush));

        Assert.Equal(Color.Parse("#CC8800"), Set(box, ":focus", () => Brush(border.BorderBrush)));
        Clear(box, ":focus");

        Assert.Equal(ResourceColor("DesktopDangerBrush"), Set(box, ":error", () => Brush(border.BorderBrush)));
        Clear(box, ":error");

        box.IsReadOnly = true;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Colors.Transparent, Brush(border.Background));
        Assert.Equal(ResourceColor("DesktopBorderBrush"), Brush(border.BorderBrush));
        Assert.Equal(Color.Parse("#CC8800"), Set(box, ":focus", () => Brush(border.BorderBrush)));
        Assert.Equal(normalSize, box.Bounds.Size);
        window.Close();
    }

    [Fact]
    public void NumericDropdownCheckBoxAndSlider_ShareHeightsAndRadii()
    {
        var numeric = new NumericUpDown { Value = 5, Classes = { "numeric-field" } };
        var combo = new ComboBox { ItemsSource = new[] { "A", "B" }, SelectedIndex = 0 };
        var check = new CheckBox { Content = "Option", IsChecked = true };
        var slider = new Slider { Minimum = 0, Maximum = 10, Value = 3 };
        var panel = new StackPanel { Spacing = 8, Children = { numeric, combo, check, slider } };
        Window window = Show(panel);

        Assert.Equal(new CornerRadius(8), numeric.CornerRadius);
        Assert.Equal(new CornerRadius(8), combo.CornerRadius);
        Assert.True(numeric.Bounds.Height >= 25);
        Assert.True(combo.Bounds.Height >= 25);
        Assert.True(check.Bounds.Height >= 26);
        Assert.True(slider.Bounds.Height >= 14);
        Assert.Equal(132, numeric.Bounds.Width);
        Assert.Equal(ResourceColor("DesktopBorderBrush"), Brush(numeric.BorderBrush));
        Assert.Equal(ResourceColor("DesktopBorderBrush"), Brush(combo.BorderBrush));

        Set(numeric, ":focus-within", () => Brush(numeric.BorderBrush));
        Assert.Equal(Color.Parse("#CC8800"), Brush(numeric.BorderBrush));
        Clear(numeric, ":focus-within");
        window.Close();
    }

    [Fact]
    public void DesktopStyles_DoNotReachTheTimerWidgetViews()
    {
        foreach (Control view in new Control[] { new FullModeView(), new CompactModeView() })
        {
            var host = new Window { Content = view, Width = 400, Height = 300 };
            host.Show();
            Dispatcher.UIThread.RunJobs();
            host.UpdateLayout();

            Assert.DoesNotContain("settings-window", host.Classes);
            Assert.NotEqual("Segoe UI", host.FontFamily.Name);
            foreach (Button button in view.GetVisualDescendants().OfType<Button>())
            {
                Assert.True(button.MinHeight < 36, $"{view.GetType().Name}: {button.Name} picked up the desktop button height");
                Assert.NotEqual(new CornerRadius(8), button.CornerRadius);
            }

            Assert.True(Application.Current!.TryGetResource("TabularTimerFontFamily", null, out object? tabular));
            Assert.Contains("Consolas", tabular!.ToString(), StringComparison.Ordinal);
            host.Close();
        }
    }

    private static Window Show(Control content)
    {
        var manager = new ThemeManager();
        manager.ApplyTheme(Palette);
        var window = new Window
        {
            Classes = { "settings-window" },
            Content = new StackPanel { Margin = new Thickness(16), Children = { content } },
            Width = 400,
            Height = 400,
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return window;
    }

    [Theory]
    [InlineData("primary")]
    [InlineData("secondary")]
    [InlineData("destructive")]
    public void ActionRoles_PreserveFocusDisabledStateAndGeometry(string role)
    {
        var button = new Button { Content = "Action", Classes = { $"action-{role}" } };
        Window window = Show(button);
        ContentPresenter surface = button.GetVisualDescendants().OfType<ContentPresenter>()
            .First(p => p.Name == "PART_ContentPresenter");
        Size size = button.Bounds.Size;
        Assert.Equal(role == "secondary" ? FontWeight.Normal : FontWeight.SemiBold, button.FontWeight);
        Assert.Equal(role == "primary" ? ResourceColor("DesktopPrimaryActionBrush") :
            role == "destructive" ? ResourceColor("DesktopDestructiveActionBrush") : ResourceColor("DesktopFieldBrush"), Brush(surface.Background));
        if (role == "destructive")
        {
            Assert.Equal(ResourceColor("DesktopDestructiveActionTextBrush"), Brush(surface.BorderBrush));
        }

        foreach (string state in new[] { ":pointerover", ":pressed" })
        {
            Set(button, state, () => surface.Background);
            Assert.NotEqual(Colors.Transparent, Brush(surface.Background));
            Set(button, ":focus-visible", () => surface.BorderBrush);
            Assert.Equal(ResourceColor("DesktopActionFocusBrush"), Brush(surface.BorderBrush));
            Assert.Equal(new Thickness(2), surface.BorderThickness);
            window.UpdateLayout();
            Assert.Equal(size, button.Bounds.Size);
            Clear(button, ":focus-visible");
            Clear(button, state);
        }

        Set(button, ":pointerover", () => surface.Background);
        Set(button, ":pressed", () => surface.Background);
        button.IsEnabled = false;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(ResourceColor("DesktopFieldBrush"), Brush(surface.Background));
        Assert.Equal(ResourceColor("DesktopBorderBrush"), Brush(surface.BorderBrush));
        Assert.Equal(Color.Parse(Palette.DisabledText), Brush(button.Foreground));
        window.Close();
    }

    private static Color Brush(IBrush? brush) => ((ISolidColorBrush)brush!).Color;

    private static Color ResourceColor(string key) => Brush((IBrush)Application.Current!.Resources[key]!);

    private static T Set<T>(Control control, string pseudoClass, Func<T> read, string? keep = null)
    {
        if (keep != null)
        {
            PseudoClasses(control).Set(keep, true);
        }

        PseudoClasses(control).Set(pseudoClass, true);
        Dispatcher.UIThread.RunJobs();
        return read();
    }

    private static void Clear(Control control, string pseudoClass)
    {
        PseudoClasses(control).Set(pseudoClass, false);
        Dispatcher.UIThread.RunJobs();
    }

    private static IPseudoClasses PseudoClasses(Control control) =>
        (IPseudoClasses)typeof(StyledElement).GetProperty("PseudoClasses", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(control)!;
}
