namespace FocusTimer.App.Controls
{
    using System;
    using System.ComponentModel;
    using Avalonia.Controls;
    using Avalonia.Input;
    using FocusTimer.App.ViewModels;

    /// <summary>
    /// Compact hue and saturation/value color picker surface.
    /// </summary>
    public partial class CompactColorPicker : UserControl
    {
        private bool _isDraggingSurface;
        private bool _isDraggingHue;
        private bool _isDraggingAlpha;
        private ColorPickerWindowViewModel? _viewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompactColorPicker"/> class.
        /// </summary>
        public CompactColorPicker()
        {
            this.InitializeComponent();

            this.DataContextChanged += this.OnDataContextChanged;
            this.SaturationValueSurface.SizeChanged += this.OnPickerSizeChanged;
            this.HueStrip.SizeChanged += this.OnPickerSizeChanged;
            this.AlphaStrip.SizeChanged += this.OnPickerSizeChanged;

            this.AttachViewModel(this.DataContext as ColorPickerWindowViewModel);
            this.UpdateThumbPositions();
        }

        private void OnSaturationValuePointerPressed(object? sender, PointerPressedEventArgs e)
        {
            this._isDraggingSurface = true;
            e.Pointer.Capture(this.SaturationValueSurface);
            this.UpdateSaturationValue(e.GetPosition(this.SaturationValueSurface));
        }

        private void OnSaturationValuePointerMoved(object? sender, PointerEventArgs e)
        {
            if (!this._isDraggingSurface)
            {
                return;
            }

            this.UpdateSaturationValue(e.GetPosition(this.SaturationValueSurface));
        }

        private void OnSurfacePointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            this._isDraggingSurface = false;
            e.Pointer.Capture(null);
        }

        private void OnSurfacePointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            this._isDraggingSurface = false;
        }

        private void OnHuePointerPressed(object? sender, PointerPressedEventArgs e)
        {
            this._isDraggingHue = true;
            e.Pointer.Capture(this.HueStrip);
            this.UpdateHue(e.GetPosition(this.HueStrip));
        }

        private void OnHuePointerMoved(object? sender, PointerEventArgs e)
        {
            if (!this._isDraggingHue)
            {
                return;
            }

            this.UpdateHue(e.GetPosition(this.HueStrip));
        }

        private void OnHuePointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            this._isDraggingHue = false;
            e.Pointer.Capture(null);
        }

        private void OnHuePointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            this._isDraggingHue = false;
        }

        private void OnAlphaPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            this._isDraggingAlpha = true;
            e.Pointer.Capture(this.AlphaStrip);
            this.UpdateAlpha(e.GetPosition(this.AlphaStrip));
        }

        private void OnAlphaPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!this._isDraggingAlpha)
            {
                return;
            }

            this.UpdateAlpha(e.GetPosition(this.AlphaStrip));
        }

        private void OnAlphaPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            this._isDraggingAlpha = false;
            e.Pointer.Capture(null);
        }

        private void OnAlphaPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            this._isDraggingAlpha = false;
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            this.AttachViewModel(this.DataContext as ColorPickerWindowViewModel);
            this.UpdateThumbPositions();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ColorPickerWindowViewModel.Hue)
                or nameof(ColorPickerWindowViewModel.Saturation)
                or nameof(ColorPickerWindowViewModel.Value)
                or nameof(ColorPickerWindowViewModel.Alpha)
                or null)
            {
                this.UpdateThumbPositions();
            }
        }

        private void OnPickerSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            this.UpdateThumbPositions();
        }

        private void AttachViewModel(ColorPickerWindowViewModel? viewModel)
        {
            if (ReferenceEquals(this._viewModel, viewModel))
            {
                return;
            }

            if (this._viewModel is not null)
            {
                this._viewModel.PropertyChanged -= this.OnViewModelPropertyChanged;
            }

            this._viewModel = viewModel;

            if (this._viewModel is not null)
            {
                this._viewModel.PropertyChanged += this.OnViewModelPropertyChanged;
            }
        }

        private void UpdateSaturationValue(Avalonia.Point point)
        {
            if (this._viewModel is null)
            {
                return;
            }

            var width = Math.Max(this.SaturationValueSurface.Bounds.Width, 1);
            var height = Math.Max(this.SaturationValueSurface.Bounds.Height, 1);

            this._viewModel.Saturation = Math.Clamp(point.X / width, 0, 1);
            this._viewModel.Value = 1 - Math.Clamp(point.Y / height, 0, 1);
        }

        private void UpdateHue(Avalonia.Point point)
        {
            if (this._viewModel is null)
            {
                return;
            }

            var height = Math.Max(this.HueStrip.Bounds.Height, 1);
            this._viewModel.Hue = Math.Clamp(point.Y / height, 0, 1) * 360;
        }

        private void UpdateAlpha(Avalonia.Point point)
        {
            if (this._viewModel is null)
            {
                return;
            }

            var height = Math.Max(this.AlphaStrip.Bounds.Height, 1);
            this._viewModel.Alpha = Math.Clamp(point.Y / height, 0, 1) * 255;
        }

        private void UpdateThumbPositions()
        {
            if (this._viewModel is null)
            {
                return;
            }

            var surfaceWidth = Math.Max(this.SaturationValueSurface.Bounds.Width, 1);
            var surfaceHeight = Math.Max(this.SaturationValueSurface.Bounds.Height, 1);
            Canvas.SetLeft(this.SaturationValueThumb, Math.Clamp(this._viewModel.Saturation * surfaceWidth, 0, surfaceWidth));
            Canvas.SetTop(this.SaturationValueThumb, Math.Clamp((1 - this._viewModel.Value) * surfaceHeight, 0, surfaceHeight));

            var hueHeight = Math.Max(this.HueStrip.Bounds.Height, 1);
            Canvas.SetLeft(this.HueThumb, 0);
            Canvas.SetTop(this.HueThumb, Math.Clamp((this._viewModel.Hue / 360) * hueHeight, 0, hueHeight));

            var alphaHeight = Math.Max(this.AlphaStrip.Bounds.Height, 1);
            Canvas.SetLeft(this.AlphaThumb, 0);
            Canvas.SetTop(this.AlphaThumb, Math.Clamp((this._viewModel.Alpha / 255) * alphaHeight, 0, alphaHeight));
        }
    }
}
