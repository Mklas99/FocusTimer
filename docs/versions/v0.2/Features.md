# FocusTimer — v0.2.0 Baseline: Features & Use Cases

Snapshot of features and capabilities implemented in FocusTimer as of the v0.2.0 baseline, verified against source and archived OpenSpec changes.

- Companion file: [OpenIssues.md](OpenIssues.md) — gaps, partial items, and backlog referenced by `OI-##`.
- Development summary: [Summary.md](Summary.md).
- Prior baseline: [v0.1/Features.md](../v0.1/Features.md).

## Features implemented as of v0.2.0

| Area | Feature | Description / Use Case | Status |
|---|---|---|---|
| **Timer Core** | Start / Pause / Reset | Elapsed time counting (HH:MM:SS), state changes (Idle, Running, Paused), session tracking | Done |
| **Timer Core** | Manual project tag | Free-text tag entered on the widget, attached to active session entries | Done |
| **Worklog Storage** | Schema Version 1 CSV | Durable entry and session GUIDs, offset-aware timestamps, derived duration, UTC modification metadata, RFC 4180 streaming format | Done (F-01) |
| **Worklog Storage** | Retention cleanup | Worklog files older than `DataRetentionDays` (default 90) automatically deleted | Done |
| **Worklog Management** | Worklog window | Dedicated window opened from tray menu with Entries, Timeline, and Summary tabs driven by a unified day selector | Done (F-03) |
| **Worklog Management** | Raw entries table | Daily table with minute precision, full provenance details pane, and case-insensitive column search with highlighted matches | Done (F-03) |
| **Worklog Management** | Manual time logging | Add missed time intervals with start time, duration parsing (`2h 30m`, `2.5h`), day-cap, and manual source flag | Done (F-03) |
| **Worklog Management** | Entry editing & deletion | Edit window title, project, and duration inline; delete entries with revision collision checks | Done (F-03) |
| **Worklog Reporting** | Timeline view | Visual chronological day axis with dynamic zoom (Ctrl + mouse wheel) and column grouping (all, app, project, window) | Done (F-03) |
| **Worklog Reporting** | Summary breakdown | Aggregate time breakdown by application, project, or window with percentage shares and read warnings | Done (F-02, F-03) |
| **Automatic Tracking** | Foreground polling | Configurable sampling interval (1–60s, default 10s) with cached Windows process handles to reduce overhead | Done (F-04) |
| **Automatic Tracking** | Rule-based project mapping | Dynamic read-time assignment of projects via wildcard patterns without modifying stored logs | Done (F-04) |
| **Automatic Tracking** | Application exclusions | Exclude specified applications from being captured in logs | Done (F-04) |
| **Automatic Tracking** | Segmentation rules | Prevent window title changes from fragmenting log entries for configured applications (e.g. browsers) | Done (F-04) |
| **Widget UI** | Full & Compact modes | Full mode with timer, controls, tag field; narrow Compact bar with density-adjusted controls | Done |
| **Widget UI** | Button placement | Choose button placement beside the clock or stacked horizontally below the clock in both modes | Done (F-06) |
| **Widget UI** | Scale & Opacity | `WidgetScale` responsive font/button resizing; independent shell tint slider and whole-window opacity multiplier | Done (M03) |
| **Widget UI** | Frosted blur backdrop | Qualified Windows acrylic frosted glass with rounded clipping, whole-widget fade, and solid fallback | Done (F-06) |
| **Widget UI** | Balanced spacing | Equal top and bottom content padding; widget minimize button removed in favor of tray Show/Hide | Done (M03) |
| **UI Design System** | Dense frost design system | Compact-row/peer-card layout, Segoe UI typography, and icon-and-underline tabs across Settings and Worklog | Done (F-05) |
| **UI Design System** | Desktop action hierarchy | Visual differentiation: primary accent fill for OK/Apply/Save, neutral bordered for Cancel, muted danger for Delete | Done (F-05) |
| **Theming** | 7 built-in themes | Dark, Light, Monokai, Solarized Dark, Nord, Dracula, High Contrast — verified for WCAG 4.5:1 / 3:1 contrast | Done (F-05) |
| **Theming** | Custom & imported themes | `.fttheme` import/export; preserved custom dropdown entry and snapshot persistence across restarts and edits | Done (F-05) |
| **Settings Experience** | Full draft transactions | Immediate appearance preview; Apply commits and preserves styling; Cancel/close restores last commit; recovery on failed save | Done (M03, F-05) |
| **Notifications** | Severity & timeouts | Configurable visibility by severity; custom auto-dismissal durations (1–120s); dialog-style rounded chrome | Done (F-06) |
| **Notifications** | Break reminders | Interval-based notifications (default 50 min) with optional sticky acknowledgement requirement | Done |
| **Idle Detection** | Auto-pause on inactivity | Windows `GetLastInputInfo` detection auto-pauses timer after 5 min; resume reminder notification on return | Done |
| **Global Hotkeys** | Show/Hide & Play/Pause | System-wide Windows hotkeys via `RegisterHotKey` (display-only in Settings UI) | Done |
| **System Tray** | State-aware icon & menu | Tray icon reflects state (Idle, Running, Paused); tooltip shows today's total; menu: Show/Hide, Start/Pause, Worklog, Settings, Exit | Done |
| **Developer Mode** | 7-click unlock | Clicking version label 7× in About tab unlocks Developer Options (polling interval, exclusion/segmentation rules) | Done (F-06) |
| **Distribution** | Packaging suite | Self-contained MSI, framework-dependent setup bootstrapper (.NET 8 download), and portable single-file EXE | Done (OI-16) |
| **Infrastructure** | Single-source versioning | Root `VERSION` file drives `Directory.Build.props`, WiX installers, and CI release workflows | Done (M04) |
| **Quality & CI** | Quality tooling | Isolated persistence tests, StyleCop/NetAnalyzers zero-warning enforcement, automated SonarQube OpenCover ingestion | Done (M02) |
