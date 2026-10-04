# Spec Delta

## Purpose

Defines how the project of a worklog entry is decided from the entry's stored project and an ordered list of rules that
map application and window patterns to project names.

## ADDED Requirements

### Requirement: Project rule shape
A project rule SHALL consist of a window rule (application and/or window-title pattern, as defined by `window-matching`)
and a non-blank project name. A rule missing either part SHALL be invalid and SHALL never apply.

#### Scenario: Rule without a project name
- **WHEN** a rule has a pattern but a blank project name
- **THEN** it is invalid and never assigns a project

### Requirement: Project resolution order
The system SHALL resolve the project of an entry as follows: an entry with an explicit project (assigned by the running
session, the editor, or an import) keeps it; otherwise an automatically captured entry takes the project of the first
rule, in list order, whose window rule matches the entry's application and window title; otherwise the entry has no
project. Manually added entries SHALL NOT be matched against rules.

#### Scenario: Explicit project wins
- **WHEN** an entry has a project set by the user and also matches a rule for another project
- **THEN** the user's project is used

#### Scenario: Rule assigns a project
- **WHEN** an automatically captured entry has no project and matches a rule
- **THEN** the rule's project is used

#### Scenario: First matching rule wins
- **WHEN** two rules match an entry
- **THEN** the earlier rule's project is used

#### Scenario: No match
- **WHEN** an entry has no project and no rule matches
- **THEN** it has no project and appears as Unassigned in by-project views

#### Scenario: Manual entry without a project
- **WHEN** a manually added entry has no project
- **THEN** it stays Unassigned even if its application text would match a rule

### Requirement: Retroactive, non-destructive resolution
Project rule resolution SHALL be applied when entries are read. Adding, changing, or removing a rule SHALL change the
resolved project of existing matching entries everywhere they are shown, and SHALL NOT rewrite stored entries.

#### Scenario: Rule added after work was recorded
- **WHEN** the user adds a rule matching entries recorded yesterday
- **THEN** yesterday's summary shows them under the rule's project, and the stored entries are unchanged

#### Scenario: Rule removed
- **WHEN** the user removes a rule
- **THEN** entries it had labeled return to their stored project, or to Unassigned

#### Scenario: Editing a rule-labeled entry
- **WHEN** the user edits an entry whose project comes from a rule
- **THEN** the Project field stays empty (the stored project is unchanged) and a hint names the rule's project, so saving without typing a project keeps the entry rule-labeled; the entry details show the project as set by a rule

#### Scenario: Entry without application or title text
- **WHEN** a stored entry has no application name or window title
- **THEN** resolution does not fail; the entry simply matches only rules whose pattern accepts empty text
