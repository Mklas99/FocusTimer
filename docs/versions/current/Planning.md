# Thoughts from dev
this is a collection of the devs thouhgs and might nod be accurat or not explored further dont treat the notes as definate future changes/plans.

## Open issues from previous version



| ID | Area | Gap | Origin | Notes |
|---|---|---|---|---|
| OI-01 | Global Hotkeys | editable hotkeys | | Registration/dispatch works; only the capture UI is missing |
| OI-02 | System Tray | Quick actions (e.g. switch project, etc.) |  |  |
| OI-03 | Widget UI | Window position/size is not persisted |  | No `WindowX/Y/Width/Height` in `Settings` |
| OI-04 | Settings / Reporting | breakdown view / user report |  |  only running total, not per-app/per-project sums or reports |
| OI-05 | Diagnostics | "Open logs folder" button in Settings |  |  |
| OI-06 | Cross-Platform | Linux gap close |  | App builds and runs on Linux probably; feature parity is the gap |
| OI-07 | Data Features | `TimeEntry` schema unchanged since Milestone 3 — no `IdleFlag`, `SessionId`, or `SourcePlatform` columns | | Blocks idle-segment tagging and cross-session/platform analysis later |
| OI-08 | Data Features | rules-based automatic project tagging (pattern → project mapping) | | currently tagging is manual |
| OI-09 | Data Features | improved storage option | | currently only CSV. Would help once per-app/per-project querying (OI-04) is needed |
| OI-10 | Focus Modes | No Pomodoro mode (work/break cycle automation) | | |
| OI-11 | Focus Modes | (enable/disable) sound cues on break/resume events | | |
| OI-12 | Reporting | data export + presets | | currently theme export only |
| OI-13 | Widget UI | function of minimise button "weird" |  | it collapses and moves to the bottom (often missed) -> change fnc to system tray click? |

## Changes
### Requirement: Per-Application Time Segmentation
Current: The system SHALL poll the foreground application/window once per second while the timer is Running, and SHALL start a new log segment whenever the active application/window changes.
Update: -> introduce "advanced" settings to make this configuratble


## New features

### Requirement: Work-Logging On/Off Switch

add delete loggs functinality
add edit log functionality


## improvements

improve installer size
improve performants
