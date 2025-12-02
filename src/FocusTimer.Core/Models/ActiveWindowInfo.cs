namespace FocusTimer.Core.Models;

/// <summary>
/// Information about the currently active window.
/// </summary>
public class ActiveWindowInfo
{
    public string ProcessName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
}
