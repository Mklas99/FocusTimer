namespace FocusTimer.Core.Models
{
    /// <summary>
    /// Stable serialized names for widget backdrop choices.
    /// </summary>
    public static class WidgetBlurModes
    {
        /// <summary>No backdrop blur.</summary>
        public const string Off = "Off";

        /// <summary>Legacy blur choice retained for settings migration.</summary>
        public const string Blur = "Blur";

        /// <summary>Use a fully opaque widget background.</summary>
        public const string Solid = "Solid";

        /// <summary>Blur live desktop content using the qualified native widget backend.</summary>
        public const string Frosted = "Frosted";

        /// <summary>Legacy lighter blur choice.</summary>
        public const string Soft = "Soft";

        /// <summary>Legacy stronger blur choice.</summary>
        public const string Strong = "Strong";

        /// <summary>
        /// Checks whether a serialized blur choice is supported.
        /// </summary>
        /// <param name="value">The serialized blur choice.</param>
        /// <returns>True when the choice is supported.</returns>
        public static bool IsValid(string? value) => value is Off or Solid or Frosted or Blur or Soft or Strong;

        /// <summary>Converts the additive material token before opening a theme with an older build.</summary>
        /// <param name="value">Current serialized material.</param>
        /// <returns>A token supported by pre-Frosted builds.</returns>
        public static string ForOlderBuild(string value) => value == Frosted ? Off : value;

        /// <summary>Maps older saved blur choices to the current choice.</summary>
        /// <param name="value">The serialized choice.</param>
        /// <returns>The current choice corresponding to the saved value.</returns>
        public static string Normalize(string value) => value is Soft or Strong or Blur ? Off : value;
    }
}
