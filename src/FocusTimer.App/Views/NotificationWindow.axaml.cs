namespace FocusTimer.App.Views;

using Avalonia.Controls;
using Avalonia.Interactivity;
using FocusTimer.Core.Models;

/// <summary>App-owned notification chrome shared with the desktop editors.</summary>
public partial class NotificationWindow : Window
{
    /// <summary>Initializes a new instance of the <see cref="NotificationWindow"/> class.</summary>
    public NotificationWindow()
        : this("Focus Timer", string.Empty, false)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="NotificationWindow"/> class.</summary>
    /// <param name="title">The notification title.</param>
    /// <param name="message">The message.</param>
    /// <param name="requireAcknowledgement">Whether the notification stays open until dismissed.</param>
    /// <param name="severity">The urgency of the notification.</param>
    public NotificationWindow(string title, string message, bool requireAcknowledgement, NotificationSeverity severity = NotificationSeverity.Information)
    {
        this.InitializeComponent();
        this.Title = title;
        this.Heading.Text = severity == NotificationSeverity.Information ? title : $"{severity}: {title}";
        this.Classes.Set("warning", severity == NotificationSeverity.Warning);
        this.Classes.Set("error", severity == NotificationSeverity.Error);
        this.Message.Text = message;
        this.Acknowledge.IsVisible = requireAcknowledgement;
        this.Dismiss.IsVisible = !requireAcknowledgement;
        this.ShowActivated = requireAcknowledgement;
    }

    private void OnDismiss(object? sender, RoutedEventArgs e) => this.Close();
}
