namespace FocusTimer.App.Services
{
    using System.Collections.Generic;
    using Avalonia.Controls;

    /// <summary>
    /// Chooses between the dense-frost shell and its solid fallback. Pure so every transition is testable.
    /// </summary>
    public static class DesktopMaterialPolicy
    {
        /// <summary>
        /// Gets the transparency levels requested for desktop windows, strongest effect first.
        /// </summary>
        public static IReadOnlyList<WindowTransparencyLevel> RequestedLevels { get; } =
            [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.None];

        /// <summary>
        /// Resolves the material. Frost requires a reported blur level; an API call that merely succeeded is not enough.
        /// </summary>
        /// <param name="inputs">The platform and theme facts.</param>
        /// <returns>The material to paint.</returns>
        public static DesktopMaterialState Resolve(in DesktopMaterialInputs inputs)
        {
            return !inputs.HighContrastTheme && !inputs.SystemHighContrast && !inputs.TransparencyDisabled
                && IsBlurLevel(inputs.Actual)
                ? DesktopMaterialState.DenseFrost
                : DesktopMaterialState.SolidFallback;
        }

        /// <summary>
        /// Returns whether a transparency level samples and softens the content behind the window.
        /// </summary>
        /// <param name="level">The level to test.</param>
        /// <returns><see langword="true"/> for blur levels.</returns>
        public static bool IsBlurLevel(WindowTransparencyLevel level) =>
            level == WindowTransparencyLevel.AcrylicBlur || level == WindowTransparencyLevel.Blur;

        /// <summary>
        /// Describes the requested and achieved material for diagnostics.
        /// </summary>
        /// <param name="inputs">The platform and theme facts.</param>
        /// <param name="state">The resolved material.</param>
        /// <returns>A one-line description.</returns>
        public static string Describe(in DesktopMaterialInputs inputs, DesktopMaterialState state) =>
            $"Window material: {(state == DesktopMaterialState.DenseFrost ? "dense frost" : "solid")} " +
            $"(requested {string.Join("/", RequestedLevels)}, actual {inputs.Actual}" +
            $"{(inputs.HighContrastTheme || inputs.SystemHighContrast ? ", high contrast" : string.Empty)}" +
            $"{(inputs.TransparencyDisabled ? ", transparency effects off" : string.Empty)})";
    }
}
