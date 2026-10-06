namespace FocusTimer.App.Services
{
    using Avalonia.Controls;

    /// <summary>
    /// The facts that decide the desktop material.
    /// </summary>
    /// <param name="Actual">The transparency level the platform reports as achieved (not merely requested).</param>
    /// <param name="HighContrastTheme">Whether the active FocusTimer theme is the High Contrast theme.</param>
    /// <param name="SystemHighContrast">Whether the operating system asks for higher contrast.</param>
    /// <param name="TransparencyDisabled">Whether the operating system has transparency effects turned off.</param>
    public readonly record struct DesktopMaterialInputs(
        WindowTransparencyLevel Actual,
        bool HighContrastTheme,
        bool SystemHighContrast,
        bool TransparencyDisabled);
}
