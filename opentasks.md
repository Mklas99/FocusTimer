# Open tasks

## F-02 worklog summary breakdown

- [x] Fix the Windows startup crash in `WindowsHotkeyService.CallWindowProc`. The import now targets `CallWindowProcW`; a native window message regression test passes, and the debug app stays open after launch.
- [ ] Finish OpenSpec task 3.2: open the Summary view in the running app, check its rows and states, and inspect it in two themes including High Contrast.
- [ ] Finish OpenSpec task 4.1: open Settings, select Summary, and confirm today's rows appear. The tab selection test already passes; the running app check remains.

The detailed implementation tasks are in `openspec/changes/worklog-summary-breakdown/tasks.md`. The screenshot from 2026-09-25 shows the older installed app at `C:\Program Files\FocusTimer\FocusTimer.Host.exe`; it cannot verify this branch. The two visual checks remain open because the branch build's Settings window could not be reached through the tray in this test environment. Once they pass, mark the OpenSpec tasks complete and update the F-02 status in `docs/versions/current/Features.md` and OI-04 in `docs/versions/current/OpenIssues.md` as appropriate.
