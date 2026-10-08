namespace FocusTimer.App.HeadlessTests;

using System.Reflection;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FocusTimer.App.Controls;
using FocusTimer.App.Services;
using FocusTimer.App.ViewModels;
using FocusTimer.App.Views;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;
using FocusTimer.Core.Stubs;
using Material.Icons.Avalonia;

/// <summary>
/// The icon-and-underline navigation, shared control geometry, persistent rule labels, and palette-group
/// disclosure of the Settings window, checked on the real control templates under the application styles.
/// </summary>
public sealed class DesktopShellNavigationTests
{
    private static readonly string[] SettingsTabNames = ["General", "Logging", "Appearance", "Hotkeys", "About"];

    [Fact]
    public async Task ThemeDropdown_ShowsThePresetChosenFromTheControl()
    {
        (SettingsWindow window, SettingsWindowViewModel vm) = await OpenSettingsAsync();
        vm.SelectedTabIndex = 2;
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        ComboBox dropdown = window.GetVisualDescendants().OfType<ComboBox>().Single(c => AutomationProperties.GetName(c) == "Theme preset");
        Assert.Equal(vm.SelectedThemeName, dropdown.SelectedItem);
        foreach (string name in new ThemeService().BuiltInThemes.Select(t => t.ThemeName).Reverse())
        {
            dropdown.SelectedItem = name;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            Assert.Equal(name, vm.Settings.ActiveThemeName);
            Assert.Equal(name, vm.SelectedThemeName);
            Assert.Equal(name, dropdown.SelectedItem);
            Assert.Contains(dropdown.GetVisualDescendants().OfType<TextBlock>(), t => t.IsEffectivelyVisible && t.Text == name);
        }

        window.Close();
    }

    public DesktopShellNavigationTests() => HeadlessAvaloniaFixture.EnsureInitialized();

    [Theory]
    [InlineData(500, 400)]
    [InlineData(640, 540)]
    public async Task Settings_FeedbackSharesTheActionRowAndLongErrorsStayBounded(int width, int height)
    {
        (SettingsWindow window, SettingsWindowViewModel vm) = await OpenSettingsAsync();
        window.Width = width;
        window.Height = height;
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        var status = window.FindControl<TextBlock>("SaveStatus")!;
        var actions = window.FindControl<StackPanel>("FooterActions")!;
        Point statusOrigin = status.TranslatePoint(default, window)!.Value;
        Point actionOrigin = actions.TranslatePoint(default, window)!.Value;
        Assert.True(statusOrigin.X + status.Bounds.Width < actionOrigin.X);
        Assert.True(statusOrigin.Y >= actionOrigin.Y);
        Assert.True(statusOrigin.Y + status.Bounds.Height <= actionOrigin.Y + actions.Bounds.Height);
        Rect normalActions = new(actionOrigin, actions.Bounds.Size);

        typeof(SettingsWindowViewModel).GetProperty(nameof(SettingsWindowViewModel.CommitError))!
            .SetValue(vm, string.Join(" ", Enumerable.Repeat("Settings could not be saved. Try again.", 100)));
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Assert.Equal(normalActions, new Rect(actions.TranslatePoint(default, window)!.Value, actions.Bounds.Size));
        var feedback = window.FindControl<StackPanel>("FooterFeedback")!;
        var scroll = Assert.IsType<ScrollViewer>(feedback.Parent);
        Assert.True(scroll.Bounds.Height <= 120);
        Assert.True(scroll.Extent.Height > scroll.Viewport.Height);
        Assert.True(actions.TranslatePoint(default, window)!.Value.Y + actions.Bounds.Height <= window.Bounds.Height);
        window.Close();
    }

    [Fact]
    public async Task SettingsTabs_AreOrderedNamedAndShowOutlinedIconsBesideLabels()
    {
        (SettingsWindow window, _) = await OpenSettingsAsync();

        TabControl tabs = window.FindControl<TabControl>("DraftPanel")!;
        var items = tabs.Items.OfType<TabItem>().ToList();

        Assert.Equal(SettingsTabNames, items.Select(AutomationProperties.GetName));
        Assert.All(items, item =>
        {
            var header = Assert.IsType<TabHeader>(item.Header);
            Assert.EndsWith("Outline", header.Icon.ToString(), StringComparison.Ordinal);
            Assert.Equal(AutomationProperties.GetName(item), header.Text);
            Assert.NotNull(item.GetVisualDescendants().OfType<MaterialIcon>().FirstOrDefault());
        });
        window.Close();
    }

    [Fact]
    public async Task SelectingATab_ShowsItsContentAndKeepsTheDeveloperUnlockOnAbout()
    {
        (SettingsWindow window, SettingsWindowViewModel vm) = await OpenSettingsAsync();
        TabControl tabs = window.FindControl<TabControl>("DraftPanel")!;

        for (int i = 0; i < SettingsTabNames.Length; i++)
        {
            vm.SelectedTabIndex = i;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(SettingsTabNames[i], AutomationProperties.GetName((TabItem)tabs.SelectedItem!));
        }

        Assert.Equal("About", AutomationProperties.GetName((TabItem)tabs.SelectedItem!));
        Assert.False(vm.IsDeveloperModeVisible);
        for (int i = 0; i < 7; i++)
        {
            vm.RegisterVersionInfoClick();
        }

        Assert.True(vm.IsDeveloperModeVisible);
        window.Close();
    }

    [Fact]
    public async Task Keyboard_ArrowKeysMoveFocusAcrossTabsAndSpaceSelectsTheFocusedOne()
    {
        (SettingsWindow window, SettingsWindowViewModel vm) = await OpenSettingsAsync();
        TabControl tabs = window.FindControl<TabControl>("DraftPanel")!;
        var items = tabs.Items.OfType<TabItem>().ToList();
        items[0].Focus();
        Dispatcher.UIThread.RunJobs();

        window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
        window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.Same(items[2], window.FocusManager!.GetFocusedElement());
        window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(2, vm.SelectedTabIndex);
        Assert.True(items[2].IsSelected);
        window.Close();
    }

    [Fact]
    public async Task SelectedTab_UsesSelectedBackgroundAsUnderlineAndAReadableAccentForLabelAndIcon()
    {
        var theme = new ThemeService().BuiltInThemes.First(t => t.ThemeName == "Dark").Clone();
        theme.AccentPrimary = "#FF0000";
        theme.TabSelectedBackground = "#00FF00";
        theme.TabSelectedText = "#FFFF00";
        theme.TabText = "#AAAAAA";
        theme.TabBackground = "#222222";
        theme.TabHoverBackground = "#333333";
        (SettingsWindow window, SettingsWindowViewModel vm) = await OpenSettingsAsync(theme);
        TabControl tabs = window.FindControl<TabControl>("DraftPanel")!;
        vm.SelectedTabIndex = 2;
        Dispatcher.UIThread.RunJobs();
        var selected = (TabItem)tabs.SelectedItem!;
        var other = tabs.Items.OfType<TabItem>().First(t => !t.IsSelected);

        Assert.Equal(Color.Parse("#00FF00"), Underline(selected));
        Assert.Equal(Colors.Transparent, Underline(other));
        Assert.NotEqual(Color.Parse(theme.AccentPrimary), Underline(selected));
        window.TryFindResource("TabSelectedLabelBrush", out object? selectedLabel);
        Color label = Assert.IsAssignableFrom<ISolidColorBrush>(selectedLabel).Color;
        Assert.True(ThemeContrast.Ratio(label, Color.Parse(theme.SettingsBackground)) >= ThemeContrast.TextRatio);
        Assert.Equal(label, ((ISolidColorBrush)selected.GetVisualDescendants().OfType<MaterialIcon>().First().Foreground!).Color);
        Assert.Equal(label, ((ISolidColorBrush)selected.Foreground!).Color);
        Assert.NotEqual(Color.Parse(theme.TabSelectedText), ((ISolidColorBrush)selected.Foreground!).Color);
        Assert.Equal(Color.Parse("#AAAAAA"), ((ISolidColorBrush)other.GetVisualDescendants().OfType<MaterialIcon>().First().Foreground!).Color);
        Assert.Equal(Colors.Transparent, ((ISolidColorBrush)Root(selected).Background!).Color);

        // The role stays live: changing the selected background while Settings is open recolors the underline.
        theme.TabSelectedBackground = "#0000FF";
        new ThemeManager().ApplyTheme(theme);
        Dispatcher.UIThread.RunJobs();
        window.TryFindResource("DesktopTabSelectedBrush", out object? selectedUnderline);
        Assert.Equal(Assert.IsAssignableFrom<ISolidColorBrush>(selectedUnderline).Color, Underline(selected));
        Assert.True(ThemeContrast.Ratio(Underline(selected), Color.Parse(theme.SettingsBackground)) >= 3);

        // The theme file contract is unchanged: the serialized fields round-trip without migration.
        Theme reloaded = System.Text.Json.JsonSerializer.Deserialize<Theme>(System.Text.Json.JsonSerializer.Serialize(theme))!;
        Assert.Equal("#0000FF", reloaded.TabSelectedBackground);
        Assert.Equal("#FFFF00", reloaded.TabSelectedText);
        window.Close();
    }

    [Fact]
    public async Task Underline_ReservesTheSameSpaceInEveryStateAndFocusUsesADistinctRing()
    {
        (SettingsWindow window, SettingsWindowViewModel vm) = await OpenSettingsAsync();
        TabControl tabs = window.FindControl<TabControl>("DraftPanel")!;
        var items = tabs.Items.OfType<TabItem>().ToList();
        double heightBefore = items[0].Bounds.Height;

        vm.SelectedTabIndex = 3;
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        Assert.All(items, item => Assert.Equal(heightBefore, item.Bounds.Height));
        Assert.All(items, item => Assert.Equal(new Thickness(0, 0, 0, 3), Root(item).BorderThickness));

        TabItem focused = items[1];
        var states = (IPseudoClasses)typeof(StyledElement).GetProperty("PseudoClasses", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(focused)!;
        states.Set(":focus-visible", true);
        Dispatcher.UIThread.RunJobs();
        Border ring = focused.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_FocusRing");

        Assert.NotEqual(Colors.Transparent, ((ISolidColorBrush)ring.BorderBrush!).Color);
        Assert.Equal(Colors.Transparent, Underline(focused));
        Assert.NotEqual(((ISolidColorBrush)ring.BorderBrush!).Color, Underline(items[3]));
        window.Close();
    }

    [Fact]
    public async Task WorklogTabs_KeepTheirOrderAndGainIconsAndNames()
    {
        var window = new WorklogWindow();
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var items = window.FindControl<TabControl>("Tabs")!.Items.OfType<TabItem>().ToList();

        Assert.Equal(["Entries", "Timeline", "Summary"], items.Select(AutomationProperties.GetName));
        Assert.All(items, item => Assert.IsType<TabHeader>(item.Header));
        window.Close();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task RuleEditors_KeepPersistentLabelsAndNamedActionsForPopulatedRules()
    {
        var provider = new SettingsProviderStub();
        Settings settings = await provider.LoadAsync();
        settings.ProjectRules.Add(new ProjectRule("code", "*repo*", "Alpha"));
        settings.ExclusionRules.Add(new WindowMatchRule("keepass", null));
        await provider.SaveAsync(settings);
        (SettingsWindow window, SettingsWindowViewModel vm) = await OpenSettingsAsync(provider: provider);

        vm.SelectedTabIndex = 1;
        Dispatcher.UIThread.RunJobs();
        ProjectRuleListEditor project = window.GetVisualDescendants().OfType<ProjectRuleListEditor>().Single();
        string[] projectLabels = project.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? string.Empty).ToArray();
        Assert.Contains("Application pattern", projectLabels);
        Assert.Contains("Window title pattern", projectLabels);
        Assert.Contains("Project", projectLabels);
        Assert.Equal("code", project.GetVisualDescendants().OfType<TextBox>().First().Text);
        Assert.Equal(
            ["Move rule up", "Move rule down", "Remove rule"],
            project.GetVisualDescendants().OfType<Button>().Where(b => b.Classes.Contains("rule-action")).Select(AutomationProperties.GetName));
        Assert.All(project.GetVisualDescendants().OfType<TextBox>(), box => Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetName(box))));

        vm.SelectedTabIndex = 4;
        for (int i = 0; i < 7; i++)
        {
            vm.RegisterVersionInfoClick();
        }

        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        window.GetVisualDescendants().OfType<Expander>().Single(e => (string?)e.Header == "Developer Options").IsExpanded = true;
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        WindowRuleListEditor exclusions = window.GetVisualDescendants().OfType<WindowRuleListEditor>().First();
        string[] exclusionLabels = exclusions.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? string.Empty).ToArray();
        Assert.Contains("Application pattern", exclusionLabels);
        Assert.Contains("Window title pattern", exclusionLabels);
        Assert.All(
            exclusions.GetVisualDescendants().OfType<Button>().Where(b => b.Classes.Contains("rule-action")),
            button => Assert.True(button.MinHeight >= 28 && button.MinWidth >= 28));
        window.Close();
    }

    [Fact]
    public async Task DesktopControls_ShareSoftRoundedGeometryWhileSwatchesKeepTheirFill()
    {
        (SettingsWindow window, SettingsWindowViewModel vm) = await OpenSettingsAsync();
        vm.SelectedTabIndex = 2;
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        foreach (Expander expander in window.GetVisualDescendants().OfType<Expander>())
        {
            expander.IsExpanded = true;
        }

        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        Button ok = window.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, "OK"));
        Assert.Equal(new CornerRadius(8), ok.CornerRadius);
        Assert.True(ok.Bounds.Height >= 25);
        Assert.All(
            window.GetVisualDescendants().OfType<ComboBox>().Where(c => c.IsEffectivelyVisible),
            combo => Assert.Equal(new CornerRadius(8), combo.CornerRadius));
        Assert.All(
            window.GetVisualDescendants().OfType<TextBox>().Where(t => t.IsEffectivelyVisible && t.FindAncestorOfType<NumericUpDown>() == null),
            box => Assert.Equal(new CornerRadius(8), box.CornerRadius));

        Button swatch = window.GetVisualDescendants().OfType<Button>().First(b => b.Classes.Contains("color-swatch"));
        var presenter = swatch.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ContentPresenter>().First();
        Assert.Equal(((ISolidColorBrush)swatch.Background!).Color, ((ISolidColorBrush)presenter.Background!).Color);
        window.Close();
    }

    [Fact]
    public async Task PaletteCards_SitBesideEachOtherAndFlagAnInvalidColorInTheirHeader()
    {
        (SettingsWindow window, SettingsWindowViewModel vm) = await OpenSettingsAsync();
        vm.SelectedTabIndex = 2;
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Border widget = window.FindControl<Border>("WidgetPaletteCard")!;
        Border dialogs = window.FindControl<Border>("DialogPaletteCard")!;

        Assert.True(widget.Bounds.Width > 0 && Math.Abs(widget.Bounds.Width - dialogs.Bounds.Width) < 1);
        Assert.Equal(widget.TranslatePoint(default, window)!.Value.Y, dialogs.TranslatePoint(default, window)!.Value.Y);
        Assert.Equal("Widget colors", vm.WidgetPaletteHeader);
        Assert.Contains(window.GetVisualDescendants().OfType<TextBox>(), t => t.Text == vm.Settings.Theme.InputBackground);

        vm.Settings.Theme.TimerText = "not-a-color";
        vm.Settings.Theme.InputBorder = "bad";
        Dispatcher.UIThread.RunJobs();

        Assert.True(vm.HasInvalidWidgetPalette);
        Assert.Contains("invalid", vm.WidgetPaletteHeader, StringComparison.Ordinal);
        Assert.Equal("not-a-color", vm.Settings.Theme.TimerText);

        await ((ReactiveUI.ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit>)vm.ApplyCommand).Execute().ToTask();
        Dispatcher.UIThread.RunJobs();
        Assert.True(vm.HasCommitError);

        vm.Settings.Theme.TimerText = "#FFFFFF";
        Dispatcher.UIThread.RunJobs();
        Assert.False(vm.HasInvalidWidgetPalette);
        Assert.True(vm.HasInvalidDialogPalette);
        Assert.Contains("invalid", vm.DialogPaletteHeader, StringComparison.Ordinal);
        window.Close();
    }

    [Fact]
    public async Task Options_AreIndentedUnderTheirHeader_AndThemeToolsStayFlushWithTheCards()
    {
        (SettingsWindow window, SettingsWindowViewModel vm) = await OpenSettingsAsync();
        vm.SelectedTabIndex = 0;
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        TextBlock header = window.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "Startup");
        CheckBox option = window.GetVisualDescendants().OfType<CheckBox>().First();
        double headerX = header.TranslatePoint(default, window)!.Value.X;
        Assert.True(option.TranslatePoint(default, window)!.Value.X - headerX >= 10, "checkbox options sit inside their header");

        vm.SelectedTabIndex = 2;
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        ComboBox themes = window.GetVisualDescendants().OfType<ComboBox>().First();
        Border card = window.FindControl<Border>("WidgetPaletteCard")!;
        Assert.Equal(card.TranslatePoint(default, window)!.Value.X, themes.TranslatePoint(default, window)!.Value.X, 1);
        window.Close();
    }

    [Fact]
    public async Task EveryPage_HasTheSameInsetOnBothSides()
    {
        (SettingsWindow window, SettingsWindowViewModel vm) = await OpenSettingsAsync();
        for (int tab = 0; tab < SettingsTabNames.Length; tab++)
        {
            vm.SelectedTabIndex = tab;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            var content = (StackPanel)window.GetVisualDescendants().OfType<ScrollViewer>()
                .First(v => v.Classes.Contains("settings-scroll") && v.IsEffectivelyVisible).Content!;
            Assert.Equal(12, content.Margin.Left);
            Assert.Equal(content.Margin.Left, content.Margin.Right);
        }

        window.Close();
        var worklog = new WorklogWindow();
        worklog.Show();
        Dispatcher.UIThread.RunJobs();
        foreach (TabItem tab in worklog.FindControl<TabControl>("Tabs")!.Items.OfType<TabItem>())
        {
            var view = (Control)tab.Content!;
            Assert.Equal(12, view.Margin.Left);
            Assert.Equal(view.Margin.Left, view.Margin.Right);
        }

        worklog.Close();
    }

    [Fact]
    public async Task RuleRows_AreMarkedWarningWhenBlankAndDangerWhenIncompleteAndBlankRowsDisappearOnApply()
    {
        (SettingsWindow window, SettingsWindowViewModel vm) = await OpenSettingsAsync();
        vm.SelectedTabIndex = 1;
        vm.ProjectList.Load([new ProjectRule("code", null, "Alpha")]);
        vm.ProjectList.Rules.Add(new ProjectRuleItemViewModel());
        vm.ProjectList.Rules.Add(new ProjectRuleItemViewModel(new ProjectRule("mail", null, null)));
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        var rows = window.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("rule-row")).ToList();

        Assert.Equal(3, rows.Count);
        Assert.False(rows[0].Classes.Contains("warning") || rows[0].Classes.Contains("danger"));
        Assert.Contains("warning", rows[1].Classes);
        Assert.Contains("danger", rows[2].Classes);

        await ((ReactiveUI.ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit>)vm.ApplyCommand).Execute().ToTask();
        Assert.False(vm.LastApplySucceeded); // the half-filled rule still blocks the commit
        Assert.Equal(3, vm.ProjectList.Rules.Count);

        vm.ProjectList.Rules[2].ProjectName = "Mail";
        await ((ReactiveUI.ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit>)vm.ApplyCommand).Execute().ToTask();
        Dispatcher.UIThread.RunJobs();
        Assert.True(vm.LastApplySucceeded);
        Assert.Equal(["Alpha", "Mail"], vm.ProjectList.Rules.Select(r => r.ProjectName));
        window.Close();
    }

    [Fact]
    public async Task Accordion_UsesOneTemplateWithOneRotatingChevron()
    {
        (SettingsWindow window, SettingsWindowViewModel vm) = await OpenSettingsAsync();
        for (int i = 0; i < 7; i++)
        {
            vm.RegisterVersionInfoClick();
        }

        vm.SelectedTabIndex = 4;
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Expander developer = window.GetVisualDescendants().OfType<Expander>().Single(e => (string?)e.Header == "Developer Options");
        developer.IsExpanded = true;
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        Assert.Single(developer.GetVisualDescendants().OfType<ToggleButton>());
        Assert.Single(developer.GetVisualDescendants().OfType<MaterialIcon>(), i => i.Name == "Chevron");
        Assert.DoesNotContain(developer.GetVisualDescendants(), v => v is Avalonia.Controls.Shapes.Path { Name: "ExpandCollapseChevron" });
        Assert.NotNull(developer.GetVisualDescendants().OfType<MaterialIcon>().Single(i => i.Name == "Chevron").RenderTransform);
        window.Close();
    }

    [Fact]
    public async Task Changelog_DropsItsOwnTitleLine_AndColorEditorsExcludeStatusColors()
    {
        (SettingsWindow window, SettingsWindowViewModel vm) = await OpenSettingsAsync();
        Assert.DoesNotContain("# Changelog", vm.ChangelogContent.Split((char)10).First(), StringComparison.OrdinalIgnoreCase);

        vm.SelectedTabIndex = 2;
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        string[] tags = window.GetVisualDescendants().OfType<Button>().Where(b => b.Classes.Contains("color-swatch")).Select(b => (string)b.Tag!).ToArray();
        Assert.DoesNotContain("SuccessColor", tags);
        Assert.DoesNotContain("WarningColor", tags);
        Assert.DoesNotContain("DangerColor", tags);
        Assert.DoesNotContain("InputFocusBorder", tags);
        Assert.Contains("InputBackground", tags);
        window.Close();
    }

    [Fact]
    public async Task Window_UsesTheFrostBackgroundOnlyWhileFrostIsActive()
    {
        (SettingsWindow window, _) = await OpenSettingsAsync();

        Assert.Equal(DesktopMaterialState.SolidFallback, DesktopWindowMaterial.GetState(window));
        Assert.DoesNotContain(DesktopWindowMaterial.FrostActiveClass, window.Classes);
        Assert.Equal(1.0, ((ISolidColorBrush)window.Background!).Opacity);

        window.Classes.Add(DesktopWindowMaterial.FrostActiveClass);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(ThemeManager.DesktopShellFrostOpacity, ((ISolidColorBrush)window.Background!).Opacity);

        window.Classes.Remove(DesktopWindowMaterial.FrostActiveClass);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1.0, ((ISolidColorBrush)window.Background!).Opacity);
        Assert.Equal(DesktopMaterialPolicy.RequestedLevels, window.TransparencyLevelHint);
        window.Close();
    }

    private static Border Root(TabItem item) =>
        item.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_LayoutRoot");

    private static Color Underline(TabItem item) =>
        ((ISolidColorBrush)Root(item).BorderBrush!).Color;

    private static async Task<(SettingsWindow Window, SettingsWindowViewModel ViewModel)> OpenSettingsAsync(
        Theme? theme = null, SettingsProviderStub? provider = null)
    {
        var manager = new ThemeManager();
        manager.InitializeThemeResources();
        if (theme != null)
        {
            manager.ApplyTheme(theme);
        }

        var vm = new SettingsWindowViewModel(
            provider ?? new SettingsProviderStub(), new LinuxAutoStartServiceStub(), new ThemeService(), manager, new SilentLogger());
        for (int i = 0; i < 40 && !vm.IsSettingsLoaded; i++)
        {
            await Task.Delay(25);
        }

        var window = new SettingsWindow { DataContext = vm, Width = 640, Height = 540 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return (window, vm);
    }

    private sealed class SilentLogger : FocusTimer.Core.Interfaces.IAppLogger
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

internal static class TaskObservableHelpers
{
    public static Task<T> ToTask<T>(this IObservable<T> observable) =>
        System.Reactive.Threading.Tasks.TaskObservableExtensions.ToTask(observable);
}
