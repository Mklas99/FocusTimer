using FocusTimer.Core.Interfaces;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using System.Diagnostics;
using System.Linq;

namespace FocusTimer.Platform.Windows;

/// <summary>
/// Windows implementation of notification service.
/// Uses lightweight in-app toast windows to avoid creating a second tray icon.
/// </summary>
public class WindowsNotificationService : INotificationService
{
    public Task ShowBreakReminderAsync(string message)
    {
        return ShowNotificationAsync("Break Reminder", message);
    }

    public Task ShowNotificationAsync(string title, string message)
    {
        try
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                ShowToastWindow(title, message);
            }
            else
            {
                Dispatcher.UIThread.Post(() => ShowToastWindow(title, message));
            }

            Debug.WriteLine($"[Notification] {title}: {message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Notification failed: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    private static void ShowToastWindow(string title, string message)
    {
        var toastWindow = new Window
        {
            Width = 360,
            Height = 100,
            CanResize = false,
            ShowInTaskbar = false,
            Topmost = true,
            SystemDecorations = SystemDecorations.None,
            Background = new SolidColorBrush(Color.Parse("#1F2937")),
            Content = new Border
            {
                Padding = new Thickness(14),
                Child = new StackPanel
                {
                    Spacing = 6,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = title,
                            FontWeight = FontWeight.SemiBold,
                            Foreground = Brushes.White,
                            FontSize = 14
                        },
                        new TextBlock
                        {
                            Text = message,
                            Foreground = new SolidColorBrush(Color.Parse("#E5E7EB")),
                            TextWrapping = TextWrapping.Wrap,
                            FontSize = 12
                        }
                    }
                }
            }
        };

        PositionToastNearBottomRight(toastWindow);

        toastWindow.Show();

        var closeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };

        closeTimer.Tick += (_, _) =>
        {
            closeTimer.Stop();
            toastWindow.Close();
        };

        closeTimer.Start();
    }

    private static void PositionToastNearBottomRight(Window toastWindow)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        var referenceWindow = desktop.Windows.FirstOrDefault(window => window.IsVisible) ?? desktop.MainWindow;
        var screens = referenceWindow?.Screens;
        var workingArea = screens?.Primary?.WorkingArea;

        if (workingArea == null)
        {
            return;
        }

        var x = workingArea.Value.Right - (int)toastWindow.Width - 20;
        var y = workingArea.Value.Bottom - (int)toastWindow.Height - 20;
        toastWindow.Position = new PixelPoint(x, y);
    }

}
