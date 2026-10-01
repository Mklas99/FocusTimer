namespace FocusTimer.Core.Models
{
    /// <summary>
    /// Stable serialized names for widget backdrop blur choices.
    /// </summary>
    public static class WidgetBlurModes
    {
        /// <summary>No backdrop blur.</summary>
        public const string Off = "Off";

        /// <summary>Standard backdrop blur.</summary>
        public const string Soft = "Soft";

        /// <summary>Frosted acrylic backdrop blur.</summary>
        public const string Strong = "Strong";

        /// <summary>
        /// Checks whether a serialized blur choice is supported.
        /// </summary>
        /// <param name="value">The serialized blur choice.</param>
        /// <returns>True when the choice is supported.</returns>
        public static bool IsValid(string? value) => value is Off or Soft or Strong;
    }
}
