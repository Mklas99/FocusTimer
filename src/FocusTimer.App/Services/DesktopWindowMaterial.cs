namespace FocusTimer.App.Services
{
    using System;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Platform;
    using Avalonia.Threading;

    /// <summary>
    /// Gives a Settings or Worklog window the dense-frost shell when the platform verifiably blurs behind it,
    /// and the solid fallback otherwise. The decision follows the transparency level the window actually
    /// received, the active theme, and the operating system's contrast and transparency preferences.
    /// </summary>
    public static class DesktopWindowMaterial
    {
        /// <summary>
        /// Class set on the window while the dense-frost background applies.
        /// </summary>
        public const string FrostActiveClass = "frost-active";

        /// <summary>
        /// Resource set by <see cref="ThemeManager"/> when the active theme must never use transparency.
        /// </summary>
        public const string ForceSolidResourceKey = "DesktopMaterialForceSolid";

        /// <summary>
        /// Defines the material the window is currently painted with.
        /// </summary>
        public static readonly AttachedProperty<DesktopMaterialState> StateProperty =
            AvaloniaProperty.RegisterAttached<Window, Window, DesktopMaterialState>("State");

        /// <summary>
        /// Defines the one-line description of the requested and achieved material.
        /// </summary>
        public static readonly AttachedProperty<string> DescriptionProperty =
            AvaloniaProperty.RegisterAttached<Window, Window, string>("Description", string.Empty);

        /// <summary>
        /// Requests the blur level and keeps the window's material in step with the platform.
        /// </summary>
        /// <param name="window">The window to manage.</param>
        public static void Attach(Window window)
        {
            window.TransparencyLevelHint = DesktopMaterialPolicy.RequestedLevels;
            var subscription = new Subscription(window);
            window.Opened += subscription.OnOpened;
            window.Closed += subscription.OnClosed;
        }

        /// <summary>
        /// Gets the material the window is currently painted with.
        /// </summary>
        /// <param name="window">The window.</param>
        /// <returns>The current material.</returns>
        public static DesktopMaterialState GetState(Window window) => window.GetValue(StateProperty);

        /// <summary>
        /// Gets a one-line description of the requested and achieved material.
        /// </summary>
        /// <param name="window">The window.</param>
        /// <returns>The description, or an empty string before the window is opened.</returns>
        public static string GetDescription(Window window) => window.GetValue(DescriptionProperty);

        /// <summary>
        /// Recomputes the material from the current platform facts.
        /// </summary>
        /// <param name="window">The window.</param>
        public static void Refresh(Window window)
        {
            var inputs = new DesktopMaterialInputs(
                window.ActualTransparencyLevel,
                IsForcedSolid(),
                window.PlatformSettings?.GetColorValues().ContrastPreference == ColorContrastPreference.High,
                WindowsTransparencySetting.IsDisabled());
            DesktopMaterialState state = DesktopMaterialPolicy.Resolve(inputs);
            window.Classes.Set(FrostActiveClass, state == DesktopMaterialState.DenseFrost);
            window.SetValue(StateProperty, state);
            window.SetValue(DescriptionProperty, DesktopMaterialPolicy.Describe(inputs, state));
        }

        private static bool IsForcedSolid() =>
            Application.Current?.Resources.TryGetResource(ForceSolidResourceKey, null, out object? value) == true &&
            value is true;

        private sealed class Subscription
        {
            private readonly Window _window;
            private IPlatformSettings? _platformSettings;

            public Subscription(Window window)
            {
                this._window = window;
                window.PropertyChanged += this.OnWindowPropertyChanged;
            }

            public void OnOpened(object? sender, EventArgs e)
            {
                this._platformSettings = this._window.PlatformSettings;
                if (this._platformSettings != null)
                {
                    this._platformSettings.ColorValuesChanged += this.OnColorValuesChanged;
                }

                if (Application.Current != null)
                {
                    Application.Current.ResourcesChanged += this.OnResourcesChanged;
                }

                this._window.Activated += this.OnActivated;
                Refresh(this._window);
            }

            public void OnClosed(object? sender, EventArgs e)
            {
                this._window.Opened -= this.OnOpened;
                this._window.Closed -= this.OnClosed;
                this._window.PropertyChanged -= this.OnWindowPropertyChanged;
                this._window.Activated -= this.OnActivated;
                if (this._platformSettings != null)
                {
                    this._platformSettings.ColorValuesChanged -= this.OnColorValuesChanged;
                }

                if (Application.Current != null)
                {
                    Application.Current.ResourcesChanged -= this.OnResourcesChanged;
                }
            }

            private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
            {
                if (e.Property == TopLevel.ActualTransparencyLevelProperty && this._window.IsLoaded)
                {
                    Refresh(this._window);
                }
            }

            private void OnColorValuesChanged(object? sender, PlatformColorValues e) =>
                Dispatcher.UIThread.Post(() => Refresh(this._window));

            private void OnActivated(object? sender, EventArgs e) => Refresh(this._window);

            private void OnResourcesChanged(object? sender, ResourcesChangedEventArgs e) => Refresh(this._window);
        }
    }
}
