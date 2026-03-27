namespace FocusTimer.Core.Models
{
    /// <summary>
    /// Hotkey modifier flags.
    /// </summary>
    [Flags]
    public enum HotkeyModifiers
    {
        /// <summary>
        /// No modifier keys.
        /// </summary>
        None = 0,

        /// <summary>
        /// Alt modifier key.
        /// </summary>
        Alt = 1,

        /// <summary>
        /// Control modifier key.
        /// </summary>
        Control = 2,

        /// <summary>
        /// Shift modifier key.
        /// </summary>
        Shift = 4,

        /// <summary>
        /// Windows modifier key.
        /// </summary>
        Win = 8,
    }

    /// <summary>
    /// Represents a global hotkey definition with modifiers and key.
    /// </summary>
    public struct HotkeyDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HotkeyDefinition"/> struct.
        /// </summary>
        /// <param name="modifiers">The modifier keys (Ctrl, Alt, Shift, Win).</param>
        /// <param name="keyCode">The key code.</param>
        public HotkeyDefinition(HotkeyModifiers modifiers, int keyCode)
        {
            this.Modifiers = modifiers;
            this.KeyCode = keyCode;
        }

        /// <summary>
        /// Gets or sets modifier keys (Ctrl, Alt, Shift, Win).
        /// </summary>
        public HotkeyModifiers Modifiers { get; set; }

        /// <summary>
        /// Gets or sets the key code.
        /// </summary>
        public int KeyCode { get; set; }

        /// <summary>
        /// Parses a hotkey string like "Ctrl+Alt+S" into a HotkeyDefinition.
        /// Returns null if parsing fails.
        /// </summary>
        /// <param name="hotkeyString">The hotkey string to parse, in the format "Modifier+Modifier+Key" (e.g., "Ctrl+Alt+S").</param>
        /// <returns>A HotkeyDefinition if parsing succeeds, or null if parsing fails.</returns>
        public static HotkeyDefinition? Parse(string? hotkeyString)
        {
            if (string.IsNullOrWhiteSpace(hotkeyString))
            {
                return null;
            }

            var parts = hotkeyString.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
            {
                return null;
            }

            var modifiers = HotkeyModifiers.None;
            string? keyPart = null;

            foreach (var part in parts)
            {
                switch (part.ToUpperInvariant())
                {
                    case "CTRL":
                    case "CONTROL":
                    {
                        modifiers |= HotkeyModifiers.Control;
                        break;
                    }

                    case "ALT":
                    {
                        modifiers |= HotkeyModifiers.Alt;
                        break;
                    }

                    case "SHIFT":
                    {
                        modifiers |= HotkeyModifiers.Shift;
                        break;
                    }

                    case "WIN":
                    case "WINDOWS":
                    {
                        modifiers |= HotkeyModifiers.Win;
                        break;
                    }

                    default:
                    {
                        keyPart = part;
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(keyPart) || keyPart.Length != 1)
            {
                return null;
            }

            var keyCode = char.ToUpperInvariant(keyPart[0]);
            return new HotkeyDefinition(modifiers, keyCode);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var parts = new List<string>();

            if (this.Modifiers.HasFlag(HotkeyModifiers.Control))
            {
                parts.Add("Ctrl");
            }

            if (this.Modifiers.HasFlag(HotkeyModifiers.Alt))
            {
                parts.Add("Alt");
            }

            if (this.Modifiers.HasFlag(HotkeyModifiers.Shift))
            {
                parts.Add("Shift");
            }

            if (this.Modifiers.HasFlag(HotkeyModifiers.Win))
            {
                parts.Add("Win");
            }

            parts.Add(((char)this.KeyCode).ToString());

            return string.Join("+", parts);
        }
    }
}
