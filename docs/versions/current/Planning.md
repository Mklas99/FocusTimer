# FocusTimer - Planning

The next steps for the project, in the order they should happen. `OpenIssues.md` says what is missing, `Features.md` groups it into outcomes, and this file says what to do next. Finished steps leave this file when their change is archived.

## Open manual checks

Checks on a real Windows desktop that archived changes left open:

- [ ] **Rule-based project tagging** (OI-08, `openspec/changes/archive/2026-10-04-rule-based-projects/`): add a rule for an IDE or window in Settings → Logging, confirm Entries and Timeline relabel live and the by-project summary updates after refresh, confirm a session project wins, remove the rule and confirm the label reverts.
- [ ] **Segmentation rules** (OI-14, `openspec/changes/archive/2026-10-04-segmentation-rules/`): add a browser rule in Developer Options, switch tabs and confirm one continuous entry in Entries and Timeline, switch application and confirm a split.
- [ ] **Application exclusions** (OI-32, `openspec/changes/archive/2026-10-03-app-exclusions/`): exclude an app by name in Developer Options, confirm the Timeline shows an empty gap during its focus and the next app starts fresh, restart the app and confirm the rules persist.
- [ ] **Installer and runtime detection** (OI-16): validate a clean-machine install, an upgrade over the prior build, and the .NET 8 detection and download path of `FocusTimer.Setup.framework-dependent.exe` in an environment without .NET 8.

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
