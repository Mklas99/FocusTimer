# FocusTimer — v0.2.0 Baseline: Open Issues

Gaps, completed milestones, and remaining backlog items as of the v0.2.0 release baseline.

- Companion file: [Features.md](Features.md) — features delivered in v0.2.0.
- Development summary: [Summary.md](Summary.md).
- Prior baseline: [v0.1/OpenIssues.md](../v0.1/OpenIssues.md).

## Open Issues summary (OI-01 to OI-45)

| ID | Area | Open issue / Scope | Status in v0.2.0 | Resolution / Notes |
|---|---|---|---|---|
| OI-01 | Global Hotkeys | Hotkeys cannot be edited in Settings | Open | Registration and dispatch work; key capture and save UI remain open for v0.3+. |
| OI-02 | System Tray | Tray menu has no quick actions (e.g. project switching) | Open | Tray menu is currently fixed: Show/Hide, Start/Pause, Worklog, Settings, Exit. |
| OI-03 | Widget UI | Window position and size are not persisted | Open | Needs screen-bounds safety check to avoid off-screen placement on multi-monitor setups. |
| OI-04 | Reporting | Breakdown or user-facing report by app or project | Partially delivered | Delivered for single days by F-02 and F-03 (Summary and Timeline tabs in Worklog window). Multi-day ranges and filter presets remain open. |
| OI-05 | Diagnostics | Settings has no action to open the logs folder | Open | Logging is fully functional; shortcut button in Settings UI remains open. |
| OI-06 | Cross-platform | Linux implementations remain incomplete | Open | Stubs exist in `FocusTimer.Core/Stubs`; platform parity requires Linux-specific implementations. |
| OI-07 | Data features | Versioned worklog foundation with durable identity and provenance | Completed | Delivered by F-01: schema version 1 CSV, durable entry/session GUIDs, UTC timestamps, revision checks. |
| OI-08 | Data features | Project tagging is manual only (Rule-based project tagging) | Completed in code (manual walkthrough open) | Delivered by F-04: read-time rule matching (`RuleProjectResolver`) via wildcard patterns; manual Windows walkthrough check open. |
| OI-09 | Persistence | CSV is the only storage backend | Open - investigate | Evaluate actual reporting and performance needs before committing to a SQLite or database backend. |
| OI-10 | Focus modes | Pomodoro work/break automation is absent | Open | Separate product decision from existing break reminders. |
| OI-11 | Focus modes | Break and resume events have no optional sound cues | Open | Needs an accessibility-conscious enable/disable toggle. |
| OI-12 | Reporting | Worklog exports and reusable export presets are absent | Open | Export currently applies only to custom themes. |
| OI-13 | Widget UI | Minimize behavior is confusing and easy to miss | Completed | Delivered by M03: removed widget minimize button; balanced spacing; Show/Hide tray action. |
| OI-14 | Activity tracking | Foreground polling and segmentation cannot be configured | Completed in code (manual walkthrough open) | Delivered by F-04: configurable polling (1–60s) with process caching, plus segmentation rules; manual Windows check open. |
| OI-15 | Worklog management | Logged entries cannot be edited or deleted | Completed | Delivered by F-03: Worklog Entries tab edits title, project, duration, and deletes with revision conflict checks. |
| OI-16 | Distribution | Installer size and release variants | Completed in code (install validation open) | Self-contained MSI (36 MB), .NET bootstrapper setup (13 MB), portable EXE (46 MB); runtime download validation open. |
| OI-17 | Performance | Broader resource costs need measurement and optimization | Partial | Process lookup caching in polling delivered; whole-app benchmarking open. |
| OI-18 | Test infrastructure | Persistence tests write to real AppData path | Completed | Delivered by M02: settings path injection in `JsonSettingsProvider`; isolated test directories. |
| OI-19 | Quality reporting | SonarQube does not receive locally generated coverage artifacts | Completed | Delivered by M02: `windows-latest` CI runner, OpenCover coverage ingestion in sonar workflow. |
| OI-20 | Data evolution | Strategy for migrating persistent worklog formats | Investigate | Open investigation for future releases beyond schema 1. |
| OI-21 | Settings / theming | Settings controls and theme colors inconsistent, save/cancel varies | Completed | Delivered by M03 and F-05: full draft commit/cancel/recovery model, immediate appearance preview, dense frost layout. |
| OI-22 | Worklog management | Users cannot add time they forgot to track | Completed | Delivered by F-03: manual entry creation in Entries tab with duration parsing (`2h 30m`, `2.5h`) and day bounds. |
| OI-23 | Reporting | Users cannot see recorded work as a chronological timeline | Partially delivered | Delivered for single days by F-03 (Timeline tab with zoom and column grouping); multi-day timeline open. |
| OI-24 | UI / theming | Settings controls look dated and theme colors reduce readability | Completed | Delivered by M03 and F-05: dense-frost design, verified 4.5:1 / 3:1 theme contrast, rounded dialog notifications. |
| OI-25 | Widget UI / theming | Background opacity control no longer makes widget visibly transparent | Completed | Delivered by M03 and F-06: independent shell tint opacity slider separate from whole-window opacity multiplier. |
| OI-26 | Developer workflow | VS Code F5 launch configuration alignment | Open | Align local launch configs with `dotnet run --project src/FocusTimer.Host --runtime win-x64`. |
| OI-27 | Settings persistence | Denied write during legacy settings identity migration | Open | Gracefully handle denied write with process-stable fallback ID. |
| OI-28 | Widget UI / theming | Platform blur cannot guarantee see-through frosted background | Completed | Delivered by F-06: native Windows acrylic blur with rounded clipping, whole-widget fade, and solid fallback. |
| OI-29 | Settings / theming | Imported themes have no selectable entry in preset dropdown | Completed | Delivered by F-05: custom/imported dropdown entry preserves snapshot across restarts and edits. |
| OI-30 | Developer diagnostics | Live application logs cannot be read inside Settings | Open | Candidate for Developer mode in v0.3+. |
| OI-31 | Worklog inspection | Users cannot inspect raw worklog entries in a table | Completed | Delivered by F-03: raw entries table with minute precision, details pane, and search highlighting. |
| OI-32 | Developer tracking | Applications cannot be excluded from worklog recording | Completed in code (manual walkthrough open) | Delivered by F-04: exclusion rule list in Developer Options; manual Windows check open. |
| OI-33 | Summary reporting | Summary table cannot display previous days | Completed | Delivered by F-03: unified DaySelector driving Entries, Timeline, and Summary tabs within retention. |
| OI-34 | Error handling | Inconsistent error handling and user-facing feedback policy | Open - investigate | Define standard app-wide error propagation, toast vs dialog rules, and recovery actions. |
| OI-35 | Widget layout | Choose widget controls beside or below the clock | Completed | Delivered by F-06: Appearance preview, persists placement in Full and Compact modes. |
| OI-36 | Developer tracking | Idle detection behavior cannot be configured | Open | Configurable idle thresholds and automatic resume options. |
| OI-37 | Developer mode | Toggle Developer mode through activation interaction | Completed | Delivered by F-06: 7 clicks on About version label toggles Developer mode. |
| OI-38 | Worklog inspection | Timeline entries cannot be bucketed into larger time groups | Open | Time-bucketing intervals for timeline inspection. |
| OI-39 | Worklog management | Choosing a project in the project field is slow and error-prone | Open | Searchable/autocomplete project dropdown improvements. |
| OI-40 | Installer experience | Installing development build opens two installer progress windows | Completed | Suppressed chained MSI internal UI in Bootstrapper Bundle.wxs; setup runs cleanly with a single progress window. |
| OI-41 | Notifications | Configure notification visibility and dismissal times | Completed | Delivered by F-06: visibility toggles by severity and 1–120s dismissal duration. |
| OI-42 | UI / buttons | Buttons for different actions have the same visual weight | Completed | Delivered by F-05: distinct action roles (primary accent, secondary bordered, destructive danger). |
| OI-43 | Developer diagnostics | Developer Settings cannot trigger app actions for testing | Open | Test notification triggers in Developer Settings. |
| OI-44 | Settings / preview | Nonappearance settings cannot be previewed before Apply/OK | Open - design | Evaluate safe and reversible live preview for nonappearance settings. |
| OI-45 | Widget performance | Live widget scaling preview is less smooth with Frosted enabled | Open - deferred | Improve live scaling smoothness under frosted blur backdrop. |
