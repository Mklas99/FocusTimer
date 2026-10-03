# Design

## Context

`WorklogSummaryService`, the by-project grouping, and the Timeline already call `IProjectResolver.Resolve(entry)`;
`StoredProjectResolver` returns `entry.ProjectTag`. `TimeEntry` already has `ProjectAssignmentSource`
(`Unassigned`, `Session`, `Rule`, `Editor`, `Imported`) and a reserved `ProjectRuleId`. `app-exclusions` provides
`WindowMatchRule`, `WindowRuleMatcher`, the tolerant list converter pattern, and a reusable editor row pattern.

## Goals / Non-Goals

**Goals:** retroactive rule-based labels everywhere projects are shown; one matcher; no change to stored data.

**Non-Goals:** writing rule results into the CSV; per-rule priorities beyond list order; regex; project colors or metadata.

## Decisions

### 1. Read-time resolution, nothing stored

`RuleProjectResolver.Resolve(entry)` is pure over the entry and the current rule list. Rule edits therefore apply to
history immediately, and Cancel or removal cleanly reverts. `ProjectRuleId` stays null: storing it would freeze labels
and conflict with retroactivity. Revisit only if a rule audit trail is wanted.

### 2. Which entries are eligible

An entry is matched against rules only when `CaptureSource` is the active window and its stored project is blank
(`ProjectAssignmentSource` Unassigned). The assignment source, not just the tag text, decides whether the project is
explicit, so a session tag typed in the widget always wins over rules. Manual entries never match, because their app
and title text is user-written.

### 3. Rule list source

The resolver reads the current rule list through a small provider interface fed by `AppController.CurrentSettings`, so a
changed list is visible to the next read without recreating services. Summary, Entries, and Timeline view models call
the same resolver; the Entries tab currently displays the raw tag and must switch to the resolver.

### 4. Where the editor lives

A "Project rules" section on the Logging tab, not Developer Options: this is an everyday feature, not a tuning knob.
It reuses the list-editing view model extracted for `segmentation-rules` if that change lands first; otherwise it
extracts it itself. A project-name field is added per row.

### 5. Normalization

Project names are trimmed on commit. Case-insensitive grouping already exists in the by-project grouping.

## Risks / Trade-offs

- Retroactivity means a rule edit can change past totals per project (never the overall total). Help text says so.
- A resolver that runs per entry must stay cheap; matching is linear in rules and entries, fine for typical rule counts.
- Entries tab and Timeline need a refresh hook after Apply; covered by a task and a scenario.

## Open Questions

- Should the Entries editor show whether a project came from a rule (read-only marker)? Not required by the specs; decide at implementation.
- Rule count limit: none for now.
