namespace FocusTimer.App.Views
{
    using Avalonia.Controls;
    using Avalonia.Interactivity;

    /// <summary>
    /// Represents the settings window for the FocusTimer application.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsWindow"/> class.
        /// </summary>
        public SettingsWindow()
        {
            this.InitializeComponent();
        }

        private void OnOkClicked(object? sender, RoutedEventArgs e)
        {
            // Close the window after OK command completes
            this.Close();
        }
    }
}
