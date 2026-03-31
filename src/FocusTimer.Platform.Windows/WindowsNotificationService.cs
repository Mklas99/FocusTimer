namespace FocusTimer.Platform.Windows
{
    using System.Linq;
    using System.Threading.Tasks;
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
        public Task ShowBreakReminderAsync(string message, bool requireAcknowledgement)
        {
            try
            {
                if (Dispatcher.UIThread.CheckAccess())
                {
                    return this.ShowBreakReminderWindowAsync(message, requireAcknowledgement);
                }

                var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
                Dispatcher.UIThread.Post(async () =>
                {
                    try
                    {
                        await this.ShowBreakReminderWindowAsync(message, requireAcknowledgement);
                        completion.TrySetResult(null);
                    }
                    catch (Exception ex)
                    {
                        this._logger?.LogError("Break reminder failed.", ex);
                        completion.TrySetResult(null);
                    }
                });

                return completion.Task;
            }
            catch (Exception ex)
            {
                this._logger?.LogError("Break reminder failed.", ex);
                return Task.CompletedTask;
            }
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

        private static IBrush GetThemeBrush(string resourceKey, string fallbackColor)
        {
            if (Application.Current?.Resources.TryGetResource(resourceKey, null, out var resource) == true && resource is IBrush brush)
            {
                return brush;
            }

            return new SolidColorBrush(Color.Parse(fallbackColor));
        }

        private Task ShowBreakReminderWindowAsync(string message, bool requireAcknowledgement)
        {
            var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var backgroundBrush = GetThemeBrush("SettingsBackgroundBrush", "#1F2937");
            var borderBrush = GetThemeBrush("WindowBorderBrush", "#374151");
            var titleBrush = GetThemeBrush("SettingsSectionHeaderBrush", "#FFFFFF");
            var bodyBrush = GetThemeBrush("SettingsLabelTextBrush", "#E5E7EB");
            var accentBrush = GetThemeBrush("AccentPrimaryBrush", "#0078D7");

            var messageBlock = new TextBlock
            {
                Text = message,
                Foreground = bodyBrush,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
            };

            var contentPanel = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Break Reminder",
                        FontWeight = FontWeight.SemiBold,
                        Foreground = titleBrush,
                        FontSize = 14,
                    },
                    messageBlock,
                },
            };

            var toastWindow = new Window
            {
                Width = 380,
                Height = requireAcknowledgement ? 140 : 110,
                CanResize = false,
                ShowInTaskbar = false,
                Topmost = true,
                SystemDecorations = SystemDecorations.None,
                Background = backgroundBrush,
            };

            if (requireAcknowledgement)
            {
                var acknowledgeButton = new Button
                {
                    Content = "OK",
                    Width = 80,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 6, 0, 0),
                    Background = accentBrush,
                    Foreground = titleBrush,
                    BorderThickness = new Thickness(0),
                };
                acknowledgeButton.Click += (_, _) => toastWindow.Close();
                contentPanel.Children.Add(acknowledgeButton);
            }

            toastWindow.Content = new Border
            {
                Padding = new Thickness(14),
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                Child = contentPanel,
            };

            toastWindow.Closed += (_, _) => completion.TrySetResult(null);

            PositionToastNearBottomRight(toastWindow);
            toastWindow.Show();

            if (!requireAcknowledgement)
            {
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

            this._logger?.LogDebug($"Break reminder shown (requires acknowledgement: {requireAcknowledgement}).");
            return completion.Task;
        }

        private void ShowToastWindow(string title, string message)
        {
            var backgroundBrush = GetThemeBrush("SettingsBackgroundBrush", "#1F2937");
            var borderBrush = GetThemeBrush("WindowBorderBrush", "#374151");
            var titleBrush = GetThemeBrush("SettingsSectionHeaderBrush", "#FFFFFF");
            var bodyBrush = GetThemeBrush("SettingsLabelTextBrush", "#E5E7EB");

            var toastWindow = new Window
            {
                Width = 360,
                Height = 100,
                CanResize = false,
                ShowInTaskbar = false,
                Topmost = true,
                SystemDecorations = SystemDecorations.None,
                Background = backgroundBrush,
                Content = new Border
                {
                    Padding = new Thickness(14),
                    BorderBrush = borderBrush,
                    BorderThickness = new Thickness(1),
                    Child = new StackPanel
                    {
                        Spacing = 6,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = title,
                                FontWeight = FontWeight.SemiBold,
                                Foreground = titleBrush,
                                FontSize = 14,
                            },
                            new TextBlock
                            {
                                Text = message,
                                Foreground = bodyBrush,
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
