# Changelog

## v0.2.0 — 2026-10-18

Major feature release introducing the Worklog Management window, smarter automatic activity tracking, dense-frost design system, widget placement and frosted glass options, and notification preferences.

### Worklog Management Window
- Tray-opened Worklog window with three dedicated tabs: Entries, Timeline, and Summary, driven by a unified day selector.
- **Entries Table**: Inspect raw daily activity records to the minute, search across any column with highlighted matches, and view full provenance metadata (entry ID, session, revision, end reason).
- **Manual Time Entry**: Add missed time intervals with automatic day bounds validation and flexible duration parsing (`2h 30m`, `2.5h`).
- **Editing & Deletion**: Edit window titles, assigned projects, and durations directly in the Entries view; delete entries with revision-checked collision safety.
- **Timeline View**: Visual chronological timeline with zoom (Ctrl + mouse wheel / buttons) and dynamic column grouping (by application, project, or window).
- **Summary Breakdown**: Aggregated daily time breakdowns by application, project, or window with percentage shares for any day in the retention window.

### Smarter Automatic Tracking
- **Rule-Based Projects**: Dynamically assign tracked activity to projects at read time using ordered wildcard pattern rules without rewriting stored logs.
- **Application Exclusions**: Configure applications excluded from capture so sensitive or non-work apps are not recorded in worklogs.
- **Segmentation Rules**: Suppress noisy title-change segment splits for applications like browsers while maintaining accurate process tracking.
- **Configurable Polling**: Adjust foreground-window sampling (1–60s) in Developer Options with bounded process-handle caching for lower overhead.

### UI Design System & Theming
- **Dense Frost Design**: Redesigned Settings and Worklog windows with Segoe UI typography, soft-rounded controls, compact peer cards, and icon-and-underline navigation.
- **Desktop Action Hierarchy**: Unified button styling across dialogs (accent primary for OK/Apply/Save, neutral bordered for Cancel, muted danger for Delete).
- **Theme Contrast Harmony**: Verified text and indicator contrast (WCAG 4.5:1 / 3:1) across all 7 built-in themes (Dark, Light, Monokai, Solarized Dark, Nord, Dracula, High Contrast).
- **Imported Theme Preservation**: Preserved custom and imported theme snapshots with a dedicated dropdown entry surviving restarts and edits.
- **Predictable Settings Edits**: Apply and OK commit the whole Settings draft, Cancel and closing the window restore the last saved state, appearance changes preview live, and a failed save shows its error and can be retried.

### Widget Layout & Backdrop
- **Control Placement**: Choose button layout beside the clock or stacked horizontally below the clock in both Full and Compact modes.
- **Independent Shell Tint**: Separated background shell tint slider from whole-window opacity multiplier.
- **Qualified Windows Frosted Blur**: Native frosted glass acrylic blur on Windows with rounded corner clipping, whole-widget fade, and graceful solid fallback.
- **Balanced Spacing**: Streamlined widget geometry, equal vertical content padding, and removed widget minimize button in favor of tray Show/Hide.

### Notifications & Preferences
- **Dismissal Timeouts**: Configure notification visibility by severity and customize auto-dismissal timeouts from 1 to 120 seconds.
- **Refined Notification Chrome**: Shared dialog styling with status icons, Danger/Warning outlines, and persistent break acknowledgement reminders.

### Developer Mode & Diagnostics
- **Activation Interaction**: Toggle Developer mode on/off via 7 clicks on the About tab version label.
- **Developer Settings**: Granular controls for activity polling frequency, exclusion rules, and segmentation rules.

### Quality, Architecture & Tooling
- Single-source versioning governed by the repository `VERSION` file.
- Multi-flavor installer packaging: self-contained MSI, .NET 8 bootstrapper setup, and portable executable. The setup shows one progress window and installs the changelog with the app.
- Complete test isolation from user AppData directories.
- SonarQube CI integration with automated OpenCover report ingestion.

### Upgrade Notes
- Worklog files now use a versioned CSV format with durable entry IDs. Files written by v0.1 are not read or converted, and no new entries are saved to a day whose file still has the old format. Move or delete the old `worklogs` folder (default `Documents\FocusTimer\worklogs`) before the first start.

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
