# Open tasks

## F-02 worklog summary breakdown

- [x] Fix the Windows startup crash in `WindowsHotkeyService.CallWindowProc`. The import now targets `CallWindowProcW`; a native window message regression test passes, and the debug app stays open after launch.
- [x] F-02 task 3.2 (archived unchecked): check the Summary view in the running Windows app, in a light theme and in High Contrast. Confirmed complete by the user on 2026-10-08 through F-03 walkthrough (recorded in ManualUiWalkthrough.md).
- [x] F-02 task 4.1 (archived): the Summary tab now lives in the Worklog window (F-03), not in Settings. Covered by F-03 task 10.5 and confirmed complete by the user on 2026-10-08.

The F-02 implementation tasks are archived in `openspec/changes/archive/2026-10-02-worklog-summary-breakdown/tasks.md`.

## F-04 smarter automatic tracking

Manual Windows checks left open when archiving the F-04 changes:

- [ ] **Rule-based project tagging** (`openspec/changes/archive/2026-10-04-rule-based-projects/`, OI-08): Add a rule for an IDE/window in Settings → Logging, confirm Entries and Timeline relabel live and by-project summary updates after refresh, confirm a session project wins, remove the rule and confirm reversion.
- [ ] **Segmentation rules** (`openspec/changes/archive/2026-10-04-segmentation-rules/`, OI-14): Add a browser rule in Developer Options, switch tabs and confirm one continuous entry in Entries and Timeline, switch application and confirm a split.
- [ ] **Application exclusions** (`openspec/changes/archive/2026-10-03-app-exclusions/`, OI-32): Exclude an app by name in Developer Options, confirm Timeline shows an empty gap during its focus and the next app starts fresh, restart the app and confirm the rules persist.

## Distribution and packaging

- [ ] **Installer and runtime detection** (OI-16): Validate clean-machine installation, upgrade over prior build, and the .NET 8 runtime detection/download bootstrapper for `FocusTimer.Setup.framework-dependent.exe` in an environment without .NET 8 preinstalled.

