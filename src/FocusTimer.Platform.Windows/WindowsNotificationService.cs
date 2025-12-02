using FocusTimer.Core.Interfaces;
using System.Diagnostics;

namespace FocusTimer.Platform.Windows;

/// <summary>
/// Windows implementation of notification service.
/// For now uses Debug output. TODO: Implement Windows Toast Notifications or balloon tips.
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
            // For now, just log to debug output
            // TODO: Implement actual Windows notifications using one of:
            // 1. Windows.UI.Notifications (Toast notifications - requires Windows App SDK or WinRT)
            // 2. System.Windows.Forms.NotifyIcon.ShowBalloonTip (requires WinForms reference)
            // 3. Custom notification window using Avalonia
            
            Debug.WriteLine($"[Notification] {title}: {message}");
            
            // Optional: Show a simple message box for break reminders (intrusive but functional)
            // System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Notification failed: {ex.Message}");
        }

        return Task.CompletedTask;
    }
}
