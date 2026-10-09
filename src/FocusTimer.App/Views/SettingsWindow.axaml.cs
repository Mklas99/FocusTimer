namespace FocusTimer.App.Views
{
    using System.ComponentModel;
    using System.Reactive;
    using System.Reactive.Threading.Tasks;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Input;
    using Avalonia.Input.TextInput;
    using Avalonia.Interactivity;
    using Avalonia.VisualTree;
    using FocusTimer.App.Services;
    using FocusTimer.App.ViewModels;
    using ReactiveUI;

    /// <summary>
    /// Represents the settings window for the FocusTimer application.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private SettingsWindowViewModel? _editor;
        private Control? _previousFocus;
        private bool _closingAfterOk;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsWindow"/> class.
        /// </summary>
        public SettingsWindow()
        {
            this.InitializeComponent();
            DesktopWindowMaterial.Attach(this);
            this.PropertyChanged += this.OnMaterialChanged;
            this.Closing += this.OnSettingsClosing;
            this.Closed += this.OnSettingsClosed;
            this.DataContextChanged += this.OnEditorChanged;
            this.AddHandler(InputElement.KeyDownEvent, this.BlockEditingInput, RoutingStrategies.Tunnel);
            this.AddHandler(InputElement.TextInputEvent, this.BlockEditingInput, RoutingStrategies.Tunnel);
            this.AddHandler(InputElement.PointerWheelChangedEvent, this.BlockEditingInput, RoutingStrategies.Tunnel);
            this.AddHandler(TextBox.PastingFromClipboardEvent, this.BlockEditingInput, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
            this.AddHandler(TextBox.CuttingToClipboardEvent, this.BlockEditingInput, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        }

        private void OnMaterialChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == DesktopWindowMaterial.DescriptionProperty)
            {
                this.MaterialStatus.Text = DesktopWindowMaterial.GetDescription(this);
            }
        }

        private void BlockEditingInput(object? sender, RoutedEventArgs e)
        {
            if (this._editor?.IsCommitting == true)
            {
                e.Handled = true;
            }
        }

        private void OnEditorChanged(object? sender, System.EventArgs e)
        {
            if (this._editor != null)
            {
                this._editor.PropertyChanged -= this.OnEditorPropertyChanged;
            }

            this._editor = this.DataContext as SettingsWindowViewModel;
            if (this._editor != null)
            {
                this._editor.PropertyChanged += this.OnEditorPropertyChanged;
            }
        }

        private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(SettingsWindowViewModel.IsCommitting))
            {
                return;
            }

            if (this._editor?.IsCommitting == true)
            {
                this._previousFocus = this.FocusManager?.GetFocusedElement() as Control;
                if (this._previousFocus is TextBox textBox)
                {
                    var request = new TextInputMethodClientRequestedEventArgs
                    {
                        RoutedEvent = InputElement.TextInputMethodClientRequestedEvent,
                    };
                    textBox.RaiseEvent(request);
                    request.Client?.SetPreeditText(string.Empty);
                }

                foreach (var visual in this.DraftPanel.GetVisualDescendants())
                {
                    if (visual is ComboBox comboBox)
                    {
                        comboBox.IsDropDownOpen = false;
                    }

                    if (visual is Control control)
                    {
                        control.ContextMenu?.Close();
                        control.ContextFlyout?.Hide();
                    }
                }

                this.SaveStatus.Focus();
            }
            else if (!this._closingAfterOk && this._previousFocus is { IsEffectivelyEnabled: true } control &&
                     control.GetVisualRoot() == this)
            {
                control.Focus();
                this._previousFocus = null;
            }
        }

        private void OnSettingsClosing(object? sender, WindowClosingEventArgs e)
        {
            if (this.DataContext is SettingsWindowViewModel viewModel)
            {
                if (!viewModel.TryDiscardAndClose())
                {
                    e.Cancel = true;
                    return;
                }
            }
        }

        private void OnSettingsClosed(object? sender, System.EventArgs e)
        {
            if (this._editor != null)
            {
                this._editor.PropertyChanged -= this.OnEditorPropertyChanged;
            }

            this.DataContextChanged -= this.OnEditorChanged;
            this.RemoveHandler(InputElement.KeyDownEvent, this.BlockEditingInput);
            this.RemoveHandler(InputElement.TextInputEvent, this.BlockEditingInput);
            this.RemoveHandler(InputElement.PointerWheelChangedEvent, this.BlockEditingInput);
            this.RemoveHandler(TextBox.PastingFromClipboardEvent, this.BlockEditingInput);
            this.RemoveHandler(TextBox.CuttingToClipboardEvent, this.BlockEditingInput);
            this._previousFocus = null;
            (this.DataContext as SettingsWindowViewModel)?.Dispose();
        }

        private async void OnColorPreviewClicked(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: string propertyName } || this.DataContext is not SettingsWindowViewModel viewModel)
            {
                return;
            }

            if (!viewModel.CanEdit)
            {
                return;
            }

            int generation = viewModel.AppearanceEditGeneration;

            var dialog = new ColorPickerWindow
            {
                DataContext = new ColorPickerWindowViewModel(viewModel.GetThemeColor(propertyName)),
            };

            string? selectedColor = await dialog.ShowDialog<string?>(this);
            if (!string.IsNullOrWhiteSpace(selectedColor) && generation == viewModel.AppearanceEditGeneration)
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
                this._closingAfterOk = true;
                await ((ReactiveCommand<Unit, Unit>)viewModel.OkCommand).Execute().ToTask();
                if (viewModel.LastApplySucceeded)
                {
                    this.Close();
                }
                else
                {
                    this._closingAfterOk = false;
                    this._previousFocus?.Focus();
                    this._previousFocus = null;
                }
            }
        }
    }
}
