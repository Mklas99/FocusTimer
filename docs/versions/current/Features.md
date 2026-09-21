# FocusTimer - Next-Version Features

> This document groups larger outcomes for the next version. Candidate scope and priority may change before implementation.

`OpenIssues.md` is the detailed backlog and source of truth. This document groups related issues into outcomes instead of repeating their implementation details.

## Active implementation and completed features

| ID | Feature | Outcome | Related issues | Status | Branch / change |
|---|---|---|---|---|---|
| F-01 | Worklog data foundation | Establishes a versioned, safe, storage-neutral worklog foundation with durable entry and running-session identities, schema-safe CSV handling, querying, and same-day mutation support. Unsupported development files require manual move/removal; OI-20 owns any future migration strategy. | OI-07; enables OI-04, OI-08, OI-12, OI-15 | Completed | `feature/F-01_worklog-data-foundation` / `establish-worklog-data-foundation` |

## Feature candidates

| Feature / improvement collection | Outcome | Related issues | Status |
|---|---|---|---|
| Everyday desktop control | The widget restores safely, minimizes predictably, offers configurable hotkeys, exposes useful tray actions, and makes logs easy to find. | OI-01, OI-02, OI-03, OI-05, OI-13 | Candidate |
| Better worklog management and reporting | Users can review, correct, delete, and export tracked work, with useful breakdowns by application and project. | OI-04, OI-07, OI-09, OI-12, OI-15 | Candidate |
| Smarter automatic tracking | Projects can be inferred from activity and advanced users can tune segmentation behavior. | OI-08, OI-14 | Candidate |
| Focus modes | Pomodoro cycles and optional sound cues support deliberate work and break routines. | OI-10, OI-11 | Candidate |
| Linux feature parity | Linux receives real implementations for the platform integrations that currently use stubs. | OI-06 | Long-term candidate |
| Product reliability and delivery | Test isolation, SonarQube coverage reporting, installer-size investigation, and measured performance work make releases more dependable. | OI-16, OI-17, OI-18, OI-19 | Improvement collection |
| Data evolution and compatibility | Future compatibility-sensitive releases have an explicit backup, migration, rollback, and legacy-retirement strategy before persistent formats must be preserved. | OI-20 | Investigation |

## Feature branch and release-note convention

Each actively implemented or completed feature receives a stable `F-##` identifier in the table above. Use that ID in its branch name as `feature/<F-##>_<short-kebab-case-description>`; for example, `feature/F-01_worklog-data-foundation`. Keep the ID with the feature when it is completed so release notes can refer to the same stable identifier.
| Product reliability and delivery | Test isolation, SonarQube coverage reporting, installer-size investigation, and measured performance work make releases more dependable. | OI-16, OI-17, OI-18, OI-19 | In progress (OI-18, OI-19 closed) |
