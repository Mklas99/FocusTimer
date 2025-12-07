namespace FocusTimer.Core.Interfaces;

/// <summary>
/// Service for managing application themes, including loading, saving, and applying themes.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the currently active theme.
    /// </summary>
    Models.Theme CurrentTheme { get; }

    /// <summary>
    /// Gets a read-only collection of built-in themes.
    /// </summary>
    IReadOnlyList<Models.Theme> BuiltInThemes { get; }

    /// <summary>
    /// Applies a theme to the application.
    /// </summary>
    /// <param name="theme">The theme to apply.</param>
    void ApplyTheme(Models.Theme theme);

    /// <summary>
    /// Loads a theme from a .fttheme JSON file.
    /// </summary>
    /// <param name="filePath">The path to the theme file.</param>
    /// <returns>The loaded theme.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the file is not valid JSON or missing required properties.</exception>
    Task<Models.Theme> LoadThemeFromFileAsync(string filePath);

    /// <summary>
    /// Saves a theme to a .fttheme JSON file.
    /// </summary>
    /// <param name="theme">The theme to save.</param>
    /// <param name="filePath">The path where the theme file should be saved.</param>
    Task SaveThemeToFileAsync(Models.Theme theme, string filePath);

    /// <summary>
    /// Gets a built-in theme by name.
    /// </summary>
    /// <param name="themeName">The name of the theme (e.g., "Dark", "Light").</param>
    /// <returns>The theme, or null if not found.</returns>
    Models.Theme? GetBuiltInTheme(string themeName);

    /// <summary>
    /// Resets the current theme to the default (Dark) theme.
    /// </summary>
    void ResetToDefault();

    /// <summary>
    /// Validates that a theme has all required color properties.
    /// </summary>
    /// <param name="theme">The theme to validate.</param>
    /// <returns>True if the theme is valid, false otherwise.</returns>
    bool ValidateTheme(Models.Theme theme);
}
