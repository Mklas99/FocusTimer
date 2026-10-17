namespace FocusTimer.App.HeadlessTests;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FocusTimer.App.Services;
using FocusTimer.App.Views;
using FocusTimer.Core.Services;
using FocusTimer.Core.Models;

public sealed class NotificationWindowTests
{
    public NotificationWindowTests() => HeadlessAvaloniaFixture.EnsureInitialized();

    [Fact]
    public void CommittedPolicyControlsRealToastsAndDoesNotRetargetOpenTimers()
    {
        // Pump the UI dispatcher without moving this window test onto another thread.
        void Complete(Task task)
        {
            var elapsed = System.Diagnostics.Stopwatch.StartNew();
            while (!task.IsCompleted && elapsed.Elapsed < TimeSpan.FromSeconds(3))
            {
                Thread.Sleep(5);
                Dispatcher.UIThread.RunJobs();
            }

            Assert.True(task.IsCompletedSuccessfully);
        }

        var windows = new List<NotificationWindow>();
        using var opened = Window.WindowOpenedEvent.AddClassHandler<NotificationWindow>((window, _) => windows.Add(window));
        var service = new DesktopNotificationService();
        var settings = new Settings
        {
            InformationNotificationsEnabled = false,
            WarningNotificationsEnabled = false,
            InformationDismissalSeconds = 1,
            ErrorDismissalSeconds = 1,
        };
        service.ApplySettings(settings);
        try
        {
            Complete(service.ShowNotificationAsync("Idle", "Paused."));
            Complete(service.ShowNotificationAsync("Warning", "Attention.", NotificationSeverity.Warning));
            Assert.Empty(windows);
            Complete(service.ShowNotificationAsync("Save failed", "Retry.", NotificationSeverity.Error));
            NotificationWindow error = Assert.Single(windows);
            Assert.True(error.IsVisible);
            Task acknowledgement = service.ShowBreakReminderAsync("Take a break.", true);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(2, windows.Count);
            NotificationWindow reminder = windows.Last();
            Assert.True(reminder.FindControl<Button>("Acknowledge")!.IsVisible);
            Assert.False(acknowledgement.IsCompleted);
            Task automatic = service.ShowBreakReminderAsync("Automatic break.", false);
            Dispatcher.UIThread.RunJobs();
            NotificationWindow automaticReminder = windows.Last();
            Assert.Equal(3, windows.Count);
            Assert.True(automaticReminder.IsVisible);
            settings.ErrorDismissalSeconds = 120;
            service.ApplySettings(settings);
            Complete(service.ShowNotificationAsync("Later error", "Retry.", NotificationSeverity.Error));
            NotificationWindow later = windows.Last();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            Dispatcher.UIThread.MainLoop(timeout.Token);

            Assert.False(error.IsVisible);
            Assert.False(automaticReminder.IsVisible);
            Complete(automatic);
            Assert.True(later.IsVisible);
            Assert.True(reminder.IsVisible);
            Assert.False(acknowledgement.IsCompleted);
            reminder.Close();
            Complete(acknowledgement);
        }
        finally
        {
            foreach (NotificationWindow window in windows) window.Close();
        }
    }

    [Theory]
    [InlineData(NotificationSeverity.Information, 5)]
    [InlineData(NotificationSeverity.Warning, 15)]
    [InlineData(NotificationSeverity.Error, 15)]
    public void Severity_SelectsDismissalDelay(NotificationSeverity severity, int seconds) =>
        Assert.Equal(TimeSpan.FromSeconds(seconds), DesktopNotificationService.GetDismissalDelay(severity));

    [Theory]
    [InlineData(NotificationSeverity.Information, "Information")]
    [InlineData(NotificationSeverity.Warning, "Warning")]
    [InlineData(NotificationSeverity.Error, "Error")]
    public void Severity_UsesLiveThemeColorsAndAnExplicitLabel(NotificationSeverity severity, string resource)
    {
        var manager = new ThemeManager();
        var themes = new ThemeService();
        manager.ApplyTheme(themes.GetBuiltInTheme("Dark")!);
        var window = new NotificationWindow("Focus Timer", "An operation needs attention.", false, severity);
        window.Show();
        Color Resolve(string suffix)
        {
            Assert.True(Application.Current!.TryGetResource($"DesktopNotification{resource}{suffix}Brush", null, out object? brush));
            return Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;
        }

        foreach (var theme in FocusTimer.Tests.ThemeFixtures.All)
        {
            manager.ApplyTheme(theme);
            Dispatcher.UIThread.RunJobs();
            var card = window.FindControl<Border>("NotificationCard")!;
            var heading = window.FindControl<TextBlock>("Heading")!;
            var message = window.FindControl<TextBlock>("Message")!;
            Color fill = Resolve("Fill");
            Assert.Equal(fill, Assert.IsAssignableFrom<ISolidColorBrush>(card.Background).Color);
            Assert.Equal(Resolve("Edge"), Assert.IsAssignableFrom<ISolidColorBrush>(card.BorderBrush).Color);
            Assert.Equal(Resolve("Text"), Assert.IsAssignableFrom<ISolidColorBrush>(heading.Foreground).Color);
            Assert.Equal(Resolve("Body"), Assert.IsAssignableFrom<ISolidColorBrush>(message.Foreground).Color);
            Assert.Equal(new Thickness(1), card.BorderThickness);
            if (severity != NotificationSeverity.Information)
            {
                Assert.StartsWith($"{severity}:", heading.Text);
            }

            Assert.True(ThemeContrast.Ratio(Resolve("Text"), fill) >= 4.5);
            Assert.True(ThemeContrast.Ratio(Resolve("Body"), fill) >= 4.5);
        }

        window.Close();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LongMessage_IsScrollableAndActionsStayReachable(bool acknowledgement)
    {
        new ThemeManager().ApplyTheme(new ThemeService().GetBuiltInTheme("Light")!);
        var window = new NotificationWindow("Break Reminder", string.Join("\n", Enumerable.Repeat("A longer message that must remain readable.", 40)), acknowledgement);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Assert.True(window.Bounds.Height <= 480);
        ScrollViewer scroll = window.GetVisualDescendants().OfType<ScrollViewer>().First();
        Assert.True(scroll.Extent.Height > scroll.Viewport.Height);
        Button button = window.FindControl<Button>(acknowledgement ? "Acknowledge" : "Dismiss")!;
        Assert.True(button.IsEffectivelyVisible);
        Point origin = button.TranslatePoint(default, window)!.Value;
        Assert.True(origin.Y + button.Bounds.Height <= window.Bounds.Height);
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.False(window.IsVisible);
    }

    [Fact]
    public void OpenNotification_FollowsLiveThemeChangesWithoutChangingSize()
    {
        var manager = new ThemeManager();
        var service = new ThemeService();
        manager.ApplyTheme(service.GetBuiltInTheme("Dark")!);
        var window = new NotificationWindow("Focus Timer", "Welcome back.", false);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Size size = window.Bounds.Size;
        foreach (var theme in FocusTimer.Tests.ThemeFixtures.All)
        {
            manager.ApplyTheme(theme);
            Dispatcher.UIThread.RunJobs();
            Border card = window.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("notification-card"));
            var background = Assert.IsAssignableFrom<ISolidColorBrush>(card.Background);
            var message = window.FindControl<TextBlock>("Message")!;
            var foreground = Assert.IsAssignableFrom<ISolidColorBrush>(message.Foreground);
            Assert.Equal(Assert.IsAssignableFrom<ISolidColorBrush>(Application.Current!.Resources["DesktopNotificationInformationFillBrush"]).Color, background.Color);
            Assert.True(ThemeContrast.Ratio(foreground.Color, background.Color) >= 4.5);
            if (theme.ThemeName == "High Contrast")
            {
                var outline = Assert.IsAssignableFrom<ISolidColorBrush>(card.BorderBrush);
                Assert.True(ThemeContrast.Ratio(outline.Color, background.Color) >= 3.0);
            }

            Assert.Equal(size, window.Bounds.Size);
        }

        window.Close();
    }
}
