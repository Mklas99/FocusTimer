# FocusTimer - Next-Version Features

> This document groups larger outcomes for the next version. Candidate scope and priority may change before implementation.

`OpenIssues.md` is the detailed backlog and source of truth. This document groups related issues into outcomes instead of repeating their implementation details.

## Active implementation and completed features

| ID | Feature | Outcome | Related issues | Status | Branch / change |
|---|---|---|---|---|---|
| F-01 | Worklog data foundation | Establishes a versioned, safe, storage-neutral worklog foundation with durable entry and running-session identities, schema-safe CSV handling, querying, and same-day mutation support. Unsupported development files require manual move/removal; OI-20 owns any future migration strategy. | OI-07; enables OI-04, OI-08, OI-12, OI-15 | Completed | `feature/F-01_worklog-data-foundation` / `establish-worklog-data-foundation` |
| F-02 | Worklog summary breakdown | Adds a reusable, UI-free summary service (range, grouping, filters, Unassigned bucket, visible read warnings) and a Today breakdown by application or project in a new Settings Summary tab. Built so more ranges, filters, groupings, rule-based project detection, and a tray-opened report window plug in without a rewrite. | OI-04 (partial); enables OI-08, OI-12, OI-15 | In progress | `feature/F-02_worklog-summary-breakdown` / `worklog-summary-breakdown` |
| M01 | UI design system foundation | Establishes a three-tier token architecture, a single authoritative style source, tokenized spacing and radii, accessible 24x24 hit targets with focus rings, aligned Full/Compact modes, and a material fallback order with a solid fallback surface. | Supports OI-03 and OI-13 (partial) | Completed | `maintainance/M01_establish-system-design-foundation` / `introduce-ui-design-system` |
| M02 | Reliability and quality tooling | Isolates persistence tests from the real AppData path, feeds coverage reports to SonarQube in CI, and clears the resulting SonarQube/StyleCop findings. | OI-18, OI-19 | Completed | `maintainance/M02_reliability-and-quality-tooling` |
| M03 | Widget design adaptation | Adds a single widget shell, platform blur choices, and independent tint opacity. Windows testing found Soft falls back to clear transparency and Strong's AcrylicBlur looks nearly solid; OI-28 tracks a custom backdrop. | OI-25; enables OI-28 | In progress | `maintainance/M03_design-adaptation` / `restore-widget-frosted-glass` |

## Feature candidates

| Feature / improvement collection | Outcome | Related issues | Status |
|---|---|---|---|
| Everyday desktop control | The widget restores safely, minimizes predictably, offers configurable hotkeys, exposes useful tray actions, and makes logs easy to find. | OI-01, OI-02, OI-03, OI-05, OI-13 | Candidate |
| UI appearance and usability | Settings controls have a consistent modern look, readable theme colors, and one color palette accordion in Appearance; widget background opacity works as configured, with a later custom backdrop investigation. | OI-24, OI-25, OI-28 | Candidate |
| Better worklog management and reporting | Users can review, correct, delete, and export tracked work, with useful breakdowns by application and project. | OI-04, OI-07, OI-09, OI-12, OI-15 | Candidate |
| Manual time log | Users can add missed work intervals to the worklog with a start, end, and work details. | OI-22 | Candidate |
| Report timeline view | Users can inspect recorded work intervals in chronological order, including manual entries. | OI-23; complements OI-04 | Candidate |
| Smarter automatic tracking | Projects can be inferred from activity and advanced users can tune sampling and segmentation behavior. | OI-08, OI-14 | Partial (polling configuration delivered; project detection and segmentation rules open) |
| Focus modes | Pomodoro cycles and optional sound cues support deliberate work and break routines. | OI-10, OI-11 | Candidate |
| Linux feature parity | Linux receives real implementations for the platform integrations that currently use stubs. | OI-06 | Long-term candidate |
| Product reliability and delivery | Smaller installer choices, measured performance work, and a reproducible F5 launch make releases and development builds more dependable. Test isolation and SonarQube coverage reporting are delivered by M02. | OI-16, OI-17, OI-26 (OI-18, OI-19 completed by M02) | Improvement collection (partly delivered) |
| Data evolution and compatibility | Future compatibility-sensitive releases have an explicit backup, migration, rollback, and legacy-retirement strategy before persistent formats must be preserved. | OI-20 | Investigation |
| Settings experience and theme consistency | Settings controls, theme colors, and Save/Apply/Cancel behavior work consistently across all pages, with a documented widget and menu theme boundary. | OI-21 | Candidate |

## Feature branch and release-note convention

Each actively implemented or completed feature receives a stable `F-##` identifier in the table above. Use that ID in its branch name as `feature/<F-##>_<short-kebab-case-description>`; for example, `feature/F-01_worklog-data-foundation`. Keep the ID with the feature when it is completed so release notes can refer to the same stable identifier.

Foundation and maintenance work that is not a user-facing feature uses a stable `M##` identifier and a `maintainance/<M##>_<short-kebab-case-description>` branch, for example `maintainance/M01_establish-system-design-foundation`.
