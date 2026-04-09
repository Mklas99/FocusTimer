namespace FocusTimer.App.Views
{
    using Avalonia.Controls;
    using Avalonia.Interactivity;
    using FocusTimer.App.ViewModels;

    /// <summary>
    /// Lightweight dialog for selecting a theme color.
    /// </summary>
    public partial class ColorPickerWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ColorPickerWindow"/> class.
        /// </summary>
        public ColorPickerWindow()
        {
            this.InitializeComponent();
        }

        private void OnOkClicked(object? sender, RoutedEventArgs e)
        {
            var selectedColor = (this.DataContext as ColorPickerWindowViewModel)?.SelectedColorHex;
            this.Close(selectedColor);
        }

        private void OnCancelClicked(object? sender, RoutedEventArgs e)
        {
            this.Close(null);
        }
    }
}
