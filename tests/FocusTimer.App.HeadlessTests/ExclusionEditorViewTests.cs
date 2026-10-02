namespace FocusTimer.App.HeadlessTests;

using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FocusTimer.App.Services;
using FocusTimer.App.ViewModels;
using FocusTimer.App.Views;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;
using FocusTimer.Core.Stubs;

/// <summary>The exclusion editor lives in Developer Options and is only reachable once developer mode is unlocked.</summary>
public sealed class ExclusionEditorViewTests
{
    public ExclusionEditorViewTests() => HeadlessAvaloniaFixture.EnsureInitialized();

    [Fact]
    public async Task EditorIsHiddenWhileLockedAndBoundWhenUnlocked()
    {
        var manager = new ThemeManager();
        manager.InitializeThemeResources();
        var provider = new SettingsProviderStub();
        var settings = await provider.LoadAsync();
        settings.ExclusionRules.Add(new WindowMatchRule("keepass", null));
        await provider.SaveAsync(settings);
        var vm = new SettingsWindowViewModel(
            provider, new LinuxAutoStartServiceStub(), new ThemeService(), manager, new StubLogger());
        for (var i = 0; i < 20 && !vm.IsSettingsLoaded; i++) await Task.Delay(25);
        var window = new SettingsWindow { DataContext = vm };
        window.Show();
        vm.SelectedTabIndex = 3; // About
        Dispatcher.UIThread.RunJobs();

        var expander = window.GetVisualDescendants().OfType<Expander>().Single(e => (string?)e.Header == "Developer Options");
        Assert.False(expander.IsVisible);
        Assert.Single(vm.ExclusionRules);

        for (var i = 0; i < 7; i++) vm.RegisterVersionInfoClick();
        Dispatcher.UIThread.RunJobs();
        Assert.True(expander.IsVisible);
        expander.IsExpanded = true;
        Dispatcher.UIThread.RunJobs();
        var list = window.GetVisualDescendants().OfType<ItemsControl>().Single(c => c.Name == "ExclusionRuleList");
        Assert.Same(vm.ExclusionRules, list.ItemsSource);
        window.Close();
    }

    private sealed class StubLogger : FocusTimer.Core.Interfaces.IAppLogger
    {
        public void LogCritical(string message, Exception? ex = null) { }
        public void LogDebug(string message) { }
        public void LogError(string message, Exception? ex = null) { }
        public void LogInformation(string message) { }
        public void LogWarning(string message) { }
    }
}
