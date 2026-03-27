namespace FocusTimer.Core.Models
{
    /// <summary>
    /// Information about the currently active window.
    /// </summary>
    public class ActiveWindowInfo
    {
        /// <summary>
        /// Gets or sets the process name of the active window.
        /// </summary>
        public string ProcessName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the title of the active window.
        /// </summary>
        public string WindowTitle { get; set; } = string.Empty;
    }
}
