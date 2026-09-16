# FocusTimer — v0.1 Baseline: Open Issues

Gaps, partial items, and known limitations found in the code as of this baseline. Referenced from [Features.md](Features.md) by ID. "Origin" points to the planning doc (now under [archived/](../../archived/), paths below relative to `docs/`) that first proposed the item, for context — not a commitment to build it as originally specced.

| ID | Area | Gap | Origin | Notes |
|---|---|---|---|---|
| OI-01 | Global Hotkeys | Hotkey fields in Settings → Hotkeys tab are read-only (`IsReadOnly="True"`, tooltip "editable hotkeys coming soon") | `archived/Milestone5-Implementation.md`, `archived/plan/02_QualityOfLifePromts.md` | Registration/dispatch works; only the capture UI is missing |
| OI-02 | System Tray | No "Switch Project → last used" quick action in the tray menu | `archived/temporary.notes2improve.md` | Tray menu is currently fixed: Show/Hide, Start/Pause, Settings, Exit |
| OI-03 | Widget UI | Window position/size is not persisted; widget does not restore where the user left it, no multi-monitor visibility validation | `archived/plan/02_QualityOfLifePromts.md` (Epic A4) | No `WindowX/Y/Width/Height` in `Settings` |
| OI-04 | Settings / Reporting | No "Today" breakdown view (by app / by project); only an aggregate total shown in the tray tooltip | `archived/plan/02_QualityOfLifePromts.md` (Epic D1) | `TodayStatsService` only exposes a running total, not per-app/per-project sums |
| OI-05 | Diagnostics | No "Open logs folder" button in Settings | `archived/plan/02_QualityOfLifePromts.md` (Epic E1) | Serilog file logging itself is implemented and working |
| OI-06 | Cross-Platform | Linux implementations are still no-op stubs (active window, notifications, hotkeys, auto-start, idle detection) | `archived/Milestone3-Implementation.md`, `archived/Milestone5-Implementation.md` | App builds and runs on Linux; feature parity is the gap |
| OI-07 | Data Features | `TimeEntry` schema unchanged since Milestone 3 — no `IdleFlag`, `SessionId`, or `SourcePlatform` columns | `archived/plan/02_QualityOfLifePromts.md` (Epic C1) | Blocks idle-segment tagging and cross-session/platform analysis later |
| OI-08 | Data Features | No rules-based automatic project tagging (pattern → project mapping) | `archived/plan/02_QualityOfLifePromts.md` (Epic C3), `archived/temporary.notes2improve.md` | Project tagging today is manual free-text only |
| OI-09 | Data Features | No SQLite storage option; CSV is the only backend | `archived/plan/01.1_UpdateSuggestions.md` (§3.3) | Would help once per-app/per-project querying (OI-04) is needed |
| OI-10 | Focus Modes | No Pomodoro mode (work/break cycle automation) | `archived/plan/02_QualityOfLifePromts.md` (Epic B2), `archived/temporary.notes2improve.md` | |
| OI-11 | Focus Modes | No sound cues on break/resume events | `archived/plan/02_QualityOfLifePromts.md` (Epic B3), `archived/temporary.notes2improve.md` | |
| OI-12 | Reporting | No export presets (e.g. "export last week for timesheets") | `archived/plan/01.1_UpdateSuggestions.md` (§7) | The existing "Export..." button in Settings is theme export only, not data export |
| OI-13 | Widget UI (unverified) | Visual bugs noted during development: compact-mode width adaptation, scale affecting only children not the window, minimize behavior moving/shrinking instead of minimizing | `archived/temporary.notes2improve.md` | Not perfect but mostly fixed. |

## Superseded / no longer applicable

`archived/CodeFunctionReview1.md` flagged critical architecture gaps (untracked/lost session data, in-memory-only "fake" stats, tray/theme duplication) against an earlier snapshot of the code. `archived/plan/03_RefactoringPlan.md` was written directly in response and has been fully carried out — `ISessionRepository`/`CsvSessionRepository` persist tracked time, `TodayStatsService` reads back from that repository, and the tray/theme split (`TrayStateController` as view-logic, `ThemeService`/`ThemeManager` as state/application) matches the plan's corrected design. No action needed; kept here only so the history isn't lost.
