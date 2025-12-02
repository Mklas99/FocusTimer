namespace FocusTimer.Core.Models;

/// <summary>
/// Represents a global hotkey definition with modifiers and key.
/// </summary>
public struct HotkeyDefinition
{
    /// <summary>
    /// Modifier keys (Ctrl, Alt, Shift, Win).
    /// </summary>
    public HotkeyModifiers Modifiers { get; set; }

    /// <summary>
    /// The key code.
    /// </summary>
    public int KeyCode { get; set; }

    public HotkeyDefinition(HotkeyModifiers modifiers, int keyCode)
    {
        Modifiers = modifiers;
        KeyCode = keyCode;
    }

    /// <summary>
    /// Parses a hotkey string like "Ctrl+Alt+S" into a HotkeyDefinition.
    /// Returns null if parsing fails.
    /// </summary>
    public static HotkeyDefinition? Parse(string? hotkeyString)
    {
        if (string.IsNullOrWhiteSpace(hotkeyString))
            return null;

        var parts = hotkeyString.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
            return null;

        var modifiers = HotkeyModifiers.None;
        string? keyPart = null;

        foreach (var part in parts)
        {
            switch (part.ToUpperInvariant())
            {
                case "CTRL":
                case "CONTROL":
                    modifiers |= HotkeyModifiers.Control;
                    break;
                case "ALT":
                    modifiers |= HotkeyModifiers.Alt;
                    break;
                case "SHIFT":
                    modifiers |= HotkeyModifiers.Shift;
                    break;
                case "WIN":
                case "WINDOWS":
                    modifiers |= HotkeyModifiers.Win;
                    break;
                default:
                    keyPart = part;
                    break;
            }
        }

        if (string.IsNullOrEmpty(keyPart) || keyPart.Length != 1)
            return null;

        var keyCode = char.ToUpperInvariant(keyPart[0]);
        return new HotkeyDefinition(modifiers, keyCode);
    }

    public override string ToString()
    {
        var parts = new List<string>();
        
        if (Modifiers.HasFlag(HotkeyModifiers.Control))
            parts.Add("Ctrl");
        if (Modifiers.HasFlag(HotkeyModifiers.Alt))
            parts.Add("Alt");
        if (Modifiers.HasFlag(HotkeyModifiers.Shift))
            parts.Add("Shift");
        if (Modifiers.HasFlag(HotkeyModifiers.Win))
            parts.Add("Win");
        
        parts.Add(((char)KeyCode).ToString());
        
        return string.Join("+", parts);
    }
}

/// <summary>
/// Hotkey modifier flags.
/// </summary>
[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Win = 8
}
