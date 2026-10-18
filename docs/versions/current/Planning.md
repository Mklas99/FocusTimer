# FocusTimer - Planning

The next steps for the project, in the order they should happen. `OpenIssues.md` says what is missing, `Features.md` groups it into outcomes, and this file says what to do next. Finished steps leave this file when their change is archived.

## Before the last release is closed

Open manual checks, tracked in [OpenTasks.md](../../OpenTasks.md) so they have one home:

- Windows walkthroughs for rule-based projects (OI-08), segmentation rules (OI-14), and application exclusions (OI-32).
- Clean-machine install, upgrade, and .NET 8 runtime detection for the setup EXE (OI-16).

## Next steps

> [!NOTE]
> The order below is not decided yet. It lists candidates from [Features.md](Features.md) with the evidence that supports each; the user picks the scope and order of the next version.

| Candidate | Why it is a candidate | Decision needed |
|---|---|---|
| Everyday desktop control (OI-01, OI-02, OI-03, OI-05) | Four independent gaps in daily use: no editable hotkeys, no tray quick actions, no remembered window position, no shortcut to the logs folder | Smallest useful tray action set (OI-02) |
| Better worklog reporting (OI-04, OI-12, OI-23, OI-38, OI-39) | Single-day views exist; ranges, export, timeline buckets, and a faster project field are missing | Whether export comes before a storage change (OI-09) |
| Diagnostics (OI-30, OI-34, OI-43) | Developer tooling gaps; OI-34 needs an error-handling policy first | Policy scope for OI-34 |
| Reliability and delivery (OI-16, OI-17, OI-26) | Install path untested on a clean machine; VS Code F5 can start a stale build | None |

## Decisions already made

- 2026-10-17: the version is written once, in the root `VERSION` file.
- 2026-10-17: the changelog entry is written at each version bump and shown in the app's changes view.
- 2026-10-17: this file holds the next steps; earlier planning analysis lives in the version snapshots, for example [v0.2/Planning.md](../v0.2/Planning.md).
