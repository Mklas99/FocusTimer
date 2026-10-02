namespace FocusTimer.Core.Models
{
    /// <summary>The registration to restore after an interrupted settings commit.</summary>
    /// <param name="Enabled">Whether auto-start was registered.</param>
    /// <param name="Command">The original command, when the platform can capture it.</param>
    /// <param name="ValueKind">The original registration value kind, when available.</param>
    public sealed record AutoStartRegistration(bool Enabled, string? Command = null, string? ValueKind = null);
}
