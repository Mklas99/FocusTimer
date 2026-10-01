namespace FocusTimer.App.Services
{
    using System;
    using System.Collections.Generic;
    using Avalonia.Controls;
    using FocusTimer.Core.Models;

    /// <summary>
    /// Maps widget blur preferences to platform backdrop fallback levels.
    /// </summary>
    public static class WidgetBackdropLevels
    {
        /// <summary>
        /// Gets the preferred levels for a theme, strongest first.
        /// </summary>
        /// <param name="theme">The active appearance theme.</param>
        /// <returns>Backdrop levels in fallback order.</returns>
        public static IReadOnlyList<WindowTransparencyLevel> ForTheme(Theme theme)
        {
            if (string.Equals(theme.ThemeName, "High Contrast", StringComparison.OrdinalIgnoreCase))
            {
                return [WindowTransparencyLevel.Transparent];
            }

            return theme.WidgetBlurMode switch
            {
                WidgetBlurModes.Off => [WindowTransparencyLevel.Transparent],
                WidgetBlurModes.Soft => [WindowTransparencyLevel.Blur, WindowTransparencyLevel.Transparent],
                _ => [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Blur, WindowTransparencyLevel.Transparent],
            };
        }
    }
}
