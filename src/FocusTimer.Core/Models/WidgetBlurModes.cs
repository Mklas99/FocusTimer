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

        /// <summary>Legacy lighter blur choice.</summary>
        public const string Soft = "Soft";

        /// <summary>Legacy stronger blur choice.</summary>
        public const string Strong = "Strong";

        /// <summary>
        /// Checks whether a serialized blur choice is supported.
        /// </summary>
        /// <param name="value">The serialized blur choice.</param>
        /// <returns>True when the choice is supported.</returns>
        public static bool IsValid(string? value) => value is Off or Solid or Blur or Soft or Strong;

        /// <summary>Maps older saved blur choices to the current choice.</summary>
        /// <param name="value">The serialized choice.</param>
        /// <returns>The current choice corresponding to the saved value.</returns>
        public static string Normalize(string value) => value is Soft or Strong or Blur ? Off : value;
    }
}
