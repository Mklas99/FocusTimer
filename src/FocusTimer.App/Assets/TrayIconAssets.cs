using Avalonia.Controls;
using Avalonia.Platform;
using FocusTimer.Core.Models;

// Centralized tray icon asset provider
public class TrayIconAssets
{
    public WindowIcon GetIcon(TimerState state)
    {
        return state switch
        {
            TimerState.Running => TrayRunning,
            TimerState.Paused => TrayPaused,
            TimerState.Idle => TrayIdle,
            _ => TrayIdle,
        };
    }

    public WindowIcon TrayRunning { get; } = new WindowIcon(
        Avalonia.Platform.AssetLoader.Open(new Uri("avares://FocusTimer.App/Assets/FocusTimer-play.png")));
    public WindowIcon TrayPaused { get; } = new WindowIcon(
        Avalonia.Platform.AssetLoader.Open(new Uri("avares://FocusTimer.App/Assets/FocusTimer-pause.png")));
    public WindowIcon TrayIdle { get; } = new WindowIcon(
        Avalonia.Platform.AssetLoader.Open(new Uri("avares://FocusTimer.App/Assets/FocusTimer-idle.png")));
}