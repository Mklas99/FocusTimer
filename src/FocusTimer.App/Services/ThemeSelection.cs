namespace FocusTimer.App.Services;

using System;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

/// <summary>Interprets saved preset identity consistently at startup and in the Settings draft.</summary>
internal static class ThemeSelection
{
    /// <summary>The persisted identity of a custom snapshot.</summary>
    internal const string CustomIdentity = "Custom";

    /// <summary>The visible entry for the single current custom draft.</summary>
    internal const string CustomLabel = "Custom/Imported";

    /// <summary>Restores the saved snapshot or resolves a mismatched factory preset.</summary>
    /// <param name="settings">The in-memory settings, never persisted by this method.</param>
    /// <param name="themes">The preset definitions.</param>
    /// <returns>The visible dropdown selection.</returns>
    internal static string Restore(Settings settings, IThemeService themes)
    {
        Theme? preset = themes.GetBuiltInTheme(settings.ActiveThemeName);
        bool legacyImport = !string.IsNullOrWhiteSpace(settings.CustomThemePath) &&
            string.Equals(settings.ActiveThemeName, settings.Theme.ThemeName, StringComparison.OrdinalIgnoreCase);
        if (string.Equals(settings.ActiveThemeName, CustomIdentity, StringComparison.OrdinalIgnoreCase) ||
            legacyImport || preset == null)
        {
            // A stale path matching a preset's name cannot be distinguished from a same-name import.
            // Preserve that snapshot until the user explicitly selects a preset or resets.
            settings.ActiveThemeName = CustomIdentity;
            return CustomLabel;
        }

        if (!string.Equals(settings.Theme.ThemeName, preset.ThemeName, StringComparison.OrdinalIgnoreCase))
        {
            settings.Theme = preset;
            settings.CustomThemePath = null;
        }

        settings.ActiveThemeName = preset.ThemeName;
        return preset.ThemeName;
    }
}
