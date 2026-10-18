# FocusTimer - Next-Version Features

> Larger outcomes for the next version, grouped from the open issues. Candidate scope and priority may change before implementation; the user decides the order.

`OpenIssues.md` is the detailed backlog and source of truth. Delivered features (F-01 to F-06, M01 to M04) are snapshotted in [v0.2/Features.md](../v0.2/Features.md); earlier ones in [v0.1/Features.md](../v0.1/Features.md). A candidate receives its `F-##` or `M-##` ID when work on it starts.

## Feature candidates

| Feature / improvement collection | Outcome | Related issues | Status |
|---|---|---|---|
| Everyday desktop control | The widget restores its position safely, offers configurable hotkeys, exposes useful tray actions, and makes logs easy to find. | OI-01, OI-02, OI-03, OI-05 | Candidate |
| Better worklog management and reporting | Users can review ranges beyond one day, bucket and filter the timeline, pick projects quickly, and export tracked work. | OI-04, OI-09, OI-12, OI-23, OI-38, OI-39 | Partly delivered (single-day summary, entries and timeline); the rest is open |
| Smarter automatic tracking | Projects are inferred from activity and advanced users tune sampling, segmentation, and application exclusions. | OI-08, OI-14, OI-32 | Implemented and archived; manual Windows walkthroughs remain open in [OpenTasks.md](../../OpenTasks.md) |
| Idle detection controls | Developer Settings configure idle thresholds and optional automatic resume when activity returns, while preserving manual pauses. | OI-36 | Candidate |
| Focus modes | Pomodoro cycles and optional sound cues support deliberate work and break routines. | OI-10, OI-11 | Candidate |
| Diagnostics and user feedback | Developer mode shows live application logs in Settings and can trigger test notifications; a consistent error and warning policy makes failures and recovery understandable. | OI-05, OI-30, OI-34, OI-43 | Candidate; the error-handling policy needs exploration |
| Settings preview | Eligible nonappearance settings can be previewed before Apply/OK, with clear rules for what is reversible. | OI-44 | Design needed |
| Widget frost polish | Live scale changes stay smooth with Frosted enabled. | OI-45 | Deferred; accepted for now |
| Product reliability and delivery | Installer choices are validated on clean machines, performance work is measured, and F5 launches the documented build. | OI-16, OI-17, OI-26 | Partly delivered; install validation and whole-app measurements open |
| Data evolution and compatibility | Compatibility-sensitive releases have a backup, migration, rollback, and legacy-retirement strategy, and settings migration cannot lose loaded preferences. | OI-20, OI-27 | Investigation |
| Linux feature parity | Linux receives real implementations for the platform integrations that currently use stubs. | OI-06 | Long-term candidate |

## Feature branch and release-note convention

Each actively implemented or completed feature receives a stable `F-##` identifier when work on it starts and keeps it in the version snapshot. Use that ID in its branch name as `feature/<F-##>_<short-kebab-case-description>`; for example, `feature/F-01_worklog-data-foundation`. Keep the ID with the feature when it is completed so release notes can refer to the same stable identifier.

Foundation and maintenance work that is not a user-facing feature uses a stable `M##` identifier and a `maintenance/<M##>_<short-kebab-case-description>` branch, for example `maintenance/M01_establish-system-design-foundation`.
