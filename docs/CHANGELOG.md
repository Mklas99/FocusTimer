# Changelog

## v0.2.0 — 2026-10-18

Worklog window, smarter tracking, a redesigned look, and more widget options.

### Worklog
- Tray-opened Worklog window with Entries, Timeline, and Summary tabs, driven by one day selector.
- Entries: search, full details per entry, manual entries (`2h 30m` or `2.5h`), edit and delete.
- Timeline: zoom, and grouping by application, project, or window.
- Summary: time per application, project, or window with shares, for any day in the retention window.

### Tracking
- Project rules assign activity to projects when it is read, without rewriting stored logs.
- Exclusion rules keep chosen applications out of the worklog; segmentation rules stop title changes from splitting entries.
- Developer Options set the polling interval (1–60 s).

### Look and widget
- Redesigned Settings and Worklog windows, consistent action buttons, readable contrast in all 7 themes, and imported themes that survive restarts.
- Settings: Apply and OK commit the whole draft, Cancel restores the last saved state, appearance previews live, and a failed save shows its error.
- Widget buttons beside or below the clock, a separate tint slider, native frosted blur on Windows with a solid fallback. The Minimize button is gone; use Show/Hide in the tray.
- Notifications: choose which severities appear and for how long (1–120 s).
- Developer mode toggles with seven clicks on the About version label.

### Under the hood
- One `VERSION` file; self-contained MSI, setup EXE that checks for .NET 8, and portable EXE; the setup shows one progress window and installs the changelog.
- Tests no longer write to the real AppData folder; SonarQube coverage runs in CI.

### Upgrade Notes
- Worklogs now use a versioned CSV format. Files written by v0.1 are not read or converted, and no new entries are saved to a day whose file still has the old format. Move or delete the old `worklogs` folder (default `Documents\FocusTimer\worklogs`) before the first start.

## v0.1.0 — 2026-09-16

Initial "release" version.

- Compact timer widget — start/pause/reset, draggable, always-on-top, with full and compact display modes
- Automatic time tracking per application, saved to daily CSV logs, with configurable auto-cleanup of old logs
- System tray icon that reflects timer state, with quick Show/Hide, Start/Pause, Settings, and Exit actions
- Global hotkeys to show/hide the widget and start/pause the timer from anywhere
- Break reminders after a configurable work interval, with optional "must acknowledge" mode
- Idle detection that auto-pauses the timer when you step away and reminds you to resume
- 7 built-in themes, plus import/export of custom themes
- Adjustable widget scale and opacity
- Auto-start on Windows login
- Settings covering general behavior, logging, appearance, hotkeys, and app info, plus a hidden developer mode
