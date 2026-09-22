namespace FocusTimer.App.Assets
{
    using System.Diagnostics.CodeAnalysis;
    using Avalonia.Controls;
    using FocusTimer.Core.Models;

    /// <summary>
    /// Centralized tray icon asset provider.
    /// </summary>
    public class TrayIconAssets
    {
        /// <summary>
        /// Gets the tray icon for the running state.
        /// </summary>
        [SuppressMessage("Sonar Code Smell", "S1075", Justification = "avares:// is Avalonia's compile-time embedded-resource scheme, not a filesystem/network path.")]
        public WindowIcon TrayRunning { get; } = new WindowIcon(
            Avalonia.Platform.AssetLoader.Open(new Uri("avares://FocusTimer.App/Assets/FocusTimer-play.png")));

        /// <summary>
        /// Gets the tray icon for the paused state.
        /// </summary>
        [SuppressMessage("Sonar Code Smell", "S1075", Justification = "avares:// is Avalonia's compile-time embedded-resource scheme, not a filesystem/network path.")]
        public WindowIcon TrayPaused { get; } = new WindowIcon(
            Avalonia.Platform.AssetLoader.Open(new Uri("avares://FocusTimer.App/Assets/FocusTimer-pause.png")));

        /// <summary>
        /// Gets the tray icon for the idle state.
        /// </summary>
        [SuppressMessage("Sonar Code Smell", "S1075", Justification = "avares:// is Avalonia's compile-time embedded-resource scheme, not a filesystem/network path.")]
        public WindowIcon TrayIdle { get; } = new WindowIcon(
            Avalonia.Platform.AssetLoader.Open(new Uri("avares://FocusTimer.App/Assets/FocusTimer-idle.png")));

        /// <summary>
        /// Gets the tray icon for the specified timer state.
        /// </summary>
        /// <param name="state">The current state of the timer.</param>
        /// <returns>A WindowIcon corresponding to the given timer state.</returns>
        public WindowIcon GetIcon(TimerState state)
        {
            return state switch
            {
                TimerState.Running => this.TrayRunning,
                TimerState.Paused => this.TrayPaused,
                TimerState.Idle => this.TrayIdle,
                _ => this.TrayIdle,
            };
        }
    }
}
