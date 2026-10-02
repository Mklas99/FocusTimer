# Spec Delta

## ADDED Requirements

### Requirement: Stable presentation during Settings commits
While Apply or OK is committing, the system SHALL preserve the displayed draft values, Settings palette, content opacity, and layout, and SHALL retain the widget's previewed appearance. Commit locking SHALL NOT apply disabled styling to the complete Settings content. Existing disabled states that are unrelated to committing SHALL remain visible.

#### Scenario: Appearance Apply is delayed
- **WHEN** a user applies previewed appearance values and saving takes time
- **THEN** Settings and the widget retain the preview without a disabled-state flash or content movement until the result is available

#### Scenario: Ordinary save failure
- **WHEN** an Apply fails and recovery is not required
- **THEN** Settings displays the error, retains the editable draft and its preview, and keeps the previous successful commit as the Cancel restore point

### Requirement: Input locking without changing draft appearance
The system SHALL block user edits and duplicate commits while Apply or OK is committing, including pointer, keyboard, paste, already-open editor menus, deferred import/color-picker results, and the widget's compact-mode action. Cancel and title-bar close SHALL remain blocked until the commit finishes. Blocked input SHALL NOT be replayed into the draft afterward.

#### Scenario: User attempts to edit during saving
- **WHEN** a commit is pending and the user types, pastes, uses a slider or dropdown, or clicks the widget's mode switch
- **THEN** the committing candidate and open draft remain unchanged, and those actions are not saved separately or deferred until editing resumes

#### Scenario: Save completes with Settings remaining open
- **WHEN** Apply succeeds
- **THEN** editing becomes available, valid prior editor focus is restored, and later Cancel restores the newly committed values

#### Scenario: User attempts to close during saving
- **WHEN** a commit is pending and the user presses Cancel or the title-bar close control
- **THEN** the window remains open until completion without abandoning persistence or recovery

### Requirement: Reserved Settings save feedback
The system SHALL show an accessible "Saving..." status while a validated Apply or OK is committing, in a reserved footer area that does not move the content or buttons. Validation failures SHALL show an error without entering the saving state. The status SHALL clear after success, ordinary failure, or recovery-required failure; existing error and retry actions SHALL remain available as appropriate.

#### Scenario: Commit starts and finishes
- **WHEN** Apply or OK begins a validated commit
- **THEN** "Saving..." is visible and announced while committing, then clears when the commit finishes; OK closes only after success and Apply keeps the window open

#### Scenario: Commit fails and requires recovery
- **WHEN** a commit cannot restore the previous state
- **THEN** the saving status clears, recovery feedback is shown, and another commit remains blocked until recovery succeeds

### Requirement: Play/Pause color editor and compatible themes
Settings SHALL replace the Timer Background editor with a Play/Pause color editor controlling the normal Start/Pause icon in both widget modes. The optional play/pause theme color SHALL default to Button Normal when absent and SHALL survive cloning, import/export, Apply/OK, and reopening. Existing TimerBackground values SHALL remain preserved and SHALL NOT be repurposed as play/pause colors. Explicit colors SHALL be validated before saving.

#### Scenario: User previews and commits Play/Pause color
- **WHEN** the user edits Play/Pause color and successfully presses Apply or OK
- **THEN** the normal Play and Pause glyphs immediately preview that color in both modes, the committed appearance matches the preview, and reopening or restarting retains the value

#### Scenario: User cancels Play/Pause color edits
- **WHEN** the user edits Play/Pause color after the last successful commit and presses Cancel or closes Settings
- **THEN** the prior committed Play/Pause color or inherited Button Normal color is restored without saving the discarded edit

#### Scenario: Older theme has no Play/Pause color
- **WHEN** settings or an imported theme omit the play/pause color
- **THEN** the normal Start/Pause icon inherits Button Normal, later Button Normal edits update it, and existing TimerBackground values are preserved through a save/export round trip

#### Scenario: Explicit Play/Pause color is invalid
- **WHEN** the user enters an invalid explicit Play/Pause color
- **THEN** the last valid appearance remains visible and Apply/OK reports validation failure without saving

### Requirement: Widget icon state color editors
Button Normal SHALL control ordinary widget icons; Play/Pause color SHALL control the normal Start/Pause icon. Hover, Pressed, and Disabled editors SHALL control corresponding icon foregrounds in both modes, including Start/Pause. Disabled SHALL override Pressed and Hover; Pressed SHALL override Hover. Existing background highlights, focus indicators, and opacity behavior SHALL remain. State color edits SHALL follow the existing live preview, commit, and Cancel rules.

#### Scenario: User interacts with widget buttons
- **WHEN** a widget button enters hover, pressed, or disabled state
- **THEN** its icon uses the corresponding shared state color and returns to its appropriate normal color when the state ends

#### Scenario: User edits a state color during preview
- **WHEN** the user edits a button state color while a widget button is in that state
- **THEN** the rendered icon immediately uses the new color, Apply/OK retains it, and Cancel restores the last successful commit

### Requirement: Reserved future status colors
Settings SHALL retain editable Success and Danger color fields and their saved theme values for future status displays. Help text SHALL state that they are reserved for future use and currently do not affect notifications. These values SHALL survive import/export and Apply/OK, and Cancel SHALL restore the last successful commit. Warning SHALL continue to color existing summary warnings.

#### Scenario: User edits a reserved status color
- **WHEN** the user edits Success or Danger
- **THEN** its swatch reflects the valid color, Apply/OK saves it, and Cancel restores it, without claiming an existing notification or status changes
