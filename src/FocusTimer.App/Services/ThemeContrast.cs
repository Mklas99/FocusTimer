namespace FocusTimer.App.Services
{
    using System;
    using System.Linq;
    using Avalonia.Media;

    /// <summary>
    /// WCAG contrast helpers used to keep derived theme colors readable without changing stored theme values.
    /// </summary>
    public static class ThemeContrast
    {
        /// <summary>
        /// The WCAG AA contrast ratio for normal-size text.
        /// </summary>
        public const double TextRatio = 4.5;

        /// <summary>
        /// Computes the WCAG contrast ratio between two opaque colors.
        /// </summary>
        /// <param name="first">The first color.</param>
        /// <param name="second">The second color.</param>
        /// <returns>A ratio from 1 to 21.</returns>
        public static double Ratio(Color first, Color second)
        {
            double a = Luminance(first);
            double b = Luminance(second);
            return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
        }

        /// <summary>
        /// Returns the color closest to <paramref name="preferred"/> that reaches the requested contrast on a surface.
        /// The preferred color is kept when it already qualifies; otherwise it is mixed toward whichever of white
        /// and black contrasts more with the surface, in small steps, which preserves its hue.
        /// </summary>
        /// <param name="preferred">The color to keep when possible, typically a theme accent.</param>
        /// <param name="surface">The color it is drawn on.</param>
        /// <param name="minimumRatio">The contrast ratio to reach.</param>
        /// <returns>An opaque color that reaches the ratio, or pure white or black when nothing closer can.</returns>
        public static Color EnsureContrast(Color preferred, Color surface, double minimumRatio = TextRatio)
        {
            var start = Color.FromRgb(preferred.R, preferred.G, preferred.B);
            if (Ratio(start, surface) >= minimumRatio)
            {
                return start;
            }

            Color target = Ratio(Colors.White, surface) >= Ratio(Colors.Black, surface) ? Colors.White : Colors.Black;
            for (int step = 1; step <= 20; step++)
            {
                Color candidate = Mix(start, target, step / 20.0);
                if (Ratio(candidate, surface) >= minimumRatio)
                {
                    return candidate;
                }
            }

            return target;
        }

        /// <summary>Finds a readable foreground shared by related desktop surfaces.</summary>
        /// <param name="preferred">The stored theme color.</param>
        /// <param name="minimumRatio">The required contrast ratio.</param>
        /// <param name="surfaces">The opaque backgrounds where the color is used.</param>
        /// <returns>A qualifying mix toward white or black.</returns>
        /// <exception cref="InvalidOperationException">No shared foreground qualifies.</exception>
        public static Color EnsureContrastAcross(Color preferred, double minimumRatio, params Color[] surfaces)
        {
            if (TryEnsureContrastAcross(preferred, minimumRatio, out Color result, surfaces))
            {
                return result;
            }

            throw new InvalidOperationException("The surfaces cannot share a foreground at the requested contrast.");
        }

        /// <summary>Finds a shared foreground without accepting a failed endpoint when surfaces conflict.</summary>
        /// <param name="preferred">The source color to retain when readable.</param>
        /// <param name="minimumRatio">The required contrast ratio.</param>
        /// <param name="result">The qualifying opaque color, or default when no color qualifies.</param>
        /// <param name="surfaces">The actual opaque backgrounds.</param>
        /// <returns>Whether a qualifying foreground exists.</returns>
        public static bool TryEnsureContrastAcross(Color preferred, double minimumRatio, out Color result, params Color[] surfaces)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(minimumRatio, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(minimumRatio, 21);
            ArgumentNullException.ThrowIfNull(surfaces);
            if (surfaces.Length == 0)
            {
                throw new ArgumentException("At least one background is required.", nameof(surfaces));
            }

            var start = Color.FromRgb(preferred.R, preferred.G, preferred.B);
            bool Qualifies(Color color) => surfaces.All(surface => Ratio(color, surface) >= minimumRatio);
            if (Qualifies(start))
            {
                result = start;
                return true;
            }

            double closestAmount = double.PositiveInfinity;
            Color closest = default;
            foreach (Color target in new[] { Colors.White, Colors.Black })
            {
                if (!Qualifies(target))
                {
                    continue;
                }

                double low = 0;
                double high = 1;
                for (int iteration = 0; iteration < 20; iteration++)
                {
                    double middle = (low + high) / 2;
                    if (Qualifies(Mix(start, target, middle)))
                    {
                        high = middle;
                    }
                    else
                    {
                        low = middle;
                    }
                }

                if (high < closestAmount)
                {
                    closestAmount = high;
                    closest = Mix(start, target, high);
                }
            }

            if (!double.IsPositiveInfinity(closestAmount))
            {
                result = closest;
                return true;
            }

            // Search both directions, including intermediate colors. Opposing surfaces can share a middle gray
            // even when neither endpoint qualifies. Fine steps avoid skipping the narrow AA interval.
            for (int step = 1; step <= 1024; step++)
            {
                foreach (Color target in new[] { Colors.White, Colors.Black })
                {
                    Color candidate = Mix(start, target, step / 1024.0);
                    if (Qualifies(candidate))
                    {
                        result = candidate;
                        return true;
                    }
                }
            }

            // Hue-preserving paths can miss a representable neutral in an otherwise feasible interval.
            for (int channel = 0; channel <= 255; channel++)
            {
                var candidate = Color.FromRgb((byte)channel, (byte)channel, (byte)channel);
                if (Qualifies(candidate))
                {
                    result = candidate;
                    return true;
                }
            }

            result = default;
            return false;
        }

        private static Color Mix(Color source, Color target, double amount)
        {
            byte Blend(byte from, byte to) => (byte)Math.Round(from + ((to - from) * amount));
            return Color.FromRgb(Blend(source.R, target.R), Blend(source.G, target.G), Blend(source.B, target.B));
        }

        private static double Luminance(Color color)
        {
            static double Channel(byte value)
            {
                double c = value / 255.0;
                return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
            }

            return (0.2126 * Channel(color.R)) + (0.7152 * Channel(color.G)) + (0.0722 * Channel(color.B));
        }
    }
}
