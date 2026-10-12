namespace FocusTimer.App.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Platform;
    using Avalonia.Threading;
    using FocusTimer.Core.Models;

    /// <summary>Connects the widget's one shell to Avalonia's optional public backdrop capability.</summary>
    internal sealed class WidgetWindowMaterial : IDisposable
    {
        private readonly Window _window;
        private readonly Border _shell;
        private readonly ThemeManager _manager;
        private readonly WidgetBackdropCapability _capability = new();
        private readonly IPlatformSettings? _platformSettings;
        private WidgetBackdropCapability.Session? _session;
        private (IntPtr Handle, double Radius, double Opacity, Size Size, bool Enabled)? _lastUpdate;
        private bool _applied;
        private bool _disposed;
        private bool _refreshing;
        private bool _refreshPosted;
        private bool _retryPosted;
        private string? _failureReason;

        /// <summary>Initializes a new instance of the <see cref="WidgetWindowMaterial"/> class.</summary>
        /// <param name="window">The widget window.</param>
        /// <param name="shell">Its single tint and outline border.</param>
        /// <param name="manager">Shared appearance manager.</param>
        internal WidgetWindowMaterial(Window window, Border shell, ThemeManager manager)
        {
            this._window = window;
            this._shell = shell;
            this._manager = manager;
            this._platformSettings = window.PlatformSettings;
            manager.ThemeApplied += this.OnThemeApplied;
            window.PropertyChanged += this.OnWindowPropertyChanged;
            shell.PropertyChanged += this.OnShellPropertyChanged;
            window.Opened += this.OnOpened;
            window.Activated += this.OnActivated;
            window.ScalingChanged += this.OnScalingChanged;
            if (this._platformSettings != null)
            {
                this._platformSettings.ColorValuesChanged += this.OnPreferencesChanged;
            }

            if (OperatingSystem.IsWindows())
            {
                Win32Properties.AddWndProcHookCallback(window, this.OnWindowMessage);
            }

            this.Refresh();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this._disposed)
            {
                return;
            }

            this._disposed = true;
            this._manager.ThemeApplied -= this.OnThemeApplied;
            this._window.PropertyChanged -= this.OnWindowPropertyChanged;
            this._shell.PropertyChanged -= this.OnShellPropertyChanged;
            this._window.Opened -= this.OnOpened;
            this._window.Activated -= this.OnActivated;
            this._window.ScalingChanged -= this.OnScalingChanged;
            if (this._platformSettings != null)
            {
                this._platformSettings.ColorValuesChanged -= this.OnPreferencesChanged;
            }

            if (OperatingSystem.IsWindows())
            {
                Win32Properties.RemoveWndProcHookCallback(this._window, this.OnWindowMessage);
            }

            this.Detach();
        }

        private void Refresh(bool retry = false)
        {
            if (this._disposed || this._refreshing || this._manager.ActiveTheme is not Theme theme)
            {
                return;
            }

            this._refreshing = true;
            try
            {
                if (retry)
                {
                    this._failureReason = null;
                }

                bool highContrast = string.Equals(theme.ThemeName, "High Contrast", StringComparison.OrdinalIgnoreCase) ||
                    this._platformSettings?.GetColorValues().ContrastPreference == ColorContrastPreference.High;
                bool transparencyOff = WindowsTransparencySetting.IsDisabled();
                bool supported = WindowsWidgetComposition.IsSupported();
                bool frost = theme.WidgetBlurMode == WidgetBlurModes.Frosted && !highContrast && !transparencyOff &&
                    supported && this._capability.IsAvailable && this._failureReason == null && this._window.IsVisible;
                IntPtr handle = this._window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                if (this._session?.Handle != handle || !frost)
                {
                    // Remove the acrylic hint before restoring Avalonia's original backdrop policy on disposal.
                    this.SetLevels([WindowTransparencyLevel.Transparent]);
                    this.Detach();
                }

                if (frost && this._session == null && handle != IntPtr.Zero)
                {
                    this._session = this._capability.TryAttach(handle);
                    if (this._session == null)
                    {
                        this._failureReason = "Native backdrop attachment failed";
                    }
                }

                this.SetLevels(this._session != null ? WidgetBackdropLevels.ForTheme(theme) : [WindowTransparencyLevel.Transparent]);
                this._manager.ReportActualWidgetTransparency(this._window.ActualTransparencyLevel);
                var inputs = new WidgetMaterialInputs(
                    theme.WidgetBlurMode,
                    this._window.ActualTransparencyLevel,
                    highContrast,
                    transparencyOff,
                    supported,
                    OperatingSystem.IsWindows(),
                    this._capability.IsAvailable,
                    this._session != null,
                    this._applied);
                if (this._session is { } session)
                {
                    (IntPtr Handle, double Radius, double Opacity, Size Size, bool Enabled) update =
                        (handle, this._shell.CornerRadius.TopLeft * this._window.RenderScaling,
                        this._window.Opacity, this._window.Bounds.Size,
                        this._window.ActualTransparencyLevel == WindowTransparencyLevel.AcrylicBlur);
                    if (this._lastUpdate != update)
                    {
                        this._lastUpdate = update;
                        this._applied = false;
                        bool accepted = session.TryUpdate(
                            handle,
                            (float)update.Radius,
                            (float)update.Opacity,
                            update.Enabled,
                            applied =>
                            {
                                if (this._disposed || this._session != session ||
                                    this._window.TryGetPlatformHandle()?.Handle != session.Handle)
                                {
                                    return;
                                }

                                this._applied = applied;
                                if (!applied)
                                {
                                    this._failureReason = "Native backdrop update failed";
                                }

                                this.Refresh();
                            });
                        if (!accepted)
                        {
                            this._failureReason = "Native backdrop update rejected";
                            this.SetLevels([WindowTransparencyLevel.Transparent]);
                            this.Detach();
                        }
                    }
                }

                var result = WidgetMaterialPolicy.Resolve(inputs with
                {
                    AttachmentSucceeded = this._session != null,
                    UpdateApplied = this._applied,
                });
                if (theme.WidgetBlurMode == WidgetBlurModes.Frosted && this._failureReason != null)
                {
                    result = new(WidgetMaterialState.SolidFallback, this._failureReason);
                }

                this._manager.ReportWidgetMaterial(result);
            }
            finally
            {
                this._refreshing = false;
            }
        }

        private void SetLevels(IReadOnlyList<WindowTransparencyLevel> levels)
        {
            if (!this._window.TransparencyLevelHint.SequenceEqual(levels))
            {
                this._window.TransparencyLevelHint = levels;
            }
        }

        private void Detach()
        {
            this._session?.Dispose();
            this._session = null;
            this._lastUpdate = null;
            this._applied = false;
        }

        private void PostRefresh(bool retry)
        {
            this._retryPosted |= retry;
            if (this._disposed || this._refreshPosted)
            {
                return;
            }

            this._refreshPosted = true;
            Dispatcher.UIThread.Post(
                () =>
                {
                    this._refreshPosted = false;
                    bool recheck = this._retryPosted;
                    this._retryPosted = false;
                    this.Refresh(recheck);
                },
                DispatcherPriority.Loaded);
        }

        private void OnThemeApplied(Theme theme) => this.Refresh(retry: true);

        private void OnOpened(object? sender, EventArgs e) => this.PostRefresh(retry: true);

        private void OnActivated(object? sender, EventArgs e) => this.PostRefresh(retry: true);

        private void OnScalingChanged(object? sender, EventArgs e) => this.Refresh();

        private void OnPreferencesChanged(object? sender, PlatformColorValues e) => this.PostRefresh(retry: true);

        private void OnShellPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Border.CornerRadiusProperty)
            {
                this.Refresh();
            }
        }

        private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Visual.BoundsProperty || e.Property == Visual.OpacityProperty ||
                e.Property == TopLevel.ActualTransparencyLevelProperty || e.Property == Visual.IsVisibleProperty)
            {
                this.Refresh(retry: e.Property == Visual.IsVisibleProperty && this._window.IsVisible);
            }
        }

        private IntPtr OnWindowMessage(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (message is 0x001A or 0x031A or 0x031E or 0x02B1)
            {
                this.PostRefresh(retry: true);
            }

            return IntPtr.Zero;
        }
    }
}
