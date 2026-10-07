namespace FocusTimer.App.Services
{
    using System;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Media;
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;

    /// <summary>
    /// Manages theme application by updating Avalonia's resource dictionary at runtime.
    /// </summary>
    public class ThemeManager
    {
        /// <summary>
        /// Tint opacity of the dense-frost Settings/Worklog shell (a near-solid 95% tint).
        /// </summary>
        public const double DesktopShellFrostOpacity = 0.95;

        private readonly IAppLogger? _logWriter;
        private IResourceDictionary? _activeResources;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemeManager"/> class.
        /// </summary>
        /// <param name="logWriter">Optional logger for error logging.</param>
        public ThemeManager(IAppLogger? logWriter = null)
        {
            this._logWriter = logWriter;
        }

        /// <summary>
        /// Raised after a theme is applied so windows can update platform backdrop settings.
        /// </summary>
        public event Action<Theme>? ThemeApplied;

        /// <summary>
        /// Raised when the window reports a different achieved transparency level.
        /// </summary>
        public event Action<WindowTransparencyLevel>? ActualWidgetTransparencyChanged;

        /// <summary>
        /// Gets the most recently applied theme snapshot.
        /// </summary>
        public Theme? ActiveTheme { get; private set; }

        /// <summary>
        /// Gets the transparency level achieved by the widget window.
        /// </summary>
        public WindowTransparencyLevel ActualWidgetTransparency { get; private set; } = WindowTransparencyLevel.None;

        /// <summary>
        /// Gets a value indicating whether the widget needs an opaque shell for the current backdrop result.
        /// </summary>
        public bool IsWidgetShellFallbackActive => this.ActiveTheme != null &&
            (string.Equals(this.ActiveTheme.ThemeName, "High Contrast", StringComparison.OrdinalIgnoreCase) ||
             this.ActiveTheme.WidgetBlurMode == WidgetBlurModes.Solid ||
             this.ActualWidgetTransparency == WindowTransparencyLevel.None);

        /// <summary>
        /// Reports the transparency level achieved by the widget window.
        /// </summary>
        /// <param name="level">The achieved level.</param>
        public void ReportActualWidgetTransparency(WindowTransparencyLevel level)
        {
            if (this.ActualWidgetTransparency == level)
            {
                return;
            }

            this.ActualWidgetTransparency = level;
            this.UpdateWidgetShellActiveBrush();
            this.ActualWidgetTransparencyChanged?.Invoke(level);
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

            var defaultTheme = new Theme(); // Uses default values
            this.ApplyTheme(defaultTheme);
        }

        /// <summary>
        /// Applies a theme by updating all color resources in the application or provided dictionary.
        /// </summary>
        /// <param name="theme">The theme to apply.</param>
        /// <param name="targetResources">Optional target resource dictionary; uses Application.Current.Resources when null.</param>
        public void ApplyTheme(Theme theme, IResourceDictionary? targetResources = null)
        {
            this.ActiveTheme = theme.Clone();
            IResourceDictionary? resources = targetResources ?? Application.Current?.Resources;
            this._activeResources = resources;
            if (resources == null)
            {
                this.ThemeApplied?.Invoke(this.ActiveTheme);
                return;
            }

            // High Contrast never uses translucent desktop material.
            SetResourceIfChanged(
                resources,
                DesktopWindowMaterial.ForceSolidResourceKey,
                string.Equals(theme.ThemeName, "High Contrast", StringComparison.OrdinalIgnoreCase));

            // Store opacity values as resources
            SetResourceIfChanged(resources, "BackgroundOpacity", theme.BackgroundOpacity);
            SetResourceIfChanged(resources, "TimerOpacity", theme.TimerOpacity);
            SetResourceIfChanged(resources, "ButtonOpacity", theme.ButtonOpacity);
            SetResourceIfChanged(resources, "WidgetBaseOpacity", theme.WidgetBaseOpacity);

            // Window Colors (with background opacity applied to brush, but transparent if opacity is 0)
            if (theme.BackgroundOpacity <= 0)
            {
                this.UpdateColorResource(resources, "WindowBackgroundColor", theme.WindowBackground, 0, useTransparentBrush: true);
            }
            else
            {
                this.UpdateColorResource(resources, "WindowBackgroundColor", theme.WindowBackground, theme.BackgroundOpacity);
            }

            try
            {
                Color baseColor = Color.Parse(theme.WindowBackground);
                Color opaqueColor = Color.FromArgb(255, baseColor.R, baseColor.G, baseColor.B);
                double tintOpacity = theme.ThemeName == "High Contrast" ? 1.0 : theme.BackgroundOpacity;
                SetBrushIfChanged(resources, "WidgetShellTintBrush", opaqueColor, tintOpacity);
                SetBrushIfChanged(resources, "WidgetShellFallbackBrush", opaqueColor);
                this.UpdateWidgetShellActiveBrush();
            }
            catch (Exception ex)
            {
                this._logWriter?.LogError($"Failed to initialize widget shell resources: {ex.Message}", ex);
            }

            this.UpdateColorResource(resources, "WindowForegroundColor", theme.WindowForeground);
            this.UpdateColorResource(resources, "WindowBorderColor", theme.WindowBorder);

            // Text Colors
            this.UpdateColorResource(resources, "PrimaryTextColor", theme.PrimaryText);
            this.UpdateColorResource(resources, "SecondaryTextColor", theme.SecondaryText);
            this.UpdateColorResource(resources, "DisabledTextColor", theme.DisabledText);

            // Widget layers apply clock and controls opacity once, including during preview.
            this.UpdateColorResource(resources, "TimerTextColor", theme.TimerText);
            this.UpdateColorResource(resources, "TimerBackgroundColor", theme.TimerBackground);

            // Buttons & Controls
            this.UpdateColorResource(resources, "ButtonNormalColor", theme.ButtonNormal);
            this.UpdateColorResource(resources, "PlayPauseColor", theme.PlayPauseColor ?? theme.ButtonNormal);
            this.UpdateColorResource(resources, "ButtonHoverColor", theme.ButtonHover);
            this.UpdateColorResource(resources, "ButtonPressedColor", theme.ButtonPressed);
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
            this.UpdateColorResource(resources, "InputFocusBorderColor", theme.InputFocusBorder);
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
            this.UpdateColorResource(resources, "TabSelectedBackgroundColor", theme.TabSelectedBackground);
            this.UpdateColorResource(resources, "TabHoverBackgroundColor", theme.TabHoverBackground);
            this.UpdateColorResource(resources, "TabTextColor", theme.TabText);
            this.UpdateColorResource(resources, "TabSelectedTextColor", theme.TabSelectedText);

            // Settings shell chrome and semantic tokens
            try
            {
                SetBrushIfChanged(resources, "SettingsAccordionHeaderBrush", Color.Parse(theme.SettingsBackground), 0.7);
                SetBrushIfChanged(resources, "SettingsAccordionBorderBrush", Color.Parse(theme.AccentPrimary), 0.4);
                SetBrushIfChanged(resources, "SettingsAccordionHeaderHoverBrush", Colors.White, 0.06);

                // Desktop material: derived from Settings roles only, independent of widget opacity values.
                Color shell = Color.Parse(theme.SettingsBackground);
                Color opaqueShell = Color.FromArgb(255, shell.R, shell.G, shell.B);
                SetBrushIfChanged(resources, "DesktopShellFrostBrush", opaqueShell, DesktopShellFrostOpacity);
                SetBrushIfChanged(resources, "DesktopCardBrush", Mix(opaqueShell, Color.Parse(theme.PrimaryText), 0.06));
                Color primaryText = Color.Parse(theme.PrimaryText);
                Color opaquePrimaryText = Color.FromArgb(255, primaryText.R, primaryText.G, primaryText.B);

                // Fields, buttons and disclosure headers share one opaque surface (the input role composited over the
                // window), so they look identical on the window and inside cards; hover and pressed are opaque mixes of it.
                Color field = Composite(Color.Parse(theme.InputBackground), opaqueShell);
                SetBrushIfChanged(resources, "DesktopFieldBrush", field);

                // Focus indication is the accent, lightened or darkened only as far as needed to reach 3:1 on the
                // window and on fields (Dark and Nord accents are below that).
                Color focus = ThemeContrast.EnsureContrast(
                    ThemeContrast.EnsureContrast(Color.Parse(theme.AccentPrimary), opaqueShell, 3.0), field, 3.0);
                SetBrushIfChanged(resources, "DesktopFocusBrush", focus);
                SetBrushIfChanged(resources, "DesktopControlHoverBrush", Mix(field, opaquePrimaryText, 0.10));
                SetBrushIfChanged(resources, "DesktopControlPressedBrush", Mix(field, opaquePrimaryText, 0.18));
                Color card = Mix(opaqueShell, primaryText, 0.06);
                Color hover = Mix(field, opaquePrimaryText, 0.10);
                Color pressed = Mix(field, opaquePrimaryText, 0.18);
                Color Readable(string value) => ThemeContrast.EnsureContrastAcross(
                    Color.Parse(value), ThemeContrast.TextRatio, opaqueShell, card, field, hover, pressed);
                SetBrushIfChanged(resources, "DesktopTextBrush", Readable(theme.PrimaryText));
                SetBrushIfChanged(resources, "DesktopSecondaryTextBrush", Readable(theme.SecondaryText));
                SetBrushIfChanged(resources, "DesktopLabelBrush", Readable(theme.SettingsLabelText));
                SetBrushIfChanged(resources, "DesktopHeadingBrush", Readable(theme.SettingsSectionHeader));
                SetBrushIfChanged(resources, "DesktopInputTextBrush", Readable(theme.InputText));
                SetBrushIfChanged(resources, "DesktopWarningBrush", Readable(theme.WarningColor));
                SetBrushIfChanged(resources, "DesktopDangerBrush", Readable(theme.DangerColor));
                SetBrushIfChanged(resources, "DesktopSuccessBrush", Readable(theme.SuccessColor));
                SetBrushIfChanged(resources, "DesktopBorderBrush", ThemeContrast.EnsureContrastAcross(
                    Composite(Color.Parse(theme.InputBorder), field), 3.0, opaqueShell, card, field));
                SetBrushIfChanged(resources, "DesktopNotificationBorderBrush", ThemeContrast.EnsureContrast(
                    Color.Parse(theme.WindowBorder), opaqueShell, 3.0));
                Color tabHover = Composite(Color.Parse(theme.TabHoverBackground), opaqueShell);
                SetBrushIfChanged(resources, "DesktopTabTextBrush", ThemeContrast.EnsureContrastAcross(
                    Color.Parse(theme.TabText), ThemeContrast.TextRatio, opaqueShell, tabHover));
                SetBrushIfChanged(resources, "DesktopSelectedTextBrush", ThemeContrast.EnsureContrast(
                    Color.Parse(theme.TabSelectedText), Composite(Color.Parse(theme.TabSelectedBackground), opaqueShell)));
                SetBrushIfChanged(resources, "DesktopTabSelectedBrush", ThemeContrast.EnsureContrast(
                    Color.Parse(theme.TabSelectedBackground), opaqueShell, 3.0));
                SetBrushIfChanged(resources, "DesktopAccentTextBrush", ThemeContrast.EnsureContrast(primaryText, focus));
                Color tabText = Color.Parse(theme.TabText);

                // Tabs sit directly on the window, so the label must be readable on the Settings background.
                SetBrushIfChanged(
                    resources,
                    "TabSelectedLabelBrush",
                    ThemeContrast.EnsureContrast(Color.Parse(theme.AccentPrimary), opaqueShell));
                SetBrushIfChanged(resources, "DesktopTabUnderlineIdleBrush", Color.FromArgb(255, tabText.R, tabText.G, tabText.B), 0.18);

                var accentColor = Color.Parse(theme.AccentPrimary);
                SetBrushIfChanged(resources, "FocusRingBrush", accentColor);
                SetBrushIfChanged(resources, "BorderSubtleBrush", Color.Parse(theme.InputBorder));
                SetBrushIfChanged(resources, "SurfaceSubtleBrush", Color.Parse(theme.InputBackground));
                SetResourceIfChanged(resources, "ActionPrimaryBrush", resources["ButtonNormalBrush"] ?? new SolidColorBrush(Color.Parse(theme.ButtonNormal)));
                SetResourceIfChanged(resources, "ActionPrimaryHoverBrush", resources["ButtonHoverBrush"] ?? new SolidColorBrush(Color.Parse(theme.ButtonHover)));
                SetResourceIfChanged(resources, "ActionPrimaryPressedBrush", resources["ButtonPressedBrush"] ?? new SolidColorBrush(Color.Parse(theme.ButtonPressed)));
            }
            catch (Exception ex)
            {
                this._logWriter?.LogError($"Failed to initialize semantic and chrome resources: {ex.Message}", ex);
            }

            this.ThemeApplied?.Invoke(this.ActiveTheme);
        }

        private static void SetColorAndBrush(IResourceDictionary resources, string key, Color color)
        {
            SetResourceIfChanged(resources, key, color);
            SetBrushIfChanged(resources, $"{key}Brush", color);
        }

        private static void SetResourceIfChanged(IResourceDictionary resources, string key, object value)
        {
            if (!resources.TryGetValue(key, out object? current) || !Equals(current, value))
            {
                resources[key] = value;
            }
        }

        private static void SetBrushIfChanged(IResourceDictionary resources, string key, Color color, double opacity = 1.0)
        {
            if (resources.TryGetValue(key, out object? current) && current is SolidColorBrush brush &&
                brush.Color == color && brush.Opacity == opacity)
            {
                return;
            }

            resources[key] = new SolidColorBrush(color, opacity);
        }

        private static Color Composite(Color foreground, Color background)
        {
            double alpha = foreground.A / 255.0;
            byte Blend(byte front, byte back) => (byte)Math.Round((alpha * front) + ((1 - alpha) * back));
            return Color.FromRgb(Blend(foreground.R, background.R), Blend(foreground.G, background.G), Blend(foreground.B, background.B));
        }

        private static Color Mix(Color source, Color target, double amount)
        {
            double clamped = Math.Clamp(amount, 0.0, 1.0);
            byte Blend(byte a, byte b) => (byte)(a + ((b - a) * clamped));
            return Color.FromArgb(
                255,
                Blend(source.R, target.R),
                Blend(source.G, target.G),
                Blend(source.B, target.B));
        }

        private void UpdateWidgetShellActiveBrush()
        {
            if (this._activeResources == null)
            {
                return;
            }

            string key = this.IsWidgetShellFallbackActive ? "WidgetShellFallbackBrush" : "WidgetShellTintBrush";
            if (this._activeResources[key] is object brush)
            {
                SetResourceIfChanged(this._activeResources, "WidgetShellActiveBrush", brush);
            }
        }

        private void UpdateFluentAccentResources(IResourceDictionary resources, string accentHex)
        {
            try
            {
                var accent = Color.Parse(accentHex);

                // Fluent theme controls (CheckBox, Slider, TabControl indicators) consume these keys.
                SetColorAndBrush(resources, "SystemAccentColor", accent);
                SetColorAndBrush(resources, "SystemAccentColorLight1", Mix(accent, Colors.White, 0.2));
                SetColorAndBrush(resources, "SystemAccentColorLight2", Mix(accent, Colors.White, 0.35));
                SetColorAndBrush(resources, "SystemAccentColorLight3", Mix(accent, Colors.White, 0.5));
                SetColorAndBrush(resources, "SystemAccentColorDark1", Mix(accent, Colors.Black, 0.18));
                SetColorAndBrush(resources, "SystemAccentColorDark2", Mix(accent, Colors.Black, 0.33));
                SetColorAndBrush(resources, "SystemAccentColorDark3", Mix(accent, Colors.Black, 0.5));

                // Fluent v2 naming used by some control templates.
                SetColorAndBrush(resources, "AccentFillColorDefault", accent);
                SetColorAndBrush(resources, "AccentFillColorSecondary", Mix(accent, Colors.White, 0.12));
                SetColorAndBrush(resources, "AccentFillColorTertiary", Mix(accent, Colors.Black, 0.12));
            }
            catch (Exception ex)
            {
                this._logWriter?.LogError($"Failed to apply Fluent accent resources for '{accentHex}': {ex.Message}", ex);
            }
        }

        private void UpdateColorResource(IResourceDictionary resources, string key, string colorHex, double opacity = 1.0, bool useTransparentBrush = false)
        {
            try
            {
                var color = Color.Parse(colorHex);
                SetResourceIfChanged(resources, key, color);

                // Also update the corresponding brush with opacity baked in
                string brushKey = key.Replace("Color", "Brush");
                if (useTransparentBrush)
                {
                    SetResourceIfChanged(resources, brushKey, Brushes.Transparent);
                }
                else
                {
                    SetBrushIfChanged(resources, brushKey, color, opacity);
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
