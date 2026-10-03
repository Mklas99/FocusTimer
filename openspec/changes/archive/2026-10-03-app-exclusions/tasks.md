# Tasks

All tasks are in `FocusTimer.Core`, `FocusTimer.Persistence`, and `FocusTimer.App`. None touches
`FocusTimer.Platform.Windows`, so no Linux-stub work is needed (matching runs on `ActiveWindowInfo`).

## 1. Window matcher (Core)

- [x] 1.1 Add the window rule model and glob matcher (case-insensitive, `*`/`?`, literal other characters, `.exe`-tolerant application pattern, blank-rule invalid, first-match evaluation); verify xUnit tests cover each `window-matching` scenario, including regex metacharacters in patterns.

## 2. Tracker exclusion (Core)

- [x] 2.1 Add the excluded state and `SetExclusionRules` to `SessionTracker`: close the open segment on an excluded sample, open nothing while excluded, start a fresh segment on the next non-excluded sample; verify tests with a fake clock cover switch to and from an excluded app, start on an excluded window, midnight while excluded, and lookup failure while excluded.
- [x] 2.2 Make a rule change trigger one prompt foreground sample (not the full interval) without closing segments by itself, and identical lists a no-op; verify tests for rule added and removed while the window is in front and for existing entries staying unchanged.
- [x] 2.3 Verify the timer, break reminders, and polling-interval behavior are unchanged by running the existing `SessionTracker` and `ActivityPolling` test suites.

## 3. Settings persistence

- [x] 3.1 Add the ordered exclusion list to `Settings` (default empty, included in the settings clone) with a property-level tolerant JSON converter that drops malformed or empty rules with a logged warning; verify Persistence tests for round trip, order, missing list, and one bad rule not resetting other settings.

## 4. Developer Options editor (App)

- [x] 4.1 Add exclusion rule editing (add, edit, reorder, remove, inline validation) to `SettingsWindowViewModel` on the shared draft, with Apply/OK/Cancel and commit-failure behavior; verify view-model tests for validation, Cancel restoring the last applied list, and no tracker change while editing.
- [x] 4.2 Add the editor to the Developer Options area, hidden while developer mode is locked; verify with a headless view test that it is absent when locked and bound when unlocked.
- [x] 4.3 Push the applied list to the tracker in `TimerWidgetViewModel` on settings load and Apply, next to `SetPollingInterval`; verify a test that an applied rule reaches the tracker and a Cancelled draft does not.

## 5. Docs and integration

- [x] 5.1 Update `OpenIssues.md` (OI-32 status), `Features.md`, and the tracker description in `ARCHITECTURE.md`, then check `openspec/` and root docs for contradictions; verify by re-reading the three files.
- [ ] 5.2 (OPEN at archive time; tracked in OpenIssues.md under OI-32) Manual Windows walkthrough: exclude one application by name, confirm the Timeline shows a gap and the next application starts fresh, restart and confirm rules persist; record the result in the change notes. Run `openspec validate app-exclusions` and the full test suite.
