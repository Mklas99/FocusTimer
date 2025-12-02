using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FocusTimer.App.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void OnOkClicked(object? sender, RoutedEventArgs e)
    {
        // Close the window after OK command completes
        Close();
    }
}
