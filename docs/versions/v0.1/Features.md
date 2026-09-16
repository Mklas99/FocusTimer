# FocusTimer — v0.1 Baseline: Features & Use Cases

Snapshot of what is actually implemented in the code as of this baseline, verified against source (not just against the planning docs). This is the consolidation point for further work.

- Companion file: [OpenIssues.md](OpenIssues.md) — gaps, partial items, and known limitations referenced by `OI-##` below.
- Original design/planning history (milestone prompts, epics, reviews, fix reports): [archived/](../../archived/).
- Solution layout: `FocusTimer.Host` (composition root/entry point) → `FocusTimer.App` (Avalonia UI) → `FocusTimer.Core` (domain logic/interfaces) → `FocusTimer.Persistence` (CSV/JSON storage) → `FocusTimer.Platform.Windows` (Win32 integrations); Linux stubs live in `FocusTimer.Core/Stubs`.

| Area | Feature | Use case | Status |
|---|---|---|---|
| Timer Core | Start / Pause / Reset | User clicks play/pause/reset on the widget; elapsed time counts up (HH:MM:SS), state is Idle/Running/Paused | Done |
| Timer Core | Optional project/task tag | User types a free-text tag before/while running; tag is attached to logged entries | Done |
| Activity Tracking | Per-application time segmentation | While running, foreground app/window is polled every second; a new log segment starts whenever the active app/window changes | Done |
| Activity Tracking | Work-logging on/off switch | Setting toggle disables tracking, in-flight/buffered entries, and stops growing "today" stats when off | Done |
| Activity Tracking | CSV persistence | Entries are appended to `worklogs/yyyy/MM/yyyy-MM-dd-worklog.csv`, flushed on pause/stop and periodically while running | Done |
| Activity Tracking | Data retention cleanup | Worklog files older than `DataRetentionDays` (default 90) are deleted once per day | Done |
| Break Reminders | Interval-based reminder | After `BreakIntervalMinutes` (default 50) of running time, a notification fires | Done |
| Break Reminders | Optional acknowledgement requirement | Setting toggle keeps the reminder up until the user acknowledges it, instead of auto-dismissing and re-firing in 10 min | Done |
| Idle Detection | Auto-pause on inactivity | Windows idle time (`GetLastInputInfo`, 5 min threshold) auto-pauses the timer and notifies the user | Done |
| Idle Detection | Resume notification | Returning from idle shows a "press play to resume" notification | Done |
| Global Hotkeys | Show/Hide widget, Toggle timer | System-wide hotkeys (default Ctrl+Alt+T / Ctrl+Alt+P) via real `RegisterHotKey` + subclassed `WndProc`, work even when unfocused | Done |
| Global Hotkeys | Hotkey editing UI | — | Partial (`OI-01`) |
| System Tray | State-aware icon + tooltip | Tray icon swaps Running/Paused/Idle art; tooltip shows state + "Today: Xh Ym" | Done |
| System Tray | Menu: Show/Hide, Start/Pause, Settings, Exit | Left-click menu drives the same actions as the widget/hotkeys | Done |
| System Tray | Quick project-switch from tray | — | Not implemented (`OI-02`) |
| Widget UI | Full mode | Draggable, always-on-top window: timer, play/pause/reset, project tag field, settings/compact-toggle icon buttons (Material icons) | Done |
| Widget UI | Compact mode | Narrow bar view, same data/theme, toggled from widget or Settings, persisted across restarts | Done |
| Widget UI | Scale & opacity | `WidgetScale` resizes fonts/buttons responsively; independent background/clock/controls opacity plus an overall multiplier | Done |
| Widget UI | Window position/size memory | — | Not implemented (`OI-03`) |
| Theming | 7 built-in themes | Dark, Light, Monokai, Solarized Dark, Nord, Dracula, High Contrast — selectable in Settings, applied live | Done |
| Theming | Custom theme import/export | `.fttheme` JSON files, loaded/saved via file picker, validated on import | Done |
| Theming | Reset to default | One click restores the Dark theme | Done |
| Settings | Tabs: General, Logging, Appearance, Hotkeys, About | Auto-start, start-minimized, always-on-top, break reminders, worklog directory + retention, theme/opacity, hotkey display, version/changelog/repo link | Done |
| Settings | Today/Stats breakdown tab | — | Not implemented (`OI-04`) |
| Settings | Hidden developer mode | Clicking the version label 7× unlocks a Developer section with a log-level picker; persisted via `DeveloperModeEnabled` | Done |
| Auto-Start | Windows registry auto-start on login | Toggle in Settings writes/removes `HKCU\...\Run` entry | Done |
| Notifications | In-app toast-style notifications | Used for break reminders and idle pause/resume messages (not native Windows toasts) | Done |
| Diagnostics | Serilog file + console logging | Rolling daily logs (30-day retention) under the app's log directory; `FOCUSTIMER_LOG_DIR` env override | Done |
| Diagnostics | "Open logs folder" button | — | Not implemented (`OI-05`) |
| Versioning | About tab: version, author, repo link, changelog | Reads `docs/CHANGELOG.md`, shows assembly informational version | Done |
| Cross-Platform | Linux stub services | App builds/runs on Linux but active-window, notification, hotkey, auto-start, idle-detection features are no-ops | Partial (`OI-06`) |
| Dev/Build Infra | CI, SonarQube, linting, WiX installer, architecture doc | GitHub Actions sonar-scan workflow, `.sonarqube/`, `stylecop.json`, `installer/` MSI project, `ARCHITECTURE.md` | Done |
| Dev/Build Infra | Automated tests | 5 test projects (Core/App/Persistence/Platform.Windows/Host) covering timer, session tracking, event bus, break reminders, theming, CSV/JSON persistence | Done |
| Data Features | Extended log schema (idle flag, session id, source platform) | — | Not implemented (`OI-07`) |
| Data Features | Rules-based project tagging | — | Not implemented (`OI-08`) |
| Data Features | SQLite storage option | — | Not implemented (`OI-09`) |
| Focus Modes | Pomodoro mode | — | Not implemented (`OI-10`) |
| Focus Modes | Sound cues | — | Not implemented (`OI-11`) |
| Reporting | Timesheet export presets | — | Not implemented (`OI-12`) |
