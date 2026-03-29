namespace FocusTimer.App.Views
{
    using System;
    using System.Runtime.InteropServices;
    using Avalonia.Controls;
    using Avalonia.Input;
    using Avalonia.Interactivity;
    using Avalonia.Markup.Xaml;
    using FocusTimer.App.ViewModels;
    using FocusTimer.Core;
    using FocusTimer.Core.Interfaces;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Timer widget window for displaying and interacting with the focus timer.
    /// </summary>
    public partial class TimerWidgetWindow : Window
    {
        private bool _isDragging = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerWidgetWindow"/> class.
        /// </summary>
        public TimerWidgetWindow()
        {
            this.InitializeComponent();
            this.Opened += this.OnWindowOpened;

            // Handle closing event to hide instead of close
            this.Closing += this.OnWindowClosing;

            // Setup Windows-specific hotkey message handling
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                this.Opened += this.OnWindowOpenedForHotkeys;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether flag to allow actual window close during app shutdown.
        /// Set this to true before calling Close() during app exit.
        /// </summary>
        public bool IsAppShuttingDown { get; set; }

        // Instance-based logger via DataContext (TimerWidgetViewModel exposes Logger)

        /// <summary>
        /// Clean up the ViewModel when window closes.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected override void OnClosed(EventArgs e)
        {
            if (this.DataContext is TimerWidgetViewModel viewModel)
            {
                viewModel.Dispose();
            }

            base.OnClosed(e);
        }

        /// <inheritdoc/>
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            this._isDragging = false;
            var dragArea = this.FindControl<Border>("DragArea");
            if (dragArea != null)
            {
                dragArea.Cursor = new Cursor(StandardCursorType.DragMove);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Initialize settings after window is opened.
        /// </summary>
        private async void OnWindowOpened(object? sender, EventArgs e)
        {
            if (this.DataContext is TimerWidgetViewModel viewModel)
            {
                await viewModel.InitializeSettingsAsync();
            }
        }

        /// <summary>
        /// Setup Windows hotkey service with window handle.
        /// </summary>
        private void OnWindowOpenedForHotkeys(object? sender, EventArgs e)
        {
            try
            {
                // Get the native window handle
                if (this.TryGetPlatformHandle()?.Handle is IntPtr hwnd && hwnd != IntPtr.Zero)
                {
                    var hotkeyService = (this.DataContext as TimerWidgetViewModel)?.HotkeyService;
                    var appController = AppHost.Services.GetService<Services.AppController>();

                    if (hotkeyService != null)
                    {
                        var setHandle = hotkeyService.GetType().GetMethod("SetWindowHandle");
                        if (setHandle != null)
                        {
                            setHandle.Invoke(hotkeyService, new object[] { hwnd });
                            appController?.RegisterHotkeys();
                            (this.DataContext as TimerWidgetViewModel)?.Logger?.LogDebug($"Window handle set for hotkeys: {hwnd}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                (this.DataContext as TimerWidgetViewModel)?.Logger?.LogError("Failed to set up hotkey window handle.", ex);
            }
        }

        // Set cursor to Grab when pointer enters drag area (if not dragging)
        private void DragArea_PointerEnter(object? sender, PointerEventArgs e)
        {
            if (!this._isDragging && sender is Border dragArea)
            {
                dragArea.Cursor = new Cursor(StandardCursorType.DragMove);
            }
        }

        // Set cursor to SizeAll when pointer leaves drag area (if not dragging)
        private void DragArea_PointerLeave(object? sender, PointerEventArgs e)
        {
            if (!this._isDragging && sender is Border dragArea)
            {
                dragArea.Cursor = new Cursor(StandardCursorType.Arrow);
            }
        }

        // Handler for draggable area
        private void WindowDragArea_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                this._isDragging = true;
                if (sender is Border dragArea)
                {
                    dragArea.Cursor = new Cursor(StandardCursorType.SizeAll);
                }

                this.BeginMoveDrag(e);
            }
        }

        // Handler for minimize button
        private void MinimizeButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
            (this.DataContext as TimerWidgetViewModel)?.Logger?.LogDebug("Timer widget minimized.");
        }

        private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
        {
            // Prevent the window from actually closing
            // Instead, just hide it (unless app is shutting down)
            if (!this.IsAppShuttingDown)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void OnToggleProjectInputClicked(object? sender, RoutedEventArgs e)
        {
            // Handled by command binding
        }
    }
}
