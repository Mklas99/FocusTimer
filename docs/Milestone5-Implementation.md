# Milestone 5 Implementation — Break Reminders, Auto-start, Global Hotkeys & Linux Abstraction

## Overview

This milestone adds advanced system integration features to FocusTimer:
- **Break Reminders**: Scheduled notifications based on timer activity
- **Auto-start on Login**: Windows registry-based auto-start (Linux stub with `.desktop` file notes)
- **Global Hotkeys**: System-wide hotkeys for show/hide and timer control (Windows only)
- **Cross-platform Abstraction**: Linux stubs ready for future implementation

## Architecture Changes

### New Interfaces

#### `IAutoStartService`
```csharp
namespace FocusTimer.Core.Interfaces;

public interface IAutoStartService
{
    void SetAutoStart(bool enabled);
    bool IsAutoStartEnabled();
}
```

### New Core Services

#### `BreakReminderService`
**Location**: `FocusTimer.Core/Services/BreakReminderService.cs`

**Responsibilities**:
- Schedules break reminders based on `Settings.BreakIntervalMinutes`
- Integrates with timer state (Start/Pause)
- Auto-snooze feature: reminds again after 10 minutes if timer still running
- Uses `System.Timers.Timer` for scheduling

**Key Methods**:
```csharp
public void OnTimerStarted()   // Schedule reminder when timer starts
public void OnTimerPaused()    // Cancel pending reminders when paused
```

**Integration**: Injected into `TimerWidgetViewModel` and called on timer start/pause.

#### `HotkeyDefinition`
**Location**: `FocusTimer.Core/Models/HotkeyDefinition.cs`

**Features**:
- Struct representing hotkey combinations (modifiers + key)
- `HotkeyModifiers` enum: Alt, Control, Shift, Win
- Parse method: `"Ctrl+Alt+T"` ? `HotkeyDefinition`
- ToString: formats back to readable string

### Windows-Specific Implementations

#### `WindowsNotificationService`
**Location**: `FocusTimer.Platform.Windows/WindowsNotificationService.cs`

**Current Implementation**:
- Simple Debug.WriteLine output for notifications
- Implements `INotificationService` interface

**TODO** (for production):
- Option 1: Windows Toast Notifications (Windows.UI.Notifications)
- Option 2: Balloon tips via System.Windows.Forms.NotifyIcon
- Option 3: Custom Avalonia notification windows

#### `WindowsAutoStartService`
**Location**: `FocusTimer.Platform.Windows/WindowsAutoStartService.cs`

**Implementation**:
- Uses Windows Registry: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- Writes executable path on enable
- Removes registry entry on disable
- Graceful error handling (logs errors, doesn't crash)

**Key Details**:
- Uses `Environment.ProcessPath` to get current executable
- Wraps path in quotes to handle spaces
- Syncs registry state with settings on load

#### `WindowsHotkeyService`
**Location**: `FocusTimer.Platform.Windows/WindowsHotkeyService.cs`

**Implementation**:
- P/Invoke: `RegisterHotKey` / `UnregisterHotKey` from user32.dll
- Requires window handle (HWND) to receive `WM_HOTKEY` messages
- Maps hotkey IDs to callback Actions
- Thread-safe callback invocation with error handling

**Key Details**:
- Modifiers: MOD_ALT (0x0001), MOD_CONTROL (0x0002), MOD_SHIFT (0x0004), MOD_WIN (0x0008)
- Window handle set via `SetWindowHandle(IntPtr hwnd)` after window creation
- `ProcessHotkeyMessage(int hotkeyId)` called from window message loop

**Note**: Avalonia doesn't expose Win32 message loop by default. Current implementation sets the handle but doesn't hook into messages. For full functionality, consider:
- Using Avalonia's platform-specific APIs to hook WndProc
- Or use a hidden helper window for hotkey messages

### Linux Stub Implementations

All located in `FocusTimer.Core/Stubs/`:

#### `LinuxNotificationServiceStub`
- Debug output only
- **TODO**: Implement using `notify-send` command or DBus notifications

#### `LinuxAutoStartServiceStub`
- Placeholder implementation
- **TODO**: Create/manage `.desktop` file in `~/.config/autostart/`
- Desktop file format:
  ```ini
  [Desktop Entry]
  Type=Application
  Name=FocusTimer
  Exec=/path/to/FocusTimer
  X-GNOME-Autostart-enabled=true
  ```

#### `LinuxHotkeyServiceStub`
- No-op implementation
- **TODO**: Requires X11 XGrabKey or Wayland-specific APIs
- Note: Complex on Linux due to display server differences

## Dependency Injection Updates

**Location**: `FocusTimer.App/Program.cs`

Added platform-specific service registration:
```csharp
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    services.AddSingleton<INotificationService, Platform.Windows.WindowsNotificationService>();
    services.AddSingleton<IAutoStartService, Platform.Windows.WindowsAutoStartService>();
    services.AddSingleton<IHotkeyService, Platform.Windows.WindowsHotkeyService>();
}
else
{
    services.AddSingleton<INotificationService, LinuxNotificationServiceStub>();
    services.AddSingleton<IAutoStartService, LinuxAutoStartServiceStub>();
    services.AddSingleton<IHotkeyService, LinuxHotkeyServiceStub>();
}

// Break reminder service (cross-platform)
services.AddSingleton<BreakReminderService>();
```

## ViewModel Integration

### `TimerWidgetViewModel` Changes

**Added**:
- `BreakReminderService` constructor parameter
- Call `_breakReminderService.OnTimerStarted()` in `StartAsync()`
- Call `_breakReminderService.OnTimerPaused()` in `PauseAsync()` and `Dispose()`

### `SettingsWindowViewModel` Changes

**Added**:
- `IAutoStartService` constructor parameter
- `LoadSettingsAsync()`: Syncs `Settings.AutoStartOnLogin` with registry state
- `ApplyAsync()`: Calls `_autoStartService.SetAutoStart()` before saving settings

## AppController Updates

**Location**: `FocusTimer.App/Services/AppController.cs`

**New Methods**:
```csharp
public void RegisterHotkeys()
{
    // Register Ctrl+Alt+T for show/hide
    // Register Ctrl+Alt+P for toggle timer
}
```

**Changes**:
- Constructor: Added `IHotkeyService` and `INotificationService` parameters
- `OnSettingsApplied()`: Re-registers hotkeys when settings change
- `ExitApplicationAsync()`: Unregisters all hotkeys on exit

## Window Integration

### `TimerWidgetWindow` Changes

**Location**: `FocusTimer.App/Views/TimerWidgetWindow.axaml.cs`

**Added**:
```csharp
private void OnWindowOpenedForHotkeys(object? sender, EventArgs e)
{
    // Get native window handle using TryGetPlatformHandle()
    // Set handle on WindowsHotkeyService
}
```

**Platform-specific**: Only hooks up on Windows via `RuntimeInformation.IsOSPlatform(OSPlatform.Windows)`

## Settings UI Updates

**Location**: `FocusTimer.App/Views/SettingsWindow.axaml`

### General Tab Additions:
- **Break Reminders**:
  - `CheckBox`: Enable/disable break reminders
  - `NumericUpDown`: Break interval in minutes (1-240, step 5)
  - Interval control disabled when reminders are off

### New Hotkeys Tab:
- Read-only `TextBox` for `HotkeyShowHide` (default: Ctrl+Alt+T)
- Read-only `TextBox` for `HotkeyToggleTimer` (default: Ctrl+Alt+P)
- Note: "Editable hotkeys coming soon"
- Tip: "Changes require restart"

## Settings Model Updates

**Location**: `FocusTimer.Core/Models/Settings.cs`

Already had properties from previous milestones:
- `BreakIntervalMinutes` (default: 50)
- `BreakRemindersEnabled` (default: true)
- `AutoStartOnLogin` (default: false)
- `HotkeyShowHide` (nullable string)
- `HotkeyToggleTimer` (nullable string)

## App Initialization Flow

**Location**: `FocusTimer.App/App.axaml.cs`

Updated `InitializeAppAsync()`:
```csharp
1. await _appController.InitializeAsync()  // Load settings
2. Show widget if !StartMinimized
3. _appController.RegisterHotkeys()        // Register after window shown
```

**Reason for order**: Windows hotkeys need a valid window handle, so we register after showing the widget window.

## Default Hotkeys

| Action | Hotkey | Editable |
|--------|--------|----------|
| Show/Hide Widget | Ctrl+Alt+T | Not yet (TODO) |
| Toggle Timer | Ctrl+Alt+P | Not yet (TODO) |

## Break Reminder Behavior

1. **Timer Started**: Schedule reminder for X minutes (from settings)
2. **Reminder Fires**: Show notification + auto-snooze for 10 minutes
3. **Timer Paused/Stopped**: Cancel all pending reminders
4. **Timer Resumed**: Start new reminder countdown

## Error Handling

All platform services use defensive programming:
- Try/catch around P/Invoke calls
- Debug.WriteLine for errors (no crashes)
- Graceful degradation: App works even if hotkeys/notifications fail

## Testing Notes

### Windows Testing
- [ ] Break reminders fire after configured interval
- [ ] Auto-start registry key created/removed correctly
- [ ] Hotkeys work globally (even when app unfocused)
- [ ] Hotkeys don't conflict with other apps
- [ ] Settings changes re-register hotkeys

### Linux Testing (with stubs)
- [x] App builds and runs without errors
- [x] Stub services log debug messages
- [ ] TODO: Implement real Linux services

## Known Limitations

1. **Hotkey Message Processing**:
   - Current implementation sets window handle but doesn't fully hook WM_HOTKEY messages
   - May require custom Avalonia platform integration or helper window
   
2. **Notifications**:
   - Windows: Currently debug output only
   - Needs Toast or balloon tip implementation
   
3. **Hotkey Editing**:
   - Settings UI shows hotkeys as read-only
   - Editing UI requires custom key capture control

4. **Linux**:
   - All features are stubs
   - X11/Wayland implementation needed

## Future Enhancements

1. **Editable Hotkeys**: Custom control to capture key combinations
2. **Notification Actions**: Snooze/dismiss buttons in notifications
3. **Multiple Break Types**: Short break (5 min) vs long break (15 min)
4. **Break Statistics**: Track break compliance
5. **Linux Implementation**: Full feature parity with Windows

## Files Added/Modified

### New Files:
- `src/FocusTimer.Core/Interfaces/IAutoStartService.cs`
- `src/FocusTimer.Core/Services/BreakReminderService.cs`
- `src/FocusTimer.Core/Models/HotkeyDefinition.cs`
- `src/FocusTimer.Platform.Windows/WindowsNotificationService.cs`
- `src/FocusTimer.Platform.Windows/WindowsAutoStartService.cs`
- `src/FocusTimer.Platform.Windows/WindowsHotkeyService.cs`
- `src/FocusTimer.Core/Stubs/LinuxNotificationServiceStub.cs`
- `src/FocusTimer.Core/Stubs/LinuxAutoStartServiceStub.cs`
- `src/FocusTimer.Core/Stubs/LinuxHotkeyServiceStub.cs`

### Modified Files:
- `src/FocusTimer.App/Program.cs` (DI registration)
- `src/FocusTimer.App/App.axaml.cs` (hotkey initialization)
- `src/FocusTimer.App/Services/AppController.cs` (hotkey management)
- `src/FocusTimer.App/ViewModels/TimerWidgetViewModel.cs` (break reminders)
- `src/FocusTimer.App/ViewModels/SettingsWindowViewModel.cs` (auto-start)
- `src/FocusTimer.App/Views/SettingsWindow.axaml` (UI for new settings)
- `src/FocusTimer.App/Views/TimerWidgetWindow.axaml.cs` (window handle setup)

## Build Status

? **Build Successful** — All features compile and run on Windows.

Linux compatibility maintained via stub implementations.
