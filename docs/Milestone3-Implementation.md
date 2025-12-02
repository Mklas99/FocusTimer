# Milestone 3 Implementation: Active Window Tracking & CSV Logging

## Overview
This milestone implements active window tracking on Windows and CSV logging functionality that automatically captures and logs time spent in different applications.

## Architecture

### Core Components

#### 1. WindowsActiveWindowService (`FocusTimer.Platform.Windows`)
**Purpose**: Detects the currently active foreground window on Windows using Win32 APIs.

**Key Features**:
- Uses P/Invoke to call Win32 APIs: `GetForegroundWindow`, `GetWindowThreadProcessId`, `GetWindowText`
- Returns `ActiveWindowInfo` with process name and window title
- Handles edge cases:
  - Process access denied ? Falls back to `Process_{processId}`
  - Process not found ? Returns "Unknown"
  - No foreground window ? Returns null
- Never crashes on Win32 errors; returns null instead

**Win32 APIs Used**:
```csharp
GetForegroundWindow()           // Gets handle of active window
GetWindowThreadProcessId()      // Gets process ID from window handle
GetWindowText()                 // Gets window title text
```

#### 2. SessionTracker (`FocusTimer.Core.Services`)
**Purpose**: Tracks window changes and creates time entry segments.

**Responsibilities**:
- Maintains current open `TimeEntry` segment
- Polls active window on each timer tick (1 second interval)
- Detects window changes by comparing process name + window title
- Closes previous segment and starts new one when window changes
- Filters out entries shorter than 1 second (noise reduction)

**State Management**:
- `_isTracking`: Prevents duplicate starts
- `_currentEntry`: Currently open time segment
- `_currentWindowInfo`: Last detected window
- `_completedEntries`: List of closed segments ready for logging

**Key Methods**:
- `StartAsync(projectTag)`: Begins tracking with initial window detection
- `OnTimerTickAsync()`: Called every second to check for window changes
- `UpdateProjectTag(tag)`: Updates tag on current entry without closing it
- `CollectAndResetSegments()`: Returns all entries and clears state

#### 3. CsvLogWriter (`FocusTimer.Persistence`)
**Purpose**: Writes time entries to CSV files with date-based organization.

**File Structure**:
```
{LogDirectory}/
  ??? {YYYY}/
      ??? {MM}/
          ??? {YYYY-MM-DD}-worklog.csv
```

**CSV Format**:
```csv
Date,StartTime,EndTime,DurationSeconds,AppName,WindowTitle,ProjectTag
2024-01-15,10:30:15,10:32:45,150,chrome,"GitHub - Project",Development
```

**Key Features**:
- Creates directories automatically if they don't exist
- Writes header only when creating new file
- Properly escapes CSV fields (quotes, commas, newlines)
- Groups entries by date (handles sessions that span midnight)
- Uses async I/O with proper file sharing (`FileShare.Read`)
- Validates entries before writing (skips invalid entries)

### Integration Flow

```
User Presses Play
    ?
TimerWidgetViewModel.StartAsync()
    ?
SessionTracker.StartAsync(projectTag)
    ?
    Creates first TimeEntry with current window
    ?
Timer Ticks (every 1 second)
    ?
TimerWidgetViewModel.OnTimerTick()
    ?
SessionTracker.OnTimerTickAsync()
    ?
    Checks if window changed
    ?
    If changed: Close current entry, start new one
    ?
User Presses Pause
    ?
TimerWidgetViewModel.PauseAsync()
    ?
SessionTracker.CollectAndResetSegments()
    ?
    Returns all completed entries
    ?
CsvLogWriter.WriteEntriesAsync(entries, settings)
    ?
    Entries written to CSV file
```

## Error Handling Strategy

### Non-Crashing Design
All components are designed to **never crash the application**:

1. **WindowsActiveWindowService**:
   - Catches all Win32 exceptions
   - Returns null on any error
   - Logs errors to Debug output

2. **SessionTracker**:
   - Wraps all operations in try-catch
   - Continues tracking even if one tick fails
   - Creates fallback entries if window detection fails

3. **CsvLogWriter**:
   - Validates input before writing
   - Continues with other dates if one fails
   - Uses proper file locking to avoid conflicts

4. **TimerWidgetViewModel**:
   - Uses ContinueWith to log async errors
   - Falls back to cached settings if reload fails
   - Best-effort flush on disposal

### Error Logging
All errors are logged to `System.Diagnostics.Debug`:
- View errors in Visual Studio Output window (Debug pane)
- Can be captured by debugger or logging infrastructure
- Never shown to user (TODO: add user notifications in Milestone 4)

## Cross-Platform Support

### Windows (Implemented)
- ? Active window tracking via Win32 APIs
- ? CSV logging (cross-platform code)

### Linux (Stub)
- ?? `LinuxActiveWindowServiceStub` always returns null
- ? CSV logging works (file I/O is cross-platform)
- TODO: Implement X11/Wayland active window detection

**DI Configuration** (`Program.cs`):
```csharp
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    services.AddSingleton<IActiveWindowService, WindowsActiveWindowService>();
}
else
{
    services.AddSingleton<IActiveWindowService, LinuxActiveWindowServiceStub>();
}
// CsvLogWriter works on both platforms
services.AddSingleton<ILogWriter, CsvLogWriter>();
```

## Testing Recommendations

### Manual Testing
1. **Basic Flow**:
   - Start timer ? Switch between apps ? Pause ? Check CSV file

2. **Window Changes**:
   - Verify entries created when switching apps
   - Check entries have correct process names and titles

3. **Edge Cases**:
   - Timer running with no window switches (should create 1 entry)
   - Switching to/from system windows (Task Manager, etc.)
   - Very rapid window switches (< 1 second)

4. **Project Tags**:
   - Start with tag ? entries should have tag
   - Change tag mid-session ? new entries get new tag
   - Old entries keep old tag

5. **Error Conditions**:
   - Invalid log directory ? should log error, not crash
   - File locked by another process ? should handle gracefully
   - Process access denied ? should show "Process_" + ID

### Unit Testing (Future)
**SessionTracker**:
- Mock `IActiveWindowService` to simulate window changes
- Verify entries created/closed correctly
- Test edge cases (null windows, rapid changes)

**CsvLogWriter**:
- Verify CSV format is correct
- Test field escaping (commas, quotes, newlines)
- Verify directory creation
- Test date-based file paths

**WindowsActiveWindowService**:
- Difficult to unit test (Win32 dependencies)
- Consider integration tests on real Windows

## Performance Considerations

### Polling Interval
- **Current**: 1 second (same as UI update)
- **Impact**: Low overhead, acceptable for desktop app
- **Optimization**: Could reduce to 0.5s for more granular tracking

### Memory Usage
- Entries held in memory until pause
- Long sessions (hours) = many entries
- **Mitigation**: Could periodically flush to disk (not implemented)

### File I/O
- Async operations avoid blocking UI
- FileStream with buffering (4KB)
- Append-only operations are fast

## Configuration

### Settings Used
```csharp
Settings.LogDirectory  // Where to write CSV files
```

### Future Settings (Milestone 4+)
- Track system/idle time: yes/no
- Minimum entry duration: X seconds
- Auto-flush interval: X minutes

## Known Limitations

1. **Windows Only**: Active tracking doesn't work on Linux yet
2. **Polling-Based**: May miss very brief window switches (< 1 second)
3. **No Idle Detection**: Counts time even if user is away
4. **Single-Threaded**: All tracking happens on UI thread via DispatcherTimer
5. **No Retroactive Logging**: If app crashes, unsaved entries are lost

## Future Enhancements (Next Milestones)

### Milestone 4
- User notifications for log write errors
- Settings UI to configure log directory

### Milestone 5
- Idle time detection
- Break reminders

### Post-Milestone 5
- Configurable polling interval
- Periodic auto-flush (every N minutes)
- Linux X11 implementation
- SQLite storage option (in addition to CSV)
- Data retention / cleanup (Settings.DataRetentionDays)
