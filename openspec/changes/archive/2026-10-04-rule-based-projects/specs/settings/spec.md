# Spec Delta

## ADDED Requirements

### Requirement: Project rules editor
Settings SHALL provide an ordered list of project rules, each with an application pattern and/or a window-title pattern and
a project name, with add, edit, reorder, and remove, and with validation that rejects a rule lacking a pattern or a
project name. The editor SHALL be available without unlocking developer mode and SHALL explain that the first matching
rule wins and that rules only label entries without an explicit project.

#### Scenario: Add a rule
- **WHEN** the user adds a rule with an application pattern and a project name and applies Settings
- **THEN** the rule is saved and summaries use it

#### Scenario: Invalid rule
- **WHEN** a rule has no pattern or no project name
- **THEN** a validation message is shown and the rule is not committed

### Requirement: Project rule persistence and commit
Project rules SHALL belong to the shared Settings draft and follow Apply, OK, Cancel, and commit-failure behavior, SHALL
survive a restart, and SHALL be applied to already open worklog views after Apply (Entries and Timeline immediately, Summary on its next refresh). A malformed saved rule SHALL be
ignored with a logged warning without resetting other settings, and a missing list SHALL mean no rules.

#### Scenario: Cancel after editing
- **WHEN** the user edits project rules and cancels
- **THEN** summaries keep using the last applied rules

#### Scenario: Open Worklog window after Apply
- **WHEN** the Worklog window is open and the user applies a changed rule list
- **THEN** the Entries table and Timeline show the new resolved projects without reloading, and the Summary shows them on its next refresh
