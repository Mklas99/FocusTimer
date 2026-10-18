namespace FocusTimer.App.HeadlessTests;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FocusTimer.App.Services;
using FocusTimer.App.ViewModels;
using FocusTimer.App.Views;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;
using FocusTimer.Core.Stubs;
using FocusTimer.Persistence;

public sealed class WidgetPlacementTests
{
    public WidgetPlacementTests() => HeadlessAvaloniaFixture.EnsureInitialized();

    [Theory]
    [InlineData(false, 0.5)]
    [InlineData(false, 1.0)]
    [InlineData(false, 3.0)]
    [InlineData(true, 0.5)]
    [InlineData(true, 1.0)]
    [InlineData(true, 3.0)]
    public void BothModesPreviewPlacementAndRestoreGeometry(bool compact, double scale)
    {
        var provider = new SettingsProviderStub();
        var logger = new Silent();
        var notifications = new LinuxNotificationServiceStub();
        var tracker = new SessionTracker(new LinuxActiveWindowServiceStub(), logger);
        using var timer = new TimerService(tracker);
        using var reminders = new BreakReminderService(notifications, provider);
        using var vm = new TimerWidgetViewModel(provider, logger, new CsvSessionRepository(provider),
            notifications, tracker, reminders, timer, null, new EventBus());
        vm.ApplySettings(new Settings { UseCompactMode = compact, WidgetScale = scale });
        var manager = new ThemeManager();
        manager.InitializeThemeResources();
        var window = new TimerWidgetWindow(manager) { DataContext = vm };
        window.Show();
        try
        {
            Settle(window);
            Size original = window.Bounds.Size;
            var draft = vm.Settings.Clone();
            draft.WidgetButtonsBelowClock = true;
            vm.PreviewAppearance(draft);
            Settle(window);
            UserControl view = compact
                ? window.GetVisualDescendants().OfType<CompactModeView>().Single()
                : window.GetVisualDescendants().OfType<FullModeView>().Single();
            Grid clock = view.FindControl<Grid>("ClockLayer")!;
            Control controls = view.FindControl<Control>("ControlsLayer")!;
            Point clockStart = clock.TranslatePoint(default, window)!.Value;
            Point controlsStart = controls.TranslatePoint(default, window)!.Value;
            Assert.True(controlsStart.Y >= clockStart.Y + clock.Bounds.Height - 0.1);
            Assert.InRange(Math.Abs((clockStart.X + clock.Bounds.Width / 2) -
                (controlsStart.X + controls.Bounds.Width / 2)), 0, 1.1);
            if (compact)
            {
                Assert.Equal(Orientation.Horizontal, ((StackPanel)controls).Orientation);
            }

            Assert.All(controls.GetVisualDescendants().OfType<Button>(), button =>
            {
                Point start = button.TranslatePoint(default, window)!.Value;
                Assert.True(button.Bounds.Width > 0 && button.Bounds.Height > 0);
                Assert.InRange(start.X, -0.1, window.Bounds.Width);
                Assert.True(start.X + button.Bounds.Width <= window.Bounds.Width + 0.1);
                Assert.True(start.Y + button.Bounds.Height <= window.Bounds.Height + 0.1);
                Assert.Equal(compact ? vm.CompactButtonSize : 24 * scale, button.Bounds.Width, 3);
            });
            Assert.False(vm.Settings.WidgetButtonsBelowClock);
            vm.ClearAppearancePreview();
            Settle(window);
            Assert.Equal(original, window.Bounds.Size);
            Assert.Equal(0, Grid.GetRow(controls));
            Assert.Equal(1, Grid.GetColumn(controls));
            if (compact) Assert.Equal(Orientation.Vertical, ((StackPanel)controls).Orientation);
        }
        finally { window.Close(); }
    }

    private static void Settle(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }

    private sealed class Silent : IAppLogger
    {
        public void LogCritical(string message, Exception? ex = null) { }
        public void LogDebug(string message) { }
        public void LogError(string message, Exception? ex = null) { }
        public void LogInformation(string message) { }
        public void LogWarning(string message) { }
    }
}
