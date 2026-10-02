namespace FocusTimer.App.Views
{
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Controls.Primitives;
    using Avalonia.Input;
    using Avalonia.Interactivity;
    using Avalonia.Markup.Xaml;
    using Avalonia.VisualTree;
    using FocusTimer.App.Services;
    using FocusTimer.App.ViewModels;
    using FocusTimer.Core;
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Timer widget window for displaying and interacting with the focus timer.
    /// </summary>
    public partial class TimerWidgetWindow : Window
    {
        private readonly ThemeManager _themeManager;
        private bool _isDragging = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerWidgetWindow"/> class for the Avalonia runtime loader.
        /// </summary>
        public TimerWidgetWindow()
            : this(new ThemeManager())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerWidgetWindow"/> class.
        /// </summary>
        /// <param name="themeManager">Shared theme manager for appearance preview.</param>
        public TimerWidgetWindow(ThemeManager themeManager)
        {
            this._themeManager = themeManager;
            this.InitializeComponent();
            this._themeManager.ThemeApplied += this.OnThemeApplied;
            this.PropertyChanged += this.OnWindowPropertyChanged;
            this.Opened += this.OnWindowOpenedForTransparency;
            if (this._themeManager.ActiveTheme is Theme activeTheme)
            {
                this.ApplyBackdrop(activeTheme);
            }

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
            this._themeManager.ThemeApplied -= this.OnThemeApplied;
            this.PropertyChanged -= this.OnWindowPropertyChanged;
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
            Border? dragArea = this.FindControl<Border>("WidgetSurface");
            if (dragArea != null)
            {
                dragArea.Cursor = new Cursor(StandardCursorType.DragMove);
            }
        }

        private static bool IsInteractiveElement(object? source)
        {
            Avalonia.Visual? current = source as Avalonia.Visual;
            while (current != null)
            {
                if (current is Button
                    or ToggleButton
                    or TextBox
                    or Slider
                    or ComboBox
                    or CheckBox
                    or NumericUpDown)
                {
                    return true;
                }

                current = current.GetVisualParent();
            }

            return false;
        }

        private void OnThemeApplied(Theme theme) => this.ApplyBackdrop(theme);

        private void ApplyBackdrop(Theme theme)
        {
            var levels = WidgetBackdropLevels.ForTheme(theme);
            if (!this.TransparencyLevelHint.SequenceEqual(levels))
            {
                this.TransparencyLevelHint = levels;
            }

            this._themeManager.ReportActualWidgetTransparency(this.ActualTransparencyLevel);
        }

        private void OnWindowOpenedForTransparency(object? sender, EventArgs e) =>
            this._themeManager.ReportActualWidgetTransparency(this.ActualTransparencyLevel);

        private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == TopLevel.ActualTransparencyLevelProperty)
            {
                this._themeManager.ReportActualWidgetTransparency(this.ActualTransparencyLevel);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
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
                    IGlobalHotkeyService? hotkeyService = (this.DataContext as TimerWidgetViewModel)?.HotkeyService;
                    Services.AppController? appController = AppHost.Services.GetService<Services.AppController>();

                    if (hotkeyService != null)
                    {
                        System.Reflection.MethodInfo? setHandle = hotkeyService.GetType().GetMethod("SetWindowHandle");
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
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                return;
            }

            // Keep controls clickable while allowing drag from most of the surface.
            if (IsInteractiveElement(e.Source))
            {
                return;
            }

            this._isDragging = true;
            if (sender is InputElement dragSurface)
            {
                dragSurface.Cursor = new Cursor(StandardCursorType.SizeAll);
            }

            this.BeginMoveDrag(e);
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
