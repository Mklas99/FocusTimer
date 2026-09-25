# Open tasks

## F-02 worklog summary breakdown

- [ ] Fix the Windows startup crash in `WindowsHotkeyService.CallWindowProc`. The `LibraryImport` declaration lacks the `CallWindowProcW` entry point. Add a regression check and confirm the app starts.
- [ ] Finish OpenSpec task 3.2: open the Summary view in the running app, check its rows and states, and inspect it in two themes including High Contrast.
- [ ] Finish OpenSpec task 4.1: open Settings, select Summary, and confirm today's rows appear. The tab selection test already passes; the running app check remains.

The detailed implementation tasks are in `openspec/changes/worklog-summary-breakdown/tasks.md`. The startup crash blocks the two visual checks above. Once they pass, mark the OpenSpec tasks complete and update the F-02 status in `docs/versions/current/Features.md` and OI-04 in `docs/versions/current/OpenIssues.md` as appropriate.
