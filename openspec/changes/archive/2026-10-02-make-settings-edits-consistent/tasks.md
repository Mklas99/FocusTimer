# Tasks

## 1. Establish the draft and restore point

- [x] 1.1 Add a deep `Settings` snapshot/copy path that preserves every persisted field, device identity, and an independent `Theme`; verify a round-trip test and mutation-isolation test in Core tests.
- [x] 1.2 Distinguish missing-file defaults from unreadable or malformed existing settings and expose load failure to the window; block editing, Apply, and OK until a successful Retry load, and verify delayed-load, failed-load, and retry tests leave the existing file untouched.
- [x] 1.3 Initialize the draft and restore point from saved settings while reading Windows auto-start registration separately; verify a mismatch leaves the saved toggle value unchanged and Cancel does not alter registration.
- [x] 1.4 Route preset choice, theme import, and reset through the draft without mutating `ThemeService.CurrentTheme`; verify tests show immediate `ThemeManager` preview but no save or shared-service mutation before Apply.
- [x] 1.5 Make Cancel and title-bar close use one idempotent discard path for a draft that has not been committed in this window; verify preview restoration, nonappearance discard, and Summary-tab navigation tests.

## 2. Make persistence and auto-start failures observable

- [x] 2.1 Add a transaction-capable settings write with a same-directory candidate, previous-state copy, and pending journal recorded before atomic replacement; keep ordinary `SaveAsync` atomic, and verify first-save, replacement, failed-write, and retry tests preserve the prior JSON.
- [x] 2.2 Detect an unfinished journal on load/startup and refuse to return its candidate as committed settings; retain recovery data and expose recovery-required status, and verify crash-point tests distinguish journal files from disposable temporary files.
- [x] 2.3 Change `IAutoStartService` and `WindowsAutoStartService` to report registration failure, including missing Run key or registry denial; update `LinuxAutoStartServiceStub` to the same contract without adding Linux registration, and verify Windows-focused service tests and Core stub compilation (OI-06).

## 3. Finish Apply, OK, and runtime activation

- [x] 3.1 Replace the fire-and-forget `SettingsApplied` path with an awaited App-owned runtime activation result and prevent concurrent commits; verify tests for widget, topmost, hotkey, and polling reload plus an activation failure that starts compensation.
- [x] 3.2 Add a Settings commit coordinator that reconciles Windows registration to the saved candidate, awaits activation, and compensates in reverse order on failure; make the App-owned startup path retry journalled file/registration restoration before activation, and verify fault-injection and repeated-startup-failure tests distinguish ordinary failure from `RecoveryRequired` and block another commit until Retry recovery succeeds.
- [x] 3.3 Show a warning when saved auto-start differs from observed Windows registration and explain that Apply/OK will reconcile it, including an unrelated edit; verify mismatch, Cancel, successful reconciliation, and failed-reconciliation tests.
- [x] 3.4 Reject Cancel and title-bar close while a commit runs, without queuing a discard; verify delayed Apply/OK tests keep the window and preview stable until the result, then allow a new close request or successful OK close.
- [x] 3.5 Make Apply and OK share full-draft validation and commit logic; advance the restore point only after success and close only for successful OK; verify mixed-tab tests for Apply followed by further edits and Cancel, invalid polling interval, failed save, and retry.
- [x] 3.6 Show load, validation, drift, commit, and recovery-required messages in Settings without losing the open draft; provide Retry load and Retry recovery actions, and verify errors, commit blocking, corrected retry, and close warning in a focused UI/view-model check.
- [x] 3.7 Update `docs/versions/current/OpenIssues.md`, OI-21 detail, and relevant design notes for the delivered consistency behavior, failure recovery, and the still-open immediate-change option and OI-24 polish; verify the documents agree with the Settings and auto-start specs.

## 4. Integration checks

- [x] 4.1 Run the affected Core, Persistence, App, Host, and Windows-platform tests/builds; verify no regression in startup registration, theme preview, Summary refresh, or developer polling interval behavior.
- [x] 4.2 Exercise the Windows Settings window with edits across tabs, Apply followed by more edits and Cancel, close during a delayed commit, imported theme preview, load failure, auto-start drift, and ordinary/incomplete recovery; verify saved JSON, registration, running widget, and visible messages match the spec.
