# FocusTimer - Next-Version Open Issues

> This is the working backlog for the next version. Items are derived from `Planning.md` and have not yet been committed to a release scope.

| ID | Area | Open issue | Status | Notes |
|---|---|---|---|---|
| OI-01 | Global Hotkeys | Hotkeys cannot be edited in Settings. | Open | Registration and dispatch work; only capture, validation, and saving are missing. |
| OI-02 | System Tray | The tray menu has no quick actions such as project switching. | Open | Define the smallest useful action set before implementation. |
| OI-03 | Widget UI | Window position and size are not persisted. | Open | No window-bounds settings exist; restoration must protect against off-screen placement. |
| OI-04 | Settings / Reporting | There is no breakdown or user-facing report by app or project. | Open | Today statistics currently provide only a total. |
| OI-05 | Diagnostics | Settings has no action to open the logs folder. | Open | Logging itself is already available. |
| OI-06 | Cross-platform | Linux implementations remain incomplete. | Open | Linux parity needs a platform target and real implementations for platform services. |
| OI-07 | Data features | Versioned worklog foundation with durable identity and provenance. | Completed | Delivered by F-01: `TimeEntry` has durable entry/session IDs, capture/activity/platform metadata, revision, and safe CSV storage. |
| OI-08 | Data features | Project tagging is manual only. | Open | Rules-based application/window pattern mapping is proposed. |
| OI-09 | Persistence | CSV is the only storage backend. | Open - investigate | Evaluate actual reporting and editing needs before committing to a storage migration. |
| OI-10 | Focus modes | Pomodoro work/break automation is absent. | Open | Separate product decision from existing break reminders. |
| OI-11 | Focus modes | Break and resume events have no optional sound cues. | Open | Needs an accessibility-conscious enable/disable setting. |
| OI-12 | Reporting | Worklog exports and reusable export presets are absent. | Open | Existing export functionality applies only to themes. |
| OI-13 | Widget UI | Minimise behavior is confusing and easy to miss. | Needs validation | Confirm observed behavior and choose whether minimising should hide to the tray. |
| OI-14 | Activity tracking | Foreground-window polling and segmentation cannot be configured. | Proposed | Current behavior is a fixed one-second poll while the timer runs. |
| OI-15 | Worklog management | Logged entries cannot be edited or deleted. | Open | Requires stable IDs and safe file-update semantics. |
| OI-16 | Distribution | Installer size has not been assessed or optimised. | Investigate | Establish a measured size target before changing packaging. |
| OI-17 | Performance | No evidence-based performance improvement plan exists. | Investigate | Profile the app first and record concrete bottlenecks. |
| OI-18 | Test infrastructure | Persistence tests write to the real AppData settings path and fail in restricted environments. | Closed | Implemented settings file path injection in JsonSettingsProvider; tests execute in isolated temporary directories. |
| OI-19 | Quality reporting | SonarQube does not receive the locally generated coverage artifacts. | Closed | Switched CI to windows-latest runner, enabled OpenCover report output across all test projects, and connected coverage ingestion in sonar-scan workflow. |
| OI-20 | Data evolution | There is no agreed general strategy for migrating persistent worklog formats between releases. | Investigate | Research installer versus first-run migration, portable upgrades, backups, idempotent recovery, rollback, and eventual removal of legacy migration code. This is intentionally outside the development-only worklog-foundation change. |
