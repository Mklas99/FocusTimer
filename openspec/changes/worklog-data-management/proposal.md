## Why

Users can record time automatically but cannot look at individual entries, fix a wrong one, remove one, or add time they forgot to track (OI-31, OI-15, OI-22). The only reporting surface is a Summary tab buried in Settings, which is a configuration window, not a place to review work. F-01 already provides revision-checked update, delete, and append in the worklog store, so the missing piece is the user-facing layer on top of it.

## What Changes

- Add a **Worklog window**, opened from a new "Worklog..." tray menu entry placed directly above "Settings...", in the same way the Settings window opens (one instance, shown and activated on repeat clicks).
- The window has an **Entries** tab (read-only table of one day's stored entries with visible read warnings), a **Timeline** tab (the same day in chronological order), and a **Summary** tab. The Summary tab moves out of Settings into this window.
- One **day selector** (default Today, previous/next/Today controls) drives all three tabs. Only single days are supported for now.
- Users can **add a manual entry** (date, start time, duration, optional window text, optional project). Manual entries use the fixed application label "Manual entry" and a new `Manual` capture source.
- Users can **edit** an entry's window title, project, and duration, and **delete** an entry after confirmation. The application is fixed; start and end are not edited directly (a new duration moves the end; the start stays). An edit never moves an entry to another day.
- An add or edit that overlaps another entry on that day is saved and shows a **dismissible warning**, never a block.
- Summary gains a third grouping, **by window**, next to by application and by project.
- Editing and deleting use the store's revision check. A stale revision or a file in use is reported with a clear message and a reload, not a silent overwrite.
- Settings loses its Summary tab.

Out of scope: editing start or end times directly, editing the application, multi-day or range views, export (OI-12), rule-based project detection (OI-08), the report window filters from OI-04 beyond day selection, and Linux-specific work (no platform code is touched).

## Capabilities

### New Capabilities
- `worklog-window`: The tray-opened window, its day selection, its Entries and Timeline tabs, its hosting of the Summary tab, and refresh behavior.
- `worklog-entry-management`: Adding manual entries and editing or deleting existing ones, including field rules, overlap warnings, and conflict handling.

### Modified Capabilities
- `worklog-storage`: Adds the `Manual` capture source so manual entries round-trip through storage.
- `worklog-summary`: Adds the by-window grouping and day-based range selection. This capability is introduced by the not yet archived `worklog-summary-breakdown` change, which must be archived first.
- `settings`: The Settings window no longer has a Summary tab.
- `system-tray`: The tray menu gains a Worklog entry above Settings.

## Impact

- `FocusTimer.Core`: new `CaptureSource.Manual`, an editing service with overlap detection, a by-window grouping. No platform code, so nothing is Windows-only and no Linux stub is needed.
- `FocusTimer.Persistence`: CSV codec maps the new capture source; no storage format version change expected (verify during implementation).
- `FocusTimer.App`: new Worklog window, view models and views (entries, timeline, add/edit dialog); `AppController.ShowWorklog`; tray menu item in `App.axaml.cs`; Summary view model re-hosted; `SettingsWindow` and its view model lose the Summary tab.
- `FocusTimer.Host`: DI registration.
- Tests: Core (service, grouping, overlap), Persistence (manual round trip, edit/delete conflicts), App (view models, refresh behavior), headless view test for the window.
- Docs: `Features.md` (new F-03 row), `OpenIssues.md` (OI-15, OI-22, OI-23, OI-31 updates; OI-33 and the previous-day part of OI-04 delivered by the shared day selector), `ARCHITECTURE.md`, README if it describes the tray menu or Summary tab.
- Related issues: OI-15, OI-22, OI-23, OI-31, OI-33, OI-04 (partial).
