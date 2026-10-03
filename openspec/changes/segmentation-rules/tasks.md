# Tasks

No `FocusTimer.Platform.Windows` change; Linux stubs need nothing extra.

## 1. Tracker (Core)

- [ ] 1.1 Add `Settings.SegmentationRules` (default empty, cloned, tolerant converter reused) and verify Persistence tests for round trip, missing list, and one malformed rule.
- [ ] 1.2 Add `SetSegmentationRules` to `SessionTracker` (valid rules only, identical list no-op, prompt sample when tracking) and change `HasWindowChanged` per design; verify tests for same-app title change, app change, unmatched app, title-only rule, excluded window, rule added mid-session, and unchanged behavior with no rules.

## 2. Editor (App)

- [ ] 2.1 Extract the exclusion list editing into a reusable list view model and verify the existing exclusion editor tests still pass unchanged.
- [ ] 2.2 Host a second list for segmentation rules in Developer Options with its own heading and help text; push both lists to the tracker in `TimerWidgetViewModel.ApplySettings`; verify view-model tests (validation, Cancel, no tracker change while drafting) and the headless view test shows both lists.

## 3. Docs and integration

- [ ] 3.1 Update `OpenIssues.md` (OI-14), `Features.md`, `ARCHITECTURE.md`, and `DEVELOPMENT.md`, then check `openspec/` and root docs for contradictions; verify by re-reading them.
- [ ] 3.2 Manual Windows walkthrough: add a browser rule, switch tabs, confirm one entry in Entries and Timeline, switch application and confirm a split; run `openspec validate segmentation-rules` and the full suite.
