# Proposal

## Why

Project tagging is manual: the user types a project name into the timer, and every automatically tracked segment
inherits that single tag. Work in a known application (an IDE, a repository folder in a window title) gets no project
unless someone remembers to set it, so by-project summaries are mostly "Unassigned". `OI-08` asks for rules that map
application and window patterns to projects. F-02 already routes every summary through `IProjectResolver`, and
`app-exclusions` delivered the window matcher. This is the third change of the smarter-tracking branch.

## What Changes

- Add an ordered list of **project rules**: a window rule (application and/or title pattern) plus a project name.
- Add a rule-based project resolver that replaces the stored-tag resolver. An entry with an explicit project (set by the
  running session, the editor, or an import) keeps it. An automatically captured entry with no explicit project gets the
  project of the first matching rule, otherwise stays Unassigned.
- Resolution happens when entries are read, so editing rules re-labels history retroactively. Stored entries are not
  rewritten and the reserved rule-id column stays unused.
- Summaries, project filters, the by-project grouping, the Timeline, and the Entries table show the resolved project.
- Add a "Project rules" editor to Settings (not developer-only) with the usual Apply/OK/Cancel behavior.

## Capabilities

### New Capabilities
- `project-rules`: how a project is resolved for an entry from its stored project and the ordered rules.

### Modified Capabilities
- `worklog-summary`: summaries, filters, and groupings use the resolved project, including retroactively.
- `settings`: adds the project rule list, its editor, and persistence.

## Impact

- `FocusTimer.Core`: project rule model, resolver, `Settings` list; Host registers the new resolver in place of the stored one.
- `FocusTimer.Persistence`: tolerant list loading.
- `FocusTimer.App`: Settings editor; Entries tab and Timeline use the resolver.
- `FocusTimer.Platform.Windows`: none.
- Docs: `OpenIssues.md` (OI-08), `Features.md`, `ARCHITECTURE.md`, `DEVELOPMENT.md`.
- Depends on `app-exclusions` (matcher). Independent of `segmentation-rules`.
