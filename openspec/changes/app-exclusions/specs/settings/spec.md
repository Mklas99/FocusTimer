# Spec Delta

## ADDED Requirements

### Requirement: Developer application exclusion rules
Developer mode SHALL provide an ordered list of application exclusion rules in Developer Options, where each rule has
an application pattern and/or a window-title pattern. Users SHALL be able to add, edit, reorder, and remove rules.
The editor SHALL reject a rule with no non-blank pattern and SHALL show why. Pattern behavior follows the
`window-matching` capability.

#### Scenario: Add a rule
- **WHEN** the user adds a rule with an application pattern and applies Settings
- **THEN** the rule is saved and used by activity tracking

#### Scenario: Invalid rule
- **WHEN** the user leaves both patterns of a rule blank
- **THEN** the editor shows a validation message and the rule is not committed

#### Scenario: Developer mode locked
- **WHEN** developer mode is not unlocked
- **THEN** the exclusion editor is not shown, and previously saved rules remain in effect

### Requirement: Exclusion rule persistence and commit
Exclusion rules SHALL belong to the shared Settings draft and follow Apply, OK, Cancel, and commit-failure behavior.
Editing the draft SHALL NOT change tracking. Cancel SHALL restore the last applied list. A saved rule list SHALL
survive restart. A malformed saved rule SHALL be ignored with a logged warning without resetting other settings.

#### Scenario: Cancel after editing
- **WHEN** the user edits rules and cancels
- **THEN** tracking continues with the last applied list

#### Scenario: Restart
- **WHEN** the application restarts after rules were applied
- **THEN** the same rules, in the same order, are in effect

#### Scenario: Malformed saved rule
- **WHEN** one saved rule is malformed or empty
- **THEN** it is ignored with a warning and all other settings and rules load normally

#### Scenario: Missing list in existing settings
- **WHEN** saved settings have no exclusion list
- **THEN** no applications are excluded
