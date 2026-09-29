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
| OI-14 | Activity tracking | Foreground-window polling and segmentation cannot be configured. | Partial | `configure-activity-polling` implements Developer Options polling of 1–60 seconds, default 10, with live application and safe capture scheduling. Segmentation rules remain open; the main specs are synchronized and the change awaits archive. |
| OI-15 | Worklog management | Logged entries cannot be edited or deleted. | Open | Requires stable IDs and safe file-update semantics. |
| OI-16 | Distribution | Installer size and release variants. | Implemented; install validation open | Clean build: self-contained MSI 36.04 MiB, direct framework-dependent MSI 12.16 MiB, .NET-aware setup 13.10 MiB, compressed portable EXE 46.34 MiB. The setup checks for .NET 8 and downloads the verified runtime only when missing. Trimming currently fails on ThemeService JSON serialization warnings (IL2026); verify install, upgrade, runtime download, and same-version variant switching before release. |
| OI-17 | Performance | Broader resource costs need measurement and optimization. | Partial | `configure-activity-polling` implements a bounded Windows process-lifetime cache and configurable sampling. [Release evidence](ActivityPollingPerformance.md) confirms component lookup reduction; whole-app savings are unmeasured. Settings reads and worklog writes remain separate investigation areas. |
| OI-18 | Test infrastructure | Persistence tests write to the real AppData settings path and fail in restricted environments. | Closed | Implemented settings file path injection in JsonSettingsProvider; tests execute in isolated temporary directories. |
| OI-19 | Quality reporting | SonarQube does not receive the locally generated coverage artifacts. | Closed | Switched CI to windows-latest runner, enabled OpenCover report output across all test projects, and connected coverage ingestion in sonar-scan workflow. |
| OI-20 | Data evolution | There is no agreed general strategy for migrating persistent worklog formats between releases. | Investigate | Research installer versus first-run migration, portable upgrades, backups, idempotent recovery, rollback, and eventual removal of legacy migration code. This is intentionally outside the development-only worklog-foundation change. |
| OI-21 | Settings / theming | Settings controls and theme colors are inconsistent, and save/cancel behavior varies by setting. | Open | [Redesign task](OI-21-SettingsRedesign.md) covers visual states, theme alignment across Settings/widget/menu, shared Save/Apply behavior, optional immediate preview, and reliable Cancel restoration. |
