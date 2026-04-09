namespace FocusTimer.App.Services
{
    using Avalonia;
    using Avalonia.Media;
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;

    /// <summary>
    /// Manages theme application by updating Avalonia's resource dictionary at runtime.
    /// </summary>
    public class ThemeManager
    {
        private readonly IAppLogger? _logWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemeManager"/> class.
        /// </summary>
        /// <param name="logWriter">Optional logger for error logging.</param>
        public ThemeManager(IAppLogger? logWriter = null)
        {
            this._logWriter = logWriter;
        }

        /// <summary>
        /// Initializes the resource dictionary with default theme color keys.
        /// This should be called once during app startup.
        /// </summary>
        public void InitializeThemeResources()
        {
            if (Application.Current == null)
            {
                return;
            }

            var resources = Application.Current.Resources;
            var defaultTheme = new Theme(); // Uses default values

            // Initialize all color resources
            this.ApplyTheme(defaultTheme);
        }

        /// <summary>
        /// Applies a theme by updating all color resources in the application.
        /// </summary>
        /// <param name="theme">The theme to apply.</param>
        public void ApplyTheme(Theme theme)
        {
            if (Application.Current == null)
            {
                return;
            }

            var resources = Application.Current.Resources;

            // Store opacity values as resources
            resources["BackgroundOpacity"] = theme.BackgroundOpacity;
            resources["TimerOpacity"] = theme.TimerOpacity;
            resources["ButtonOpacity"] = theme.ButtonOpacity;

            // Window Colors (with background opacity applied to brush, but transparent if opacity is 0)
            if (theme.BackgroundOpacity <= 0)
            {
                resources["WindowBackgroundBrush"] = Avalonia.Media.Brushes.Transparent;
            }
            else
            {
                this.UpdateColorResource(resources, "WindowBackgroundColor", theme.WindowBackground, theme.BackgroundOpacity);
                resources["WindowBackgroundBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(theme.WindowBackground), theme.BackgroundOpacity);
            }

            this.UpdateColorResource(resources, "WindowForegroundColor", theme.WindowForeground);
            this.UpdateColorResource(resources, "WindowBorderColor", theme.WindowBorder);

            // Text Colors
            this.UpdateColorResource(resources, "PrimaryTextColor", theme.PrimaryText);
            this.UpdateColorResource(resources, "SecondaryTextColor", theme.SecondaryText);
            this.UpdateColorResource(resources, "DisabledTextColor", theme.DisabledText);

            // Timer Display (with timer opacity applied to text brush)
            this.UpdateColorResource(resources, "TimerTextColor", theme.TimerText, theme.TimerOpacity);
            this.UpdateColorResource(resources, "TimerBackgroundColor", theme.TimerBackground);

            // Buttons & Controls (with button opacity applied to brushes)
            this.UpdateColorResource(resources, "ButtonNormalColor", theme.ButtonNormal, theme.ButtonOpacity);
            this.UpdateColorResource(resources, "ButtonHoverColor", theme.ButtonHover, theme.ButtonOpacity);
            this.UpdateColorResource(resources, "ButtonPressedColor", theme.ButtonPressed, theme.ButtonOpacity);
            this.UpdateColorResource(resources, "ButtonDisabledColor", theme.ButtonDisabled);

            // Accents
            this.UpdateColorResource(resources, "AccentPrimaryColor", theme.AccentPrimary);
            this.UpdateColorResource(resources, "AccentSecondaryColor", theme.AccentSecondary);
            this.UpdateColorResource(resources, "DangerColor", theme.DangerColor);
            this.UpdateColorResource(resources, "SuccessColor", theme.SuccessColor);
            this.UpdateColorResource(resources, "WarningColor", theme.WarningColor);
            this.UpdateFluentAccentResources(resources, theme.AccentPrimary);

            // Input Controls
            this.UpdateColorResource(resources, "InputBackgroundColor", theme.InputBackground);
            this.UpdateColorResource(resources, "InputBorderColor", theme.InputBorder);
            this.UpdateColorResource(resources, "InputFocusBorderColor", theme.AccentPrimary);
            this.UpdateColorResource(resources, "InputTextColor", theme.InputText);

            // Project Tag
            this.UpdateColorResource(resources, "ProjectTagBackgroundColor", theme.ProjectTagBackground);
            this.UpdateColorResource(resources, "ProjectTagBorderColor", theme.ProjectTagBorder);
            this.UpdateColorResource(resources, "ProjectTagTextColor", theme.ProjectTagText);

            // Title Bar
            this.UpdateColorResource(resources, "TitleBarBackgroundColor", theme.TitleBarBackground);
            this.UpdateColorResource(resources, "TitleBarForegroundColor", theme.TitleBarForeground);
            this.UpdateColorResource(resources, "TitleBarButtonHoverColor", theme.TitleBarButtonHover);
            this.UpdateColorResource(resources, "TitleBarButtonPressedColor", theme.TitleBarButtonPressed);

            // Settings Window
            this.UpdateColorResource(resources, "SettingsBackgroundColor", theme.SettingsBackground);
            this.UpdateColorResource(resources, "SettingsSectionHeaderColor", theme.SettingsSectionHeader);
            this.UpdateColorResource(resources, "SettingsLabelTextColor", theme.SettingsLabelText);

            // Tab Control
            this.UpdateColorResource(resources, "TabBackgroundColor", theme.TabBackground);
            this.UpdateColorResource(resources, "TabSelectedBackgroundColor", theme.AccentPrimary);
            this.UpdateColorResource(resources, "TabHoverBackgroundColor", theme.TabHoverBackground);
            this.UpdateColorResource(resources, "TabTextColor", theme.TabText);
            this.UpdateColorResource(resources, "TabSelectedTextColor", theme.TabSelectedText);

            // Settings shell chrome
            resources["SettingsAccordionHeaderBrush"] = new SolidColorBrush(Color.Parse(theme.SettingsBackground), 0.7);
            resources["SettingsAccordionBorderBrush"] = new SolidColorBrush(Color.Parse(theme.AccentPrimary), 0.4);
            resources["SettingsAccordionHeaderHoverBrush"] = new SolidColorBrush(Colors.White, 0.06);
        }

        private void UpdateFluentAccentResources(Avalonia.Controls.IResourceDictionary resources, string accentHex)
        {
            try
            {
                var accent = Color.Parse(accentHex);

                // Fluent theme controls (CheckBox, Slider, TabControl indicators) consume these keys.
                this.SetColorAndBrush(resources, "SystemAccentColor", accent);
                this.SetColorAndBrush(resources, "SystemAccentColorLight1", this.Mix(accent, Colors.White, 0.2));
                this.SetColorAndBrush(resources, "SystemAccentColorLight2", this.Mix(accent, Colors.White, 0.35));
                this.SetColorAndBrush(resources, "SystemAccentColorLight3", this.Mix(accent, Colors.White, 0.5));
                this.SetColorAndBrush(resources, "SystemAccentColorDark1", this.Mix(accent, Colors.Black, 0.18));
                this.SetColorAndBrush(resources, "SystemAccentColorDark2", this.Mix(accent, Colors.Black, 0.33));
                this.SetColorAndBrush(resources, "SystemAccentColorDark3", this.Mix(accent, Colors.Black, 0.5));

                // Fluent v2 naming used by some control templates.
                this.SetColorAndBrush(resources, "AccentFillColorDefault", accent);
                this.SetColorAndBrush(resources, "AccentFillColorSecondary", this.Mix(accent, Colors.White, 0.12));
                this.SetColorAndBrush(resources, "AccentFillColorTertiary", this.Mix(accent, Colors.Black, 0.12));
            }
            catch (Exception ex)
            {
                this._logWriter?.LogError($"Failed to apply Fluent accent resources for '{accentHex}': {ex.Message}", ex);
            }
        }

        private void SetColorAndBrush(Avalonia.Controls.IResourceDictionary resources, string key, Color color)
        {
            resources[key] = color;
            resources[$"{key}Brush"] = new SolidColorBrush(color);
        }

        private Color Mix(Color source, Color target, double amount)
        {
            var clamped = Math.Clamp(amount, 0.0, 1.0);
            byte Blend(byte a, byte b) => (byte)(a + ((b - a) * clamped));
            return Color.FromArgb(
                255,
                Blend(source.R, target.R),
                Blend(source.G, target.G),
                Blend(source.B, target.B));
        }

        private void UpdateColorResource(Avalonia.Controls.IResourceDictionary resources, string key, string colorHex, double opacity = 1.0)
        {
            try
            {
                var color = Color.Parse(colorHex);
                resources[key] = color;

                // Also update the corresponding brush with opacity baked in
                var brushKey = key.Replace("Color", "Brush");
                if (resources.ContainsKey(brushKey))
                {
                    resources[brushKey] = new SolidColorBrush(color, opacity);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash the application
                this._logWriter?.LogError($"Failed to parse color '{colorHex}' for key '{key}': {ex.Message}", ex);
            }
        }
    }
}
