namespace FocusTimer.Platform.Windows
{
    using System.Linq;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Controls.ApplicationLifetimes;
    using Avalonia.Layout;
    using Avalonia.Media;
    using Avalonia.Threading;
    using FocusTimer.Core.Interfaces;

    /// <summary>
    /// Windows implementation of notification service.
    /// Uses lightweight in-app toast windows to avoid creating a second tray icon.
    /// </summary>
    public class WindowsNotificationService : INotificationService
    {
        private readonly IAppLogger? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsNotificationService"/> class.
        /// </summary>
        public WindowsNotificationService()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsNotificationService"/> class with a logger.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public WindowsNotificationService(IAppLogger? logger)
        {
            this._logger = logger;
        }

        /// <inheritdoc/>
        public Task ShowBreakReminderAsync(string message)
        {
            return this.ShowNotificationAsync("Break Reminder", message);
        }

        /// <inheritdoc/>
        public Task ShowNotificationAsync(string title, string message)
        {
            try
            {
                if (Dispatcher.UIThread.CheckAccess())
                {
                    this.ShowToastWindow(title, message);
                }
                else
                {
                    Dispatcher.UIThread.Post(() => this.ShowToastWindow(title, message));
                }

                this._logger?.LogInformation($"[Notification] {title}: {message}");
            }
            catch (Exception ex)
            {
                this._logger?.LogError("Notification failed.", ex);
            }

            return Task.CompletedTask;
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

        private void ShowToastWindow(string title, string message)
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
                                FontSize = 14,
                            },
                            new TextBlock
                            {
                                Text = message,
                                Foreground = new SolidColorBrush(Color.Parse("#E5E7EB")),
                                TextWrapping = TextWrapping.Wrap,
                                FontSize = 12,
                            },
                        },
                    },
                },
            };

            PositionToastNearBottomRight(toastWindow);

            toastWindow.Show();
            this._logger?.LogDebug("Toast notification displayed.");

            var closeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5),
            };

            closeTimer.Tick += (_, _) =>
            {
                closeTimer.Stop();
                toastWindow.Close();
            };

            closeTimer.Start();
        }
    }
}
