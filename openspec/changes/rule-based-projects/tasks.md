# Tasks

No `FocusTimer.Platform.Windows` change; Linux stubs need nothing extra.

## 1. Resolver (Core)

- [x] 1.1 Add the project rule model and `Settings.ProjectRules` (default empty, cloned, tolerant list converter) and verify Persistence tests for round trip, missing list, and malformed or nameless rules being dropped with a warning.
- [x] 1.2 Add `RuleProjectResolver` and a rule-list provider interface; verify tests for explicit-project-wins, first-match-wins, no match, manual entries never matching, and case or space normalization.
- [x] 1.3 Register the resolver in the Host in place of `StoredProjectResolver` and feed it the applied settings; verify a Host or App test that an applied rule list changes the next resolution.

## 2. Views (App)

- [x] 2.1 Make the Entries table and Timeline use the resolver, and verify tests that Entries, Timeline, and Summary show the same project for a rule-matched entry.
- [x] 2.2 Refresh open Worklog tabs after Apply and verify a view-model test that a changed rule list updates resolved projects on next refresh.

## 3. Editor and docs

- [x] 3.1 Add the Project rules editor to the Logging tab (pattern fields plus project name, validation, reorder) with Apply/OK/Cancel behavior; verify view-model tests and a headless test that it is visible without developer mode.
- [x] 3.2 Update `OpenIssues.md` (OI-08), `Features.md`, `ARCHITECTURE.md`, and `DEVELOPMENT.md`; check `openspec/` and root docs for contradictions; verify by re-reading them.
- [ ] 3.3 Manual Windows walkthrough: add a rule for an IDE, confirm yesterday's by-project summary changes, set a session project and confirm it wins, remove the rule and confirm reversion; run `openspec validate rule-based-projects` and the full suite.
