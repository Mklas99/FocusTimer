using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using FocusTimer.App.ViewModels;
using Avalonia.Interactivity;
using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;

namespace FocusTimer.App.Views;

public partial class TimerWidgetWindow : Window
{
    public TimerWidgetWindow()
    {
        InitializeComponent();
        Opened += OnWindowOpened;

        // Handle closing event to hide instead of close
        Closing += OnWindowClosing;
        
        // Setup Windows-specific hotkey message handling
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Opened += OnWindowOpenedForHotkeys;
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
        if (DataContext is TimerWidgetViewModel viewModel)
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
            if (TryGetPlatformHandle()?.Handle is IntPtr hwnd && hwnd != IntPtr.Zero)
            {
                var hotkeyService = Program.Services.GetService<Core.Interfaces.IHotkeyService>();
                
                if (hotkeyService is Platform.Windows.WindowsHotkeyService windowsHotkeyService)
                {
                    windowsHotkeyService.SetWindowHandle(hwnd);
                    System.Diagnostics.Debug.WriteLine($"Window handle set for hotkeys: {hwnd}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to setup hotkey window handle: {ex.Message}");
        }
    }

    /// <summary>
    /// Clean up the ViewModel when window closes.
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is TimerWidgetViewModel viewModel)
        {
            viewModel.Dispose();
        }
        base.OnClosed(e);
    }

    // Handler for draggable area
    private void WindowDragArea_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    // Handler for minimize button
    private void MinimizeButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        // Prevent the window from actually closing
        // Instead, just hide it (unless app is shutting down)
        if (!IsAppShuttingDown)
        {
            e.Cancel = true;
            Hide();
        }
    }

    /// <summary>
    /// Flag to allow actual window close during app shutdown.
    /// Set this to true before calling Close() during app exit.
    /// </summary>
    public bool IsAppShuttingDown { get; set; }

    private void OnToggleProjectInputClicked(object? sender, RoutedEventArgs e)
    {
        // Handled by command binding
    }
}
