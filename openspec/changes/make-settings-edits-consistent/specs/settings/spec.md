# Spec Delta

## ADDED Requirements

### Requirement: Shared Settings draft
The system SHALL keep edits from every editable Settings tab in one draft, separate from the last successfully applied settings. Navigation, theme selection, theme import, and theme reset SHALL NOT persist the draft.

#### Scenario: User edits multiple tabs
- **WHEN** the user changes General, Logging, Appearance, or Developer values and switches tabs
- **THEN** the edited values remain in the open draft and the saved settings remain unchanged

#### Scenario: User chooses a preset or imports a theme
- **WHEN** the user selects, imports, or resets a theme in Appearance
- **THEN** the draft theme changes and no settings file is saved until Apply or OK succeeds

### Requirement: Settings load safety
The system SHALL distinguish a missing settings file from an existing file that cannot be read or parsed. A failed load SHALL block editing and commits, leave the existing file untouched, and show an error with a retry action.
At startup, a failed load SHALL stop activation of default settings and offer the same retry path.

#### Scenario: Existing settings cannot be loaded
- **WHEN** an existing settings file is unreadable or malformed when Settings opens
- **THEN** no editable draft is offered, Apply and OK are unavailable, and the user can retry loading without overwriting that file

#### Scenario: Settings file is absent
- **WHEN** no settings file exists and Settings opens
- **THEN** valid defaults form the draft and can be committed normally

### Requirement: Settings commit actions
The system SHALL validate and save the full draft as one Settings commit. Apply SHALL keep the window open; OK SHALL close it only after success. A successful commit SHALL activate the saved values in the running app and become the new discard restore point.

#### Scenario: Apply succeeds
- **WHEN** the user changes values on multiple tabs and Apply succeeds
- **THEN** all valid draft values are saved and activated together, the window remains open, and later Cancel retains those applied values

#### Scenario: OK succeeds
- **WHEN** the user presses OK and the full draft is saved and activated successfully
- **THEN** the window closes with the committed values active

#### Scenario: Validation fails
- **WHEN** any editable value is invalid and the user presses Apply or OK
- **THEN** nothing is saved or activated, the window stays open, the draft is retained, and the invalid value has visible feedback

#### Scenario: User tries to close during a commit
- **WHEN** Apply or OK is still committing and the user presses Cancel or the title-bar close control
- **THEN** the close request is declined, the in-progress commit runs to a result, and the window shows its result before a new close request can discard edits; a successful OK may close the window itself

### Requirement: Settings discard and appearance preview
The system SHALL treat appearance changes shown before commit as temporary previews. Cancel and title-bar close SHALL discard the current draft and restore all previewed appearance to the last successful commit, including edits made after an earlier Apply.

#### Scenario: Cancel after a theme preview
- **WHEN** the user previews a theme and presses Cancel without a successful commit
- **THEN** the saved theme and running appearance return to the last successful commit

#### Scenario: Close after Apply and further edits
- **WHEN** Apply succeeds, the user makes further draft and appearance changes, then closes Settings with the title-bar control
- **THEN** the first applied values remain saved and active and the later edits are discarded

#### Scenario: Nonappearance edit is canceled
- **WHEN** the user changes a nonappearance setting and presses Cancel before a successful commit
- **THEN** the saved value and running behavior remain unchanged

### Requirement: Settings commit failure
The system SHALL NOT report a successful commit or advance the restore point until persistence, auto-start reconciliation, and runtime activation finish. A failed write SHALL leave the prior file intact. After a later failure, the system SHALL attempt restoration; if restoration fails, it SHALL retain recovery data and block new commits until recovery succeeds.

#### Scenario: Settings file write fails
- **WHEN** a settings file write fails during Apply or OK
- **THEN** the prior saved file remains readable, the new draft stays in Settings, no success is reported, and the window remains open with an error

#### Scenario: Failure after the file was replaced
- **WHEN** auto-start reconciliation or runtime activation fails and restoration succeeds
- **THEN** the previous saved and active values are restored, the draft remains available in the open window, and the commit is reported as failed

#### Scenario: Restoration fails
- **WHEN** a commit fails after replacement and restoration cannot complete
- **THEN** Settings shows a recovery-required error, preserves the prior-state recovery data, blocks another Apply or OK, and offers a recovery retry without claiming which values are active

#### Scenario: Recovery is still pending at startup
- **WHEN** the application starts with an unfinished settings recovery record
- **THEN** it attempts recovery before accepting or activating that candidate as committed settings and reports a recovery-required state if recovery still fails

#### Scenario: User retries after failure
- **WHEN** the user corrects the cause of an ordinary failed commit, or completes required recovery, and retries Apply or OK
- **THEN** the same draft can be committed without reopening Settings
