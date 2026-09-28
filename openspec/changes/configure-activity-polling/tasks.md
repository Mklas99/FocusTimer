# Tasks

## 1. Capture the baseline

- [x] 1.1 Add an isolated Release measurement driver for foreground capture with scripted observations and synthetic settings/worklogs; verify it leaves real AppData and the user's running timer untouched and does not save personal window titles.
- [x] 1.2 Record the pre-change revision and three warmed-up runs for idle/stopped, logging disabled, stable foreground, same-process title changes, and process switches in `docs/versions/current/ActivityPollingPerformance.md`; verify raw CPU seconds, allocations, lookup counts/durations, handle counts, workload duration, machine, OS, and .NET details are present. Use the existing worklog baseline's reporting pattern, but keep capture evidence separate.

## 2. Persist the interval safely

- [x] 2.1 Add `Settings.ActivityPollingIntervalSeconds` with default 10, whole-second range 1–60, and fallback 10 for invalid stored values; verify xUnit Settings tests cover omitted, valid, zero, negative, and excessive values.
- [x] 2.2 Add property-local tolerant JSON integer parsing without changing unrelated serialization; verify JsonSettingsProvider tests preserve other valid settings with null, strings, fractional numbers, or unexpected JSON values for this property, and round-trip valid values as integers.
- [x] 2.3 Document the additive setting, defaults, supported range, and fallback in DEVELOPMENT.md; verify examples agree with model and persistence tests.

## 3. Separate scheduling and maintenance

- [x] 3.1 Extend SessionTracker test clock support to advance monotonic and civil time independently; verify tests reproduce interval deadlines without sleeps and wall-clock jumps do not affect capture cadence.
- [x] 3.2 Add effective interval/deadline scheduling in SessionTracker using its injected TimeProvider and the existing one-second TimerService tick; verify immediate start and 1/10/60-second due boundaries, unchanged-value no-op, changed-value rescheduling, failure cadence, and no catch-up burst after a delayed tick.
- [x] 3.3 Keep day-boundary maintenance outside foreground-capture admission and run it before final closure; verify midnight between samples, midnight during a slow lookup, pause before the next sample, multi-day delay, and existing DST tests produce valid same-day segments.
- [x] 3.4 Coordinate initial and periodic capture with one non-waiting capture reservation and only the latest pending startup; verify delayed-window tests show at most one lookup, no periodic backlog, stale-result rejection after stop/disable/restart, and completion/cancellation of superseded startup requests.
- [x] 3.5 Observe fire-and-forget tracking tasks in TimerService while preserving its public timing/state behavior; verify TimerService state/tick tests and injected failure tests show no unobserved startup or tick errors.
- [x] 3.6 Document observation-time attribution, maintenance independence, and capture lifecycle in ARCHITECTURE.md; verify normalized one-second segment sequences match baseline app/title transitions and lifecycle reasons.

## 4. Wire Developer Options and live application

- [x] 4.1 Add the numeric interval control and tradeoff text to the existing unlocked About/Developer Options expander; verify the Settings view builds, the seven-click unlock still works, and invalid fractional/out-of-range input is visibly rejected without saving.
- [x] 4.2 Apply the effective interval before capture in TimerWidgetViewModel initialization/reload, using the existing successful SettingsApplied flow; verify App tests for startup, live 1-to-5 and 5-to-1 changes, hidden Developer mode, logging disabled/re-enabled, and unrelated settings reload without deadline reset.
- [x] 4.3 Cover draft versus applied settings in SettingsWindowViewModel tests; verify Apply, OK, Cancel, Apply-then-edit-then-Cancel, and save failure preserve the specified persisted/active values and never reset elapsed time or split a segment solely due to an interval change.
- [x] 4.4 Document the control location and sampling tradeoff in DEVELOPMENT.md; update OI-14 with the delivered polling subset when verified, retaining the remaining segmentation scope and checking Features.md remains consistent.

## 5. Reduce Windows process lookups

- [x] 5.1 Add an internal injectable owned-process accessor to Platform.Windows while keeping IActiveWindowService unchanged; verify fake-lifetime tests preserve current name representation and existing access-denied/exited-process fallbacks. This Windows-native implementation has no real Linux equivalent yet (OI-06); verify Linux stubs still compile against the same interface.
- [x] 5.2 Add the single-entry cache using an owned lifetime handle and non-blocking liveness check, continuing title/PID reads on every capture; verify stable-process lookup counts, title changes, switching windows within one process, absent window, PID zero, process exit, and simulated PID reuse.
- [x] 5.3 Dispose cached references on replacement/failure/shutdown and synchronize lookup/disposal; verify failed lifetime acquisition uses uncached capture, failed lookups retry, fake resources are released exactly once, concurrent calls remain safe, and Host DI disposes the concrete singleton.
- [x] 5.4 Record cache ownership, fallback behavior, and measurement criteria in ARCHITECTURE.md and the capture performance document; verify repeated process switches and disposal show no growing retained handle count.

## 6. Verify the integrated result

- [x] 6.1 Run relevant Core, App, Persistence, Windows, and Host xUnit suites plus a Release solution build; verify all delta-spec scenarios are covered and normalized captures at explicitly configured 1 second and the new 10-second default, pause/exit persistence, timer display, and break-reminder behavior remain correct.
- [x] 6.2 Repeat the baseline workloads three times at interval 1, then measure the 10-second default and 60-second upper bound; verify same-process name lookup work falls, report run-to-run CPU/allocation variation, and investigate consistent regressions. If the lifetime cache is not beneficial, remove it, rerun checks, and document the decision while retaining interval configuration.
- [x] 6.3 Perform a Windows Settings/tray smoke check with an isolated app instance; verify Apply/Cancel/restart, continued one-second display updates, and resource cleanup without changing the user's running session.
- [x] 6.4 Reconcile the delivered subset and evidence across OI-14, OI-17, Features.md, root docs, and these artifacts; verify no unmeasured savings or full closure of OI-14/OI-17 is claimed, and validate this change before separately requested spec synchronization/archive.

## Verification evidence (2026-09-27)

- Release solution suites: Core 169, Persistence 68, App 57, Windows 37, Host 14; 345 passing tests combined. Release solution build: zero warnings/errors. Core was rerun after the final four regression cases were added.
- ActivityPollingTests covers deadlines, rescheduling, wall-clock independence, bounded slow capture/restart, title transitions, and multi-day pause boundaries; existing DST, timer, persistence, and reminder tests remain passing.
- ActivityPollingEditorTests and the isolated Avalonia driver cover validation, Apply/OK, discarded drafts, save failure, seven-click unlock, real live 1-to-5-to-1 reloads, unchanged reload, hidden mode, disabled/re-enabled logging, and uninterrupted timer ticks at 60 seconds.
- Windows smoke passed initially at 10 and after process restart at saved 60 with Developer mode hidden. Tray creation, settings reopen, and clean service disposal were verified using synthetic data under ignored artifacts/polling-smoke-integrated. The driver exited; the user's running session was untouched.
- ActivityPollingPerformance.md preserves three baseline runs and three after runs at each of 1/10/60 seconds, with measurement limits and the cache retention decision. Fake lifetime and Host disposal tests cover ownership separately from aggregate handle measurements.
- Main-spec synchronization is complete; archive remains a separate workflow step.
