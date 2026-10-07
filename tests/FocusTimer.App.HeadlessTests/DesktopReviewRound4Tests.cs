namespace FocusTimer.App.HeadlessTests;

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FocusTimer.App.Services;
using FocusTimer.App.ViewModels;
using FocusTimer.App.Views;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;
using FocusTimer.Core.Stubs;
using Material.Icons.Avalonia;

/// <summary>
/// The fourth review round: Worklog toolbar and table header, hover hints instead of loose helper text,
/// opacity wording, lighter rule actions, color picker labels, and the widget project row and compact buttons.
/// </summary>
public sealed class DesktopReviewRound4Tests
{
    public DesktopReviewRound4Tests() => HeadlessAvaloniaFixture.EnsureInitialized();

    [Fact]
    public void Worklog_HasOneIconRefreshAtTheSameRightPlaceOnEveryTab()
    {
        var window = new WorklogWindow { Width = 900, Height = 620 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        TabControl tabs = window.FindControl<TabControl>("Tabs")!;

        Button refresh = Assert.Single(window.GetVisualDescendants().OfType<Button>(), b => AutomationProperties.GetName(b) == "Refresh");
        Assert.IsType<MaterialIcon>(refresh.Content);
        Assert.DoesNotContain(window.GetVisualDescendants().OfType<Button>(), b => b.Content is "Refresh");

        var places = new List<Point>();
        for (int i = 0; i < 3; i++)
        {
            tabs.SelectedIndex = i;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            places.Add(refresh.TranslatePoint(default, window)!.Value);
        }

        Assert.All(places, p => Assert.Equal(places[0], p));
        Assert.True(places[0].X + refresh.Bounds.Width > window.Bounds.Width - 40, "The refresh icon sits on the right side.");
        window.Close();
    }

    [Fact]
    public void Worklog_DayArrowsAreIconsAndTheTimelineHintIsAHoverIcon()
    {
        var window = new WorklogWindow { Width = 900, Height = 620 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        foreach (string name in new[] { "Previous day", "Next day" })
        {
            Button arrow = window.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetName(b) == name);
            Assert.IsType<MaterialIcon>(arrow.Content);
        }

        window.FindControl<TabControl>("Tabs")!.SelectedIndex = 1;
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Border hint = window.GetVisualDescendants().OfType<Border>().Single(b => b.Classes.Contains("help-hint"));
        Assert.Equal("Ctrl + mouse wheel zooms the hours", ToolTip.GetTip(hint));
        Assert.DoesNotContain(
            window.GetVisualDescendants().OfType<TextBlock>(),
            t => t.Text?.Contains("mouse wheel", StringComparison.Ordinal) == true);
        window.Close();
    }

    [Fact]
    public void WorklogSummary_DoesNotRepeatTheDayAboveTheTabs()
    {
        var window = new WorklogWindow { Width = 900, Height = 620 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        window.FindControl<TabControl>("Tabs")!.SelectedIndex = 2;
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        WorklogSummaryView summary = window.GetVisualDescendants().OfType<WorklogSummaryView>().Single();

        Assert.DoesNotContain(
            summary.GetVisualDescendants().OfType<TextBlock>(),
            t => t.Classes.Contains("SectionHeader"));
        Assert.DoesNotContain(summary.GetVisualDescendants().OfType<Button>(), b => b.Content is "Refresh");
        window.Close();
    }

    [Fact]
    public void WorklogTableHeader_IsSemiBoldOnItsOwnBand()
    {
        var window = new WorklogWindow { Width = 900, Height = 620 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        Border header = window.GetVisualDescendants().OfType<Border>().Single(b => b.Classes.Contains("worklog-table-header"));
        Assert.NotNull(header.Background);
        Assert.Equal(new Thickness(0, 0, 0, 1), header.BorderThickness);
        var labels = header.GetVisualDescendants().OfType<TextBlock>().ToList();
        Assert.Equal(["Start", "End", "Duration", "Application", "Window", "Project"], labels.Select(t => t.Text));
        Assert.All(labels, t => Assert.Equal(FontWeight.SemiBold, t.FontWeight));
        window.Close();
    }

    [Fact]
    public async Task Appearance_UsesOpacityWordingAndHoverHintsInsteadOfLooseHelperText()
    {
        var manager = new ThemeManager();
        manager.InitializeThemeResources();
        var vm = new SettingsWindowViewModel(
            new SettingsProviderStub(), new LinuxAutoStartServiceStub(), new ThemeService(), manager, new Silent());
        for (int i = 0; i < 40 && !vm.IsSettingsLoaded; i++)
        {
            await Task.Delay(25);
        }

        vm.SelectedTabIndex = 2;
        var window = new SettingsWindow { DataContext = vm, Width = 640, Height = 540 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        var texts = window.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? string.Empty).ToList();
        Assert.Contains("Overall opacity", texts);
        Assert.DoesNotContain(texts, t => t.Contains("Overall fade", StringComparison.Ordinal));
        Assert.DoesNotContain(texts, t => t.StartsWith("Appearance changes preview", StringComparison.Ordinal));
        Assert.Equal("Overall opacity", AutomationProperties.GetName(window.GetVisualDescendants().OfType<Slider>().Last()));

        var hints = window.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("help-hint")).ToList();
        Assert.Contains(hints, h => AutomationProperties.GetName(h) == "About theme changes"
            && ToolTip.GetTip(h)?.ToString()?.StartsWith("Appearance changes preview", StringComparison.Ordinal) == true);
        Assert.Contains(hints, h => AutomationProperties.GetName(h) == "About color formats");
        window.Close();
    }

    [Fact]
    public async Task RuleActions_AreLightAndSmall()
    {
        var provider = new SettingsProviderStub();
        Settings settings = await provider.LoadAsync();
        settings.ProjectRules.Add(new ProjectRule("code", "*repo*", "Alpha"));
        await provider.SaveAsync(settings);
        var manager = new ThemeManager();
        manager.InitializeThemeResources();
        var vm = new SettingsWindowViewModel(provider, new LinuxAutoStartServiceStub(), new ThemeService(), manager, new Silent());
        for (int i = 0; i < 40 && !vm.IsSettingsLoaded; i++)
        {
            await Task.Delay(25);
        }

        vm.SelectedTabIndex = 1;
        var window = new SettingsWindow { DataContext = vm, Width = 640, Height = 540 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        List<Button> actions = window.GetVisualDescendants().OfType<Button>().Where(b => b.Classes.Contains("rule-action")).ToList();
        Assert.Equal(3, actions.Count);
        Button regular = window.GetVisualDescendants().OfType<Button>().First(b => !b.Classes.Contains("rule-action") && !b.Classes.Contains("color-swatch"));
        Assert.All(actions, a =>
        {
            Assert.True(a.FontSize < regular.FontSize, "Rule actions use smaller text than regular buttons.");
            var chrome = a.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ContentPresenter>().First(p => p.Name == "PART_ContentPresenter");
            Assert.Equal(Colors.Transparent, ((ISolidColorBrush)chrome.Background!).Color);
            Assert.Equal(Colors.Transparent, ((ISolidColorBrush)chrome.BorderBrush!).Color);
            Assert.True(a.MinWidth >= 24 && a.MinHeight >= 24, "The click target stays at least 24 px.");
        });
        window.Close();
    }

    [Fact]
    public void ColorPicker_LabelsAboveTheFieldsShareOneLook()
    {
        var manager = new ThemeManager();
        manager.InitializeThemeResources();
        var window = new ColorPickerWindow();
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        string[] expected = ["H", "S %", "V %", "Hex", "R", "G", "B"];
        var labels = window.GetVisualDescendants().OfType<TextBlock>().Where(t => expected.Contains(t.Text)).ToList();
        Assert.Equal(expected.Order(), labels.Select(t => t.Text!).Order());
        Assert.All(labels, t => Assert.Contains("SettingLabel", t.Classes));
        Assert.Single(labels.Select(t => t.FontSize).Distinct());
        Assert.Single(labels.Select(t => ((ISolidColorBrush)t.Foreground!).Color).Distinct());
        window.Close();
    }

    [Fact]
    public void Widget_ProjectRowKeepsAGapAndCentersTheText_AndCompactButtonsTouch()
    {
        var full = new FullModeView();
        var compact = new CompactModeView();
        var host = new Window { Content = new StackPanel { Children = { full, compact } }, Width = 500, Height = 400 };
        host.Show();
        Dispatcher.UIThread.RunJobs();
        host.UpdateLayout();

        TextBox project = full.GetVisualDescendants().OfType<TextBox>().Single();
        Assert.True(project.Margin.Left >= 8, "The project box keeps a gap to its label.");
        Assert.Equal(VerticalAlignment.Center, project.VerticalContentAlignment);

        StackPanel controls = compact.GetVisualDescendants().OfType<StackPanel>().Single(p => p.Name == "ControlsLayer");
        Assert.Equal(0, controls.Spacing);
        Assert.Equal(Orientation.Vertical, controls.Orientation);
        Assert.Equal(VerticalAlignment.Center, controls.VerticalAlignment);
        Assert.All(controls.Children.OfType<Button>(), b =>
        {
            Assert.Equal(new Thickness(0), b.Margin);
            Assert.Equal(VerticalAlignment.Stretch, b.VerticalAlignment);
            Assert.Equal(0, b.MinWidth);
            Assert.Equal(0, b.MinHeight);
            Assert.Contains("compact-button", b.Classes);
        });
        host.Close();
    }

    private sealed class Silent : FocusTimer.Core.Interfaces.IAppLogger
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
}
