# Open tasks

## F-02 worklog summary breakdown

- [x] Fix the Windows startup crash in `WindowsHotkeyService.CallWindowProc`. The import now targets `CallWindowProcW`; a native window message regression test passes, and the debug app stays open after launch.
- [ ] F-02 task 3.2 (archived unchecked): check the Summary view in the running Windows app, in a light theme and in High Contrast. A headless Skia render of the Summary view in Light, Dark, Nord, and High Contrast (with rows, shares, and the empty state) looked correct on 2026-10-02; only the real-app look remains.
- [ ] F-02 task 4.1 (archived): the Summary tab now lives in the Worklog window (F-03), not in Settings. Open the tray menu, choose Worklog, select Summary, and confirm the day's rows appear and that Refresh and the day selector work. Covered by F-03 task 10.5.

The detailed implementation tasks are in `openspec/changes/archive/2026-10-02-worklog-summary-breakdown/tasks.md`. The screenshot from 2026-09-25 shows the older installed app at `C:\Program Files\FocusTimer\FocusTimer.Host.exe`; it cannot verify this branch. The two visual checks remain open because the branch build's Settings window could not be reached through the tray in this test environment. Once they pass, mark the OpenSpec tasks complete and update the F-02 status in `docs/versions/current/Features.md` and OI-04 in `docs/versions/current/OpenIssues.md` as appropriate.
