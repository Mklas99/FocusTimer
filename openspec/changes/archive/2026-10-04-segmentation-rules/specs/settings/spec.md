# Spec Delta

## ADDED Requirements

### Requirement: Developer segmentation rules
Developer mode SHALL provide an ordered list of segmentation rules in Developer Options, each with an application
pattern and/or a window-title pattern, with add, edit, reorder, and remove, and with validation that rejects a rule
without a non-blank pattern. The list SHALL explain that matching applications are not split when only the window title changes.

#### Scenario: Add a rule
- **WHEN** the user adds a rule for an application and applies Settings
- **THEN** the rule is saved and used by activity tracking

#### Scenario: Invalid rule
- **WHEN** both patterns of a rule are blank
- **THEN** a validation message is shown and the rule is not committed

### Requirement: Segmentation rule persistence and commit
Segmentation rules SHALL belong to the shared Settings draft and follow Apply, OK, Cancel, and commit-failure behavior.
Editing the draft SHALL NOT change tracking, Cancel SHALL restore the last applied list, and the list SHALL survive a
restart. A malformed saved rule SHALL be ignored with a logged warning without resetting other settings, and a missing
list SHALL mean no segmentation rules.

#### Scenario: Cancel after editing
- **WHEN** the user edits segmentation rules and cancels
- **THEN** tracking continues with the last applied list

#### Scenario: Missing list in existing settings
- **WHEN** saved settings have no segmentation list
- **THEN** every title change still starts a new segment
