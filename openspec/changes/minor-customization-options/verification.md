# Verification report

Verified on 2026-10-09 for `minor-customization-options` (spec-driven).

| Dimension | Result |
|---|---|
| Completeness | 11/11 implementation tasks complete; 6/6 added or modified requirements mapped to implementation. |
| Correctness | Focused regression checks pass for all three OIs. Native interaction verification for OI-35, OI-41, and OI-37 was confirmed complete by the user on 2026-10-09. The full headless suite is not verified because broader runs stalled and were stopped. |
| Coherence | Existing settings snapshots, commit/rollback, appearance preview, severity styling, and DI paths retained. Main specs and current docs updated. |

## Acceptance

- [x] OI-35 widget placement verification, confirmed by the user on 2026-10-09.
- [x] OI-41 notification preferences verification, confirmed by the user on 2026-10-09.
- [x] OI-37 Developer toggle verification, confirmed by the user on 2026-10-09.

The independent review in this session passed 90 focused App tests, 19 headless tests, one persistence test, and git diff --check, with no actionable findings.

## OI-35

`Settings.WidgetButtonsBelowClock` defaults to beside and survives cloning and JSON persistence. Full and compact views bind their grid placement to the appearance snapshot. Both clock and controls span the same columns below the clock; compact controls become horizontal, and their outer margins stay symmetric. Full-mode project input remains below both rows.

Evidence: `CustomizationSettingsTests`, `WidgetPlacementTests` (six full/compact cases at 0.5, 1, and 3 scale, including centering, target size, containment, and discard), `CustomizationCommit_FailedSaveRetryAndDiscardPreserveTheLastCommit`, and four controller/window close-or-Cancel cases. Existing timer/widget App tests also pass.

## OI-41

General exposes information/warning enabled flags and three dismissal inputs. Whole seconds from 1 to 120 are required; missing settings keep enabled flags and 5/15/15 seconds. `NotificationPolicy` snapshots committed preferences and provides safe fallback durations for invalid stored integers. `AppController` activates preferences at startup and coordinated runtime activation; compensation uses the same activation path. Errors remain enabled. Break reminders bypass general suppression, use the information duration, and never time out when acknowledgement is required. Open windows keep their original timer.

Evidence: persistence defaults/round-trip; policy snapshot, suppression, mandatory error, per-severity timeout, and fallback tests; invalid decimal/null/out-of-range draft tests; save failure/retry/Apply/OK/discard tests; startup and real window commit tests. `CommittedPolicyControlsRealToastsAndDoesNotRetargetOpenTimers` runs the actual dispatcher loop and confirms suppression, automatic error and nonacknowledgement reminder dismissal, retained existing-toast timing after a policy change, and mandatory acknowledgement behavior.

## OI-37

The existing About version gesture toggles the draft after every seven editable clicks. Loading/commit/disposal checks and an explicit recovery guard reject blocked gestures. Replacing a draft resets partial counts. Hiding Developer mode leaves its log level, interval, and rules intact.

Evidence: repeated toggle without saves, commit-blocked clicks, disposal, partial-count reset, failed save/retry, Apply/OK, Cancel/title-bar close, and persisted deactivation/reopening tests. A saved 42-second polling interval and Warning log level survive hiding and reopening.

## Commands and results

| Check | Result |
|---|---|
| Core tests, solution run | 339 passed |
| Persistence tests, solution run | 104 passed |
| Windows platform tests, solution run | 70 passed |
| Host tests, solution run | 22 passed |
| App tests, final project run | 423 passed, 5 existing opt-in native tests skipped |
| Focused headless run | 19 passed |
| Windows Host build, `--no-restore --verbosity quiet` | Passed; 0 warnings and 0 errors |
| `openspec validate minor-customization-options --strict` | Passed |
| Strict validation of widget-ui, notifications, and settings main specs | Passed; informational existing long-requirement notices only |
| `git diff --check` | Passed |

The focused headless command was:

```powershell
dotnet test tests/FocusTimer.App.HeadlessTests/FocusTimer.App.HeadlessTests.csproj --no-restore --filter 'FullyQualifiedName~WidgetPlacementTests|FullyQualifiedName~NotificationWindowTests|FullyQualifiedName~Customization_WindowCloseOrCancel' --verbosity quiet --blame-hang-timeout 30s
```

The initial solution test run and a broader headless class selection did not finish. Their repository-specific testhost processes were stopped after stalls; those runs are not passing evidence. The focused run above finished successfully.

## Remaining verification limits

No critical implementation issues found in the checks that ran. The user confirmed the remaining verification for OI-35, OI-41, and OI-37 complete on 2026-10-09. This is user-reported acceptance; no additional native captures, display-scale values, or automated native runs were supplied. The complete headless suite remains unverified because of the stalls. The five opt-in native tests were not run. This change remains active and is not archived.
