namespace FocusTimer.App.Views
{
    using Avalonia.Controls;
    using Avalonia.Input;
    using Avalonia.Interactivity;
    using FocusTimer.App.ViewModels;

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

        private async void OnColorPreviewClicked(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: string propertyName } || this.DataContext is not SettingsWindowViewModel viewModel)
            {
                return;
            }

            var dialog = new ColorPickerWindow
            {
                DataContext = new ColorPickerWindowViewModel(viewModel.GetThemeColor(propertyName)),
            };

            var selectedColor = await dialog.ShowDialog<string?>(this);
            if (!string.IsNullOrWhiteSpace(selectedColor))
            {
                viewModel.SetThemeColor(propertyName, selectedColor);
            }
        }

        private void OnVersionInfoPressed(object? sender, PointerPressedEventArgs e)
        {
            if (this.DataContext is SettingsWindowViewModel viewModel)
            {
                viewModel.RegisterVersionInfoClick();
            }
        }

        private void OnOkClicked(object? sender, RoutedEventArgs e)
        {
            // Close the window after OK command completes
            this.Close();
        }
    }
}
