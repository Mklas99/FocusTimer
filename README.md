## FocusTimer – Project Setup & Architecture Skeleton

This project is a cross-platform desktop timer widget built with .NET 8 and Avalonia UI.


### Solution Structure

- `FocusTimer.App` — Avalonia UI entry point (Windows & Linux)
- `FocusTimer.Core` — Core logic, models, interfaces (framework-agnostic)
- `FocusTimer.Platform.Windows` — Windows-specific integrations (active window, notifications, hotkeys)
- `FocusTimer.Persistence` — Logging and settings persistence

### Key Features (Milestone 1)

- Clean, extensible .NET 8 solution
- MVVM-ready Avalonia UI shell
- Core models: `TimeEntry`, `Session`, `Settings`
- Interfaces: `IActiveWindowService`, `ILogWriter`, `ISettingsProvider`, `INotificationService`, `IHotkeyService`
- Dependency injection with stub implementations
- Minimal MainWindow UI to validate plumbing

### Build & Run

#### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Avalonia Templates](https://docs.avaloniaui.net/docs/getting-started/installation)

#### CLI Commands
#### Linux Notes

- Avalonia supports Linux (X11/XWayland). No extra steps for basic UI.
- Platform-specific features (active window, notifications, hotkeys) are Windows-only for now; Linux stubs included.

---

See `FocusTimer.Core` for models/interfaces, and `FocusTimer.App` for the Avalonia shell and DI setup.

### Initial Promts:
Initial starting promts where:
For detailed milestone prompts and architecture, see [InitialPrompts.md](./InitialPrompts.md).



### Further improvements:
- more customisability for users (mabey add theme file):
  - changeable color of background
  - changeable color of clock
  - change transparency of clock, background, & all other elements combined seperatly
- add the compact mode
- improve action buttons with icons

- add working system tray icon and make it clearly reflect state play/pause
* Optional tiny **“Today: 3h 12m”** in tray tooltip so you get instant feedback without opening anything.

- 
* **Pomodoro mode**: optional mode with work/break cycles and auto-advance.
* **Sound cues**: soft chime on break time or when resuming after long pause which can be disabled in settings.

* Persist **window position** per monitor:
  * On move/resize, store position in Settings.
  * On startup, restore to that position (and validate it’s still on a visible screen).

* Improve the log schema a bit
    Right now: `Date, Start, End, Duration, App, WindowTitle, Project`.
    consider adding:
    * `IdleFlag` (bool) – if we later auto-pause or detect idle periods.
    * `SessionId` – so you can reconstruct which segments belong to the same “intentional session”.
    * `SourcePlatform` – “Windows” vs “Linux” if you’ll run it on both.

* Lightweight in-app “Today” view
    Without building a full analytics dashboard, add a simple **“Today” panel**:
    * Group today’s CSV by:
      * App
      * Project (if set)
    * Show:
      * “Today total: 5h 40m”
      * Top 5 apps/projects with durations.
    Even a simple `ListBox` in a “Stats” tab in Settings would already make it feel much more useful.

* Idle detection & auto-pause
    * Use OS APIs to detect idle (e.g., `GetLastInputInfo` on Windows).
    * If idle > X minutes (configurable):
        * Auto-pause the timer, and optionally pop a small notification:
        “You’ve been idle for 8 min, timer paused.”
    * On user returning, show:
        * “Resume timer?” as a small dialog or notification.

* Rules-based project tagging (later)
    Add a simple rules engine:
    * Rule examples:
      * If `WindowTitle` contains “HSH-xxxx” → project “HSH-xxxx”.
    * Store as a list of rules (order matters).
    * Apply on each new segment.

* Centralized logging / diagnostics
  * Introduce a simple `ILogger` abstraction or just use `Microsoft.Extensions.Logging`.
    * Log:
      * Exceptions from platform services.
      * CSV write failures.
      * Registry write issues for autostart.
    * Provide an “Open logs folder” action so you can inspect issues later.

* Clear versioning & changelog
  * Embed version into the app (e.g., “FocusTimer v0.3.0” in Settings/About).
  * Keep a small `CHANGELOG.md` – super helpful when you start iterating on features.


#### suggested improvements:
Make everything testable
Clean cross-platform boundaries

If you want to further improve stability, you can:
•	Add more try/catch blocks around file I/O in JsonSettingsProvider.
•	Add user notifications for errors (future UX).
•	Validate settings before saving (e.g., check if log directory is writable).