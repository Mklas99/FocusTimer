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

    [Theory]
    [InlineData(NotificationSeverity.Information, 5)]
    [InlineData(NotificationSeverity.Warning, 15)]
    [InlineData(NotificationSeverity.Error, 15)]
    public void Severity_SelectsDismissalDelay(NotificationSeverity severity, int seconds) =>
        Assert.Equal(TimeSpan.FromSeconds(seconds), DesktopNotificationService.GetDismissalDelay(severity));

    [Theory]
    [InlineData(NotificationSeverity.Warning, "DesktopWarningBrush")]
    [InlineData(NotificationSeverity.Error, "DesktopDangerBrush")]
    public void Severity_UsesLiveThemeColorsAndAnExplicitLabel(NotificationSeverity severity, string resource)
    {
        var manager = new ThemeManager();
        var themes = new ThemeService();
        manager.ApplyTheme(themes.GetBuiltInTheme("Dark")!);
        var window = new NotificationWindow("Focus Timer", "An operation needs attention.", false, severity);
        window.Show();
        foreach (var theme in FocusTimer.Tests.ThemeFixtures.All)
        {
            manager.ApplyTheme(theme);
            Dispatcher.UIThread.RunJobs();
            Assert.True(Application.Current!.TryGetResource(resource, null, out object? brush));
            var expected = Assert.IsAssignableFrom<ISolidColorBrush>(brush);
            var card = window.FindControl<Border>("NotificationCard")!;
            var heading = window.FindControl<TextBlock>("Heading")!;
            Assert.Equal(expected.Color, Assert.IsAssignableFrom<ISolidColorBrush>(card.BorderBrush).Color);
            Assert.Equal(expected.Color, Assert.IsAssignableFrom<ISolidColorBrush>(heading.Foreground).Color);
            Assert.Equal(new Thickness(1), card.BorderThickness);
            Assert.StartsWith($"{severity}:", heading.Text);
            Assert.True(ThemeContrast.Ratio(expected.Color, Assert.IsAssignableFrom<ISolidColorBrush>(card.Background).Color) >= 4.5);
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
            Assert.Equal(Assert.IsAssignableFrom<ISolidColorBrush>(Application.Current!.Resources["DesktopShellBrush"]).Color, background.Color);
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
