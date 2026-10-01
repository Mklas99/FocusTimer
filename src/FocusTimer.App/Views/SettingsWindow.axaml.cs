namespace FocusTimer.App.Views
{
    using System.Reactive;
    using System.Reactive.Threading.Tasks;
    using Avalonia.Controls;
    using Avalonia.Input;
    using Avalonia.Interactivity;
    using FocusTimer.App.ViewModels;
    using ReactiveUI;

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
            this.Closing += this.OnSettingsClosing;
            this.Closed += this.OnSettingsClosed;
        }

        private void OnSettingsClosing(object? sender, WindowClosingEventArgs e)
        {
            (this.DataContext as SettingsWindowViewModel)?.RestoreAppearancePreview();
        }

        private void OnSettingsClosed(object? sender, System.EventArgs e)
        {
            (this.DataContext as SettingsWindowViewModel)?.Dispose();
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

            string? selectedColor = await dialog.ShowDialog<string?>(this);
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

        private async void OnOkClicked(object? sender, RoutedEventArgs e)
        {
            if (this.DataContext is SettingsWindowViewModel viewModel)
            {
                await ((ReactiveCommand<Unit, Unit>)viewModel.OkCommand).Execute().ToTask();
                if (viewModel.LastApplySucceeded)
                {
                    this.Close();
                }
            }
        }
    }
}
