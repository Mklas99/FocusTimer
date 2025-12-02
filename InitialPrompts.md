### Prompt 1 — Project Setup & Architecture Skeleton
C — Context

You are helping me build a lightweight, cross-platform desktop application:

Name: (working title) "FocusTimer"  
Tech stack:  
- .NET 8 (or latest LTS)  
- C#  
- Avalonia UI (latest stable)  

Primary OS: Windows 10/11  
Later: Linux (X11/XWayland first; pure Wayland can come later).

Goal of the app: an always-on-top “widget-like” timer that helps me track time spent per application/project with minimal friction.

High-level features (to keep in mind for later milestones):
- Always-on-top widget window with:
  - Timer display
  - Play/Pause button
  - Optional project/task label
  - Adjustable opacity and size/scale
  - Compact mode (very small bar view)
- Background tracking:
  - Detect currently active window (app process name + window title)
  - Attribute time spent to that app (and optionally project label)
- System tray icon:
  - Show/hide widget
  - Start/pause timer
  - Open Settings
  - Exit app
- Settings UI:
  - Break reminder interval (e.g. 50 minutes)
  - Log directory selection
  - Widget opacity and size options
  - Toggle always-on-top, start minimized to tray
  - (Later) global hotkeys configuration
- Logging:
  - Simple exportable logs (CSV to start)
  - Folder structure like logs/YYYY/MM/YYYY-MM-DD-worklog.csv

This prompt is for **Milestone 1 only**: project setup and architecture skeleton. Do NOT implement all features. Your output must set me up with a clean, maintainable solution that I can extend with further prompts.

R — Role

You are a senior C#/.NET desktop engineer and Avalonia expert.

You:
- Know how to structure multi-project .NET solutions.
- Have experience building cross-platform Avalonia apps.
- Understand separation of concerns (UI, core logic, OS-specific integration, persistence).
- Design for Windows-first but keep Linux portability in mind.

A — Actions

1. Design the solution structure
   - Propose a .NET 8 solution layout with clear projects, for example:
     - `FocusTimer.App` (Avalonia UI entry point, Windows & Linux)
     - `FocusTimer.Core` (timer logic, session model, settings model, interfaces)
     - `FocusTimer.Platform.Windows` (Win32 interop: active window detection, notifications, hotkeys — for later)
     - `FocusTimer.Persistence` (settings + logging; can be in Core if you prefer, but justify)
   - Explain briefly why you chose this structure and how it supports future Linux support.

2. Create the initial Avalonia app
   - Use the recommended way (Avalonia templates) to bootstrap the app for .NET 8.
   - Configure:
     - A main App class
     - A single initial MainWindow (temporary, will later be replaced/augmented by the widget window).
   - Apply basic project settings:
     - Target .NET 8
     - Use nullable reference types
     - Configure app to run on Windows and Linux.

3. Define core domain models & interfaces (no heavy implementation yet)
   - Define simple C# classes/interfaces in `FocusTimer.Core` for:
     - `TimeEntry` (start/end, duration, appName, windowTitle, optional projectTag)
     - Optional `Session` wrapper (start/end, list of TimeEntry, projectTag)
     - `Settings` (properties only; include: autoStartOnLogin, startMinimized, alwaysOnTop, breakIntervalMinutes, logDirectory, widgetOpacity, widgetScale, useCompactMode, dataRetentionDays, etc.)
   - Define key interfaces (no implementation yet):
     - `IActiveWindowService` (e.g. `Task<ActiveWindowInfo?> GetForegroundWindowAsync()` or similar)
     - `ILogWriter` (for appending entries to logs)
     - `ISettingsProvider` (load/save Settings)
     - `INotificationService` (to show break reminders later)
     - `IHotkeyService` (for global hotkeys later)
   - Make these interfaces framework-agnostic (no Avalonia types).

4. Implement a minimal “shell” UI
   - Create a simple placeholder `MainWindow` in Avalonia that:
     - Shows a label “FocusTimer – skeleton” and a dummy button.
     - Loads Settings (from a stub provider or default instance) and displays one setting value in the UI to validate plumbing (e.g. show `LogDirectory` somewhere).
   - Wire up dependency injection if you recommend it (e.g. using `Microsoft.Extensions.DependencyInjection`).
   - Setup a simple composition root in `FocusTimer.App` that:
     - Registers the core services/interfaces with placeholder implementations (`ActiveWindowServiceStub`, `LogWriterStub`, etc.).
     - Passes them to the UI (via constructor injection, view models, or a simple service locator — but explain your choice).

5. Document how to build & run
   - Provide the CLI commands to:
     - Create the solution and projects
     - Add Avalonia
     - Add project references
     - Build and run the app on Windows
   - If there are any extra steps for Linux (even if not fully tested yet), mention them briefly.

F — Format

- Provide:
  - A clear folder / project structure (as a tree).
  - The most important `.csproj` definitions (not every property, only relevant ones).
  - The key C# files:
    - `Program.cs` / `App.axaml.cs`
    - Skeleton `MainWindow.axaml` / `.cs`
    - `TimeEntry`, `Settings`, interfaces
    - Stub service implementations
  - Use fenced code blocks with language tags (`csharp`, `xml`, `bash`) where appropriate.
- Organize your answer with headings:
  - “Solution Structure”
  - “Project Setup Commands”
  - “Core Models & Interfaces”
  - “Initial Avalonia App & Composition Root”
  - “How to Build & Run”

T — Tone & Target Audience

- Audience: experienced developer comfortable with .NET, C#, and Avalonia basics.
- Tone: pragmatic, direct, implementation-focused.
- Don’t explain C# or Avalonia fundamentals; focus on concrete structure and code.
- Prefer clear, concise explanations + ready-to-use code snippets.

----------------------------------------------------------------------------------------------

### Prompt 2 — Timer Widget UI & Core Timer Logic
C — Context

We are continuing the “FocusTimer” app:

Stack:
- .NET 8
- C#
- Avalonia UI

Solution structure and basic skeleton already exist (from a previous step):
- `FocusTimer.App` – Avalonia UI entry point.
- `FocusTimer.Core` – models (TimeEntry, Settings) and interfaces (IActiveWindowService, ILogWriter, ISettingsProvider, etc.).
- Dependency injection / composition root is wired.

New goal (Milestone 2):
Implement the **always-on-top timer widget window** and core timer logic. We *do not* yet implement active window tracking, logging, tray icon, or break reminders — just the widget and internal timer.

Widget requirements (for this milestone):
- A small, borderless (or minimal) window that:
  - Is always-on-top (Topmost)
  - Shows:
    - Elapsed time (HH:MM:SS)
    - Play/Pause toggle button
    - Optional project/task text field (manual entry for now)
- Supports:
  - Adjustable opacity from Settings (via `Settings.WidgetOpacity`)
  - Adjustable scale/size from Settings (via `Settings.WidgetScale`)
- Behaviors:
  - Play starts a timer counting up from 00:00:00 and sets internal state to “running”.
  - Pause stops the timer, but does not reset to 0.
  - A “Reset” action is helpful (may be via right-click or secondary button, or temporary in UI).
  - When app starts:
    - Read Settings and initialize widget’s opacity, scale, and AlwaysOnTop flag.
    - Timer should be paused initially.

R — Role

You are a senior Avalonia/C# engineer.

You:
- Are comfortable designing MVVM-style UIs in Avalonia.
- Know how to implement timers without blocking UI (e.g., `DispatcherTimer`, `System.Timers.Timer`, or reactive patterns).
- Design the UI so it’s simple, clean, and easily theme-able later.

A — Actions

1. Introduce a dedicated Widget window
   - Define a new Avalonia `Window` (e.g. `TimerWidgetWindow`) separate from any debug MainWindow.
   - Make it minimal and widget-like:
     - Always-on-top (Topmost=true or equivalent).
     - No heavy chrome (window decorations minimal or none, configurable if needed).
   - Decide where to show it on startup (e.g. center screen, or bottom-right). Use a simple default for now.

2. Implement a ViewModel for the widget
   - Create a `TimerWidgetViewModel` in `FocusTimer.Core` or `FocusTimer.App` (explain your choice).
   - ViewModel responsibilities:
     - Properties:
       - `TimeSpan Elapsed` (or string `ElapsedFormatted`)
       - `bool IsRunning`
       - `string? ProjectTag`
       - Commands: `StartCommand`, `PauseCommand`, `ToggleCommand`, `ResetCommand`
     - Timer logic:
       - Use a timer mechanism (e.g. `DispatcherTimer`) that ticks every 1 second (or 500ms) and increments Elapsed only when `IsRunning` is true.
       - Ensure the timer runs on the UI thread or dispatches updates appropriately.
   - Wire up commands to Play/Pause buttons in XAML.

3. Bind Settings to widget appearance
   - The window should react to `Settings.WidgetOpacity`, `Settings.WidgetScale`, and `Settings.AlwaysOnTop`.
   - For now:
     - Read Settings at startup and apply:
       - `Opacity = WidgetOpacity`
       - `Topmost = AlwaysOnTop`
       - Use `WidgetScale` to scale the root layout (e.g. via `LayoutTransform` or FontSize multipliers).
   - Provide methods to re-apply settings if they change at runtime (this will be used later when Settings UI is implemented).
   - If you need a small “SettingsService” to retrieve the current settings instance, use the `ISettingsProvider` interface.

4. Design the XAML layout
   - Create `TimerWidgetWindow.axaml` with:
     - A horizontal layout (e.g. StackPanel / Grid) with:
       - Time label
       - Play/Pause button (toggle content or separate icons)
       - Simple `TextBox` for ProjectTag
     - Use basic styling suitable for both dark/light themes (but don’t overcomplicate).
   - Ensure the layout looks good at different scales (WidgetScale = 1.0, 1.5, 2.0).
   - Make sure tab order is logical, and keyboard focus doesn’t trap user.

5. Integrate into app startup
   - In `App.OnFrameworkInitializationCompleted` (or equivalent), instead of showing MainWindow, show `TimerWidgetWindow` with its ViewModel.
   - Keep any existing “debug” window optional (e.g. only in debug builds).
   - Ensure closing the window closes the app for now (tray behavior will come later).

6. Include a minimal unit/integration test plan
   - (You don’t need to write real tests, but outline what you would test and where.)
   - Example:
     - Timer increments Elapsed only when IsRunning = true.
     - Reset sets Elapsed = 0 and IsRunning = false.
     - Applying Settings updates opacity/scale correctly.

F — Format

- Provide code snippets for:
  - `TimerWidgetWindow.axaml` and `.cs`
  - `TimerWidgetViewModel`
  - Timer initialization and tick logic
  - Updated `App` startup code
- Use `csharp` and `xml` fenced code blocks as appropriate.
- Add short explanations before/after key snippets, but stay focused on implementation.

T — Tone & Target Audience

- Audience: experienced C#/Avalonia developer (me).
- Tone: direct, practical, minimal fluff.
- Emphasize clear, maintainable code and good separation between View and ViewModel.


----------------------------------------------------------------------------------------------

### Prompt 3 — Active Window Tracking & CSV Logging (Windows-first)
C — Context

The FocusTimer app now has:
- A working Avalonia-based widget window (`TimerWidgetWindow`) with Play/Pause and internal timer logic.
- Core models: `TimeEntry`, `Settings`.
- Core interfaces: `IActiveWindowService`, `ILogWriter`, `ISettingsProvider`, etc.
- Basic Settings and DI wiring.

New goal (Milestone 3):
Implement **active window tracking on Windows** and **CSV logging**, and connect them to the running timer:

- When timer is running:
  - Detect the current foreground application and window title.
  - Create `TimeEntry` segments whenever the active app/window changes or the timer is paused/stopped.
- When timer is paused/stopped:
  - Flush the in-memory segments to a CSV log file for the current day in a configurable log directory.

Linux implementation can be a stub for now; focus on Windows.

R — Role

You are a senior C#/.NET engineer with Win32 interop experience.

You:
- Know how to use P/Invoke with User32 APIs.
- Can design a clean service that hides OS-specific details behind `IActiveWindowService`.
- Understand file I/O and CSV writing in .NET.

A — Actions

1. Implement `IActiveWindowService` for Windows
   - In `FocusTimer.Platform.Windows`, create `WindowsActiveWindowService` that implements `IActiveWindowService`.
   - Use P/Invoke to call:
     - `GetForegroundWindow`
     - `GetWindowThreadProcessId`
     - `GetWindowTextW`
   - Define a small DTO like `ActiveWindowInfo` with:
     - `string ProcessName`
     - `string WindowTitle`
   - Provide a method like `ActiveWindowInfo? GetActiveWindow()` (sync) or async equivalent.
   - Handle edge cases:
     - If there is no foreground window, return null.
     - If process info cannot be resolved, still return window title if possible.

2. Wire `IActiveWindowService` into the core timer
   - Introduce a `SessionTracker` or similar class in `FocusTimer.Core`:
     - Responsibilities:
       - Maintain current `ActiveWindowInfo` and current open `TimeEntry` segment.
       - On each “tick” (e.g. every second while timer is running), check if active window has changed.
       - If changed, close the previous segment (`EndTime = now`, compute duration) and start a new `TimeEntry` with new app/window and current projectTag.
       - On timer pause/stop, close the last segment.
       - Expose a method to return completed segments for flushing (and then clear them), e.g. `IReadOnlyList<TimeEntry> CollectAndResetSegments()`.
   - Decide where the polling loop lives:
     - Option 1: Use a dedicated polling timer in `SessionTracker`.
     - Option 2: Reuse the existing UI timer tick that already updates Elapsed.
   - Implement a clean API so the widget’s ViewModel or a higher-level `TimerController` can start/stop tracking in sync with the timer.

3. Implement CSV `ILogWriter`
   - Create `CsvLogWriter` in `FocusTimer.Persistence` (or Core if you decided that earlier).
   - It should:
     - Accept `Settings.LogDirectory` and ensure the directory & date-based subfolders exist.
     - Determine the log file path for current day, e.g.: `{LogDirectory}/{YYYY}/{MM}/{YYYY-MM-DD}-worklog.csv`.
     - Create file with header if it doesn’t exist.
     - Append lines for each `TimeEntry`:
       - Fields: Date, StartTime, EndTime, DurationSeconds, AppName, WindowTitle, ProjectTag.
       - Use a consistent format (e.g. ISO 8601 for date/time) and proper CSV escaping.
   - Provide a method like:
     - `Task WriteEntriesAsync(IEnumerable<TimeEntry> entries, Settings settings);`

4. Integrate everything in the timer lifecycle
   - When user presses Play:
     - Start the timer AND start/enable active window tracking (SessionTracker).
   - On each tick (or at reasonable intervals):
     - SessionTracker checks the active window and manages segments.
   - When user presses Pause or when timer is stopped:
     - Ask SessionTracker to close the current segment and return all collected segments.
     - Pass those segments to `ILogWriter` to append to CSV.
     - Clear segments from memory.
   - Ensure that if timer is running but user never switches windows, we still get at least one TimeEntry on pause.

5. Add basic error handling & diagnostics
   - Implement logging or at least try/catch:
     - If `WindowsActiveWindowService` fails, it should not crash the app; just skip that tick.
     - If writing to CSV fails (e.g., due to path or IO error), display a minimal fallback (e.g. debug log) and avoid crashing.
   - Add a TODO comment or stub for Linux `IActiveWindowService` implementation (for future).

F — Format

- Provide code snippets for:
  - `WindowsActiveWindowService` with P/Invoke declarations.
  - `ActiveWindowInfo` DTO.
  - `SessionTracker` (or whatever you call the class responsible for window-based segmentation).
  - `CsvLogWriter`.
  - Changes to ViewModel / Timer control to integrate tracking & logging.
- Use `csharp` code fences.
- Add small comments explaining tricky parts (P/Invoke, file paths), but keep narrative short.

T — Tone & Target Audience

- Audience: experienced C# dev.
- Tone: precise, practical, small justifications where design decisions matter.
- Avoid overexplaining Win32 basics; focus on how to integrate them cleanly into the app.


----------------------------------------------------------------------------------------------

### Prompt 4 — System Tray Icon, Show/Hide Logic & Settings Window
C — Context

The FocusTimer app now has:
- Timer widget window with Play/Pause and projectTag.
- Active window tracking on Windows via `IActiveWindowService`.
- Session tracking and CSV logging via `ILogWriter`.
- Settings model & provider (with at least LogDirectory, WidgetOpacity, WidgetScale, AlwaysOnTop, etc.).

New goal (Milestone 4):
Add a **system tray icon**, **show/hide widget behavior**, and a **basic Settings UI** that allows changing key preferences.

Requirements:

System Tray:
- Use Avalonia `TrayIcon` to show an icon in the system tray on Windows.
- Tray menu should include:
  - Show/Hide Timer
  - Start/Pause Timer
  - Settings…
  - Exit
- Left-click or double-click on tray should toggle widget visibility (if supported).

Show/Hide Widget Behavior:
- Closing/hiding the widget should NOT exit the app.
- App should continue tracking (if timer running) even with widget hidden.
- “Exit” from tray should cleanly stop timer, flush logs, and exit.

Settings Window:
- A new window with basic tabs/sections:
  - General: `AutoStartOnLogin`, `StartMinimized`, `AlwaysOnTop`
  - Logging: `LogDirectory`
  - Appearance: `WidgetOpacity`, `WidgetScale` (or Size mode)
- Changes should be saved to Settings and applied to widget.

R — Role

You are a senior Avalonia engineer with experience using TrayIcon and basic dialogs.

You:
- Know how to instantiate and configure `TrayIcon` in Avalonia.
- Can design a small Settings window with data-binding to the Settings model.
- Can manage app lifecycle (don’t exit when widget closes; only when explicitly exiting from tray).

A — Actions

1. Add a TrayIcon with menu in Avalonia
   - In `App.xaml` or equivalent, define a `TrayIcon` (or programmatically in App initialization).
   - Use a simple icon resource (placeholder is fine).
   - Attach a `NativeMenu` with items:
     - `Show/Hide Timer`
     - `Start/Pause`
     - `Settings…`
     - `Exit`
   - Implement click handlers:
     - `Show/Hide` toggles visibility of `TimerWidgetWindow`.
     - `Start/Pause` invokes the same logic as widget’s Play/Pause.
     - `Settings…` opens the Settings window.
     - `Exit` shuts down the app gracefully.

2. Implement proper show/hide behaviors
   - Override the close behavior of `TimerWidgetWindow`:
     - Instead of exiting app, hide the window (e.g. handle Closing event, `e.Cancel = true`, call `Hide()`).
     - Timer should continue working in background if running.
   - Ensure there’s a central place (e.g. an `AppController` or simple service) that keeps a reference to `TimerWidgetWindow` and can show/hide it from tray menu or hotkeys later.

3. Implement a Settings window with data-binding
   - Create `SettingsWindow.axaml` and `SettingsWindowViewModel`.
   - Bind `SettingsWindowViewModel` to the `Settings` object:
     - Fields:
       - General: `AutoStartOnLogin`, `StartMinimized`, `AlwaysOnTop`
       - Logging: `LogDirectory`
       - Appearance: `WidgetOpacity`, `WidgetScale`
   - Use standard Avalonia controls:
     - CheckBoxes for booleans
     - Slider for opacity/scale
     - Button + text field for LogDirectory (opens folder picker).
   - On “Apply” or “OK”:
     - Save settings via `ISettingsProvider`.
     - Apply them to `TimerWidgetWindow` (opacity, scale, always-on-top).
     - If `StartMinimized` or `AutoStartOnLogin` change, just store them for now; activation of auto-start can be implemented in the next milestone.

4. Update app startup behavior based on settings
   - If `StartMinimized` is true:
     - Do not show `TimerWidgetWindow` at startup; only show tray icon.
   - If `StartMinimized` is false:
     - Show `TimerWidgetWindow` as before.
   - Always create and show the tray icon on startup.

5. Manage app exit cleanly
   - Implement an `Exit` command/service that:
     - If timer is running, stops it and flushes any pending `TimeEntry` segments to CSV.
     - Disposes resources if necessary.
     - Then calls `Application.Current.Shutdown()` or equivalent.
   - Wire `Exit` tray menu item to this.

F — Format

- Provide:
  - Code snippets for:
    - TrayIcon definition (XAML or C#).
    - Updated `TimerWidgetWindow` close/hide behavior.
    - `SettingsWindow` XAML & ViewModel.
    - Changes in `App` for startup logic based on settings.
  - Brief notes on where icons/resources should live.
- Use `xml` and `csharp` code fences.
- Keep explanations short and focused on how the parts connect.

T — Tone & Target Audience

- Audience: me (developer, comfortable with Avalonia).
- Tone: straightforward, implementation-heavy, minimal handholding.
- Emphasize wiring and lifecycle (don’t forget to keep app alive when widget is hidden).


----------------------------------------------------------------------------------------------

### Prompt 5 — Break Reminders, Auto-start, Global Hotkeys & Linux Abstraction
C — Context

The FocusTimer app now has:
- Always-on-top widget with timer & projectTag.
- Active window tracking on Windows and CSV logging.
- Tray icon with show/hide & start/pause & Exit.
- Basic Settings window for core preferences.

New goal (Milestone 5):
Add **break reminders**, **auto-start on login (Windows)**, **basic global hotkeys (Windows)**, and prepare **cross-platform abstraction hooks** for Linux.

Requirements:

Break reminders:
- Use `Settings.BreakIntervalMinutes` and `Settings.BreakRemindersEnabled`.
- When timer has run for BreakIntervalMinutes without being paused:
  - Notify the user (Windows: simple toast or tray balloon).
  - Optionally auto-snooze (remind again later) if not acknowledged.

Auto-start on login (Windows):
- Use `Settings.AutoStartOnLogin`.
- Create/remove a Run entry in `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` for the app.
- Handle enabling/disabling from Settings.

Global hotkeys (Windows):
- Use `Settings.HotkeyShowHide` and `Settings.HotkeyToggleTimer` (or hard-coded combos if config is not yet supported).
- Register global hotkeys:
  - Show/hide widget.
  - Start/pause timer.
- Ensure they work even when app is not focused.

Linux:
- For now, implement stubs and todos only:
  - No global hotkeys.
  - Auto-start via `.desktop` file in `~/.config/autostart` (can be noted as TODO).
  - Notification: use a simple plugin (e.g. call `notify-send`) or leave a TODO with an interface.

R — Role

You are a senior C#/.NET/Avalonia engineer with experience in:
- Windows registry & auto-start.
- Windows global hotkeys (`RegisterHotKey`).
- Basic desktop notifications on Windows.

You:
- Know how to isolate platform-specific code behind interfaces.
- Can design break reminder logic tied to the timer state.

A — Actions

1. Implement break reminder scheduler
   - Add a `BreakReminderService` in `FocusTimer.Core` that:
     - Subscribes to timer state changes (Start, Pause).
     - On Start/Resume:
       - Schedule a reminder for `BreakIntervalMinutes` in the future.
     - On Pause:
       - Cancel any pending reminder (or reset the counter).
     - On reminder time reached:
       - Call `INotificationService.ShowBreakReminder(...)`.
   - For now, ignore snooze/skip actions (or implement a simple built-in snooze interval).
   - Use `Settings.BreakRemindersEnabled` to toggle this feature.

2. Implement notifications for Windows
   - In `FocusTimer.Platform.Windows`, implement `WindowsNotificationService : INotificationService`.
   - Choose a pragmatic method:
     - EITHER use `System.Windows.Forms.NotifyIcon.ShowBalloonTip` as a simple solution tied to the existing tray icon.
     - OR use Windows Toast Notifications if you know a clean, minimal approach.
   - Implement at least:
     - `ShowBreakReminder(string message)` — shows “Time to take a break!”.
   - Wire this in DI and use it from `BreakReminderService`.

3. Implement Windows auto-start
   - Add a service `WindowsAutoStartService` or integrate this into `ISettingsProvider` / a small `IAutoStartService` interface.
   - On Settings change (AutoStartOnLogin toggled):
     - If true: write a value into `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` with the app’s executable path.
     - If false: remove that key.
   - Take care to:
     - Resolve the correct path to the currently running executable.
     - Handle exceptions gracefully (no crash, just log error).
   - If needed, add a one-time check at startup to verify whether registry entry matches current setting and fix conflicts.

4. Implement Windows global hotkeys
   - Create `WindowsHotkeyService : IHotkeyService` with methods like:
     - `RegisterShowHideHotkey(HotkeyDefinition def, Action callback)`
     - `RegisterToggleTimerHotkey(HotkeyDefinition def, Action callback)`
     - `UnregisterAll()`
   - Implement using Win32:
     - P/Invoke `RegisterHotKey` and `UnregisterHotKey`.
     - Use an underlying window handle:
       - Either from `TimerWidgetWindow`’s native handle.
       - Or create a hidden helper window dedicated to receiving `WM_HOTKEY`.
     - Process `WM_HOTKEY` messages and dispatch callbacks.
   - Wire callbacks to:
     - Show/hide widget.
     - Toggle timer state.
   - If you prefer, define `HotkeyDefinition` as a simple struct with modifiers and key code.

5. Prepare for Linux with stubs
   - In DI setup, use:
     - `WindowsActiveWindowService` / `WindowsNotificationService` / `WindowsHotkeyService` on Windows.
     - Stub implementations on non-Windows (e.g. `NoopHotkeyService`, `ConsoleNotificationService`, etc.).
   - Add TODO comments and interface points for:
     - Linux active window tracking (X11).
     - Linux notification (DBus / `notify-send`).
     - Linux auto-start (`.desktop` file).
   - Ensure the app still builds and runs cleanly on non-Windows (even if features are no-ops).

6. Update Settings UI for new options
   - In `SettingsWindow`, add controls for:
     - `BreakRemindersEnabled` and `BreakIntervalMinutes`.
     - `AutoStartOnLogin`.
     - Display global hotkey combos (even if read-only for now or hard-coded).
   - Make sure changes call the appropriate services:
     - Auto-start service when toggling `AutoStartOnLogin`.
     - BreakReminderService picks up new interval (you can re-read settings or inject settings into it).

F — Format

- Provide:
  - Code snippets for:
    - `BreakReminderService`.
    - `WindowsNotificationService`.
    - Auto-start registry writer.
    - `WindowsHotkeyService` with P/Invoke and message handling.
    - DI configuration updates for platform services.
    - Updates to Settings UI for new fields.
  - Use `csharp` code fences.
- Keep explanations short; highlight tricky Win32/registry parts.

T — Tone & Target Audience

- Audience: experienced C# dev implementing system integration features.
- Tone: precise, focused, aware of platform limitations.
- Make clear where behavior is Windows-only and where Linux stubs live so I can extend later.
