# Settings Specification

## Purpose
Defines the Settings window's structure and its hidden developer-mode unlock.

## Requirements

### Requirement: Settings Tabs
The system SHALL provide a Settings window with General, Logging, Appearance, Hotkeys, and About tabs, covering auto-start, start-minimized, always-on-top, break reminders, worklog directory and retention, theme/opacity, hotkey display, and version/changelog/repo link. Worklog reporting SHALL be provided by the Worklog window, not by Settings.

#### Scenario: User opens Settings
- **WHEN** the user opens the Settings window
- **THEN** the General, Logging, Appearance, Hotkeys, and About tabs are available with their respective controls and no Summary tab is shown

#### Scenario: User opens the Summary tab
- **WHEN** the user looks for the Summary tab in Settings
- **THEN** there is none, and today's breakdown is available in the Summary tab of the Worklog window instead

### Requirement: Hidden Developer Mode
The system SHALL unlock a Developer section, including a log-level picker and an activity polling interval control, when the user clicks the version label 7 times in the About tab, and SHALL persist the unlock via `DeveloperModeEnabled`.

#### Scenario: User clicks the version label 7 times
- **WHEN** the user clicks the version label 7 times in the About tab
- **THEN** the Developer section, including the log-level picker and activity polling interval control, becomes visible and stays unlocked across restarts

### Requirement: Developer activity polling interval
The system SHALL provide a numeric control labelled "Activity polling interval (seconds)" in the unlocked Developer section. It SHALL accept whole-second values from 1 through 60 and default to 10 seconds. It SHALL explain that longer intervals reduce polling work but can miss short app visits. Developer-section visibility SHALL NOT reset or otherwise change an already saved interval.

#### Scenario: Developer edits the interval
- **WHEN** the Developer section is unlocked
- **THEN** its interval control displays the saved value, accepts whole numbers from 1 through 60, and explains the sampling tradeoff

#### Scenario: Invalid value is entered in the control
- **WHEN** the user enters a fractional or out-of-range interval
- **THEN** the invalid value is visibly rejected and is not saved or applied

#### Scenario: Interval survives restart with the section hidden
- **WHEN** the application restarts with a valid saved interval and Developer mode disabled
- **THEN** tracking still uses the saved interval and the Developer section stays hidden

### Requirement: Polling interval persistence and application
The system SHALL save and activate the interval through the existing Apply/OK flow only after validation and a successful settings save. The interval SHALL take effect during the current running session without restart. Cancel SHALL discard interval edits made since the last successful Apply. A missing, nonnumeric, fractional, or out-of-range stored interval SHALL resolve to 10 seconds without preventing other valid settings from loading.

#### Scenario: Valid interval is applied while tracking
- **WHEN** the user changes the interval from 1 to 5 seconds and successfully applies it
- **THEN** 5 seconds is persisted and the running session adopts that cadence without resetting the timer

#### Scenario: User cancels edits
- **WHEN** the user edits the interval and presses Cancel without applying that edit
- **THEN** the saved and active intervals remain at their last successfully applied values

#### Scenario: Settings save fails
- **WHEN** saving the edited interval fails
- **THEN** the active capture cadence remains at its previously applied value

#### Scenario: Older settings omit the interval
- **WHEN** a settings file has no activity polling interval
- **THEN** the interval defaults to 10 seconds and the other settings load normally

#### Scenario: Stored interval is invalid
- **WHEN** an otherwise valid settings file contains an invalid interval value
- **THEN** the interval falls back to 10 seconds while other valid settings remain available

### Requirement: Complete live appearance preview
Every Appearance control SHALL preview immediately, including presets, imported themes, colors, background choice, background tint, clock and button opacity, overall fade, widget scale, and compact mode. Preview SHALL remain separate from committed settings and SHALL NOT persist draft edits or activate nonappearance behavior. Clock and button opacity SHALL be applied once to their widget layers.

#### Scenario: User previews appearance and applies
- **WHEN** the user edits appearance controls and successfully presses Apply or OK
- **THEN** the persisted and active appearance matches the preview exactly, and those values become the new restore point

#### Scenario: User toggles compact mode from the widget during preview
- **WHEN** Settings is open and editable and the user activates the widget compact-mode toggle
- **THEN** the Settings checkbox and widget update from the same unsaved draft, Apply or OK commits that value, and Cancel restores the last successful commit

#### Scenario: User cancels after an earlier Apply
- **WHEN** Apply succeeds and the user makes further appearance edits before Cancel or title-bar close
- **THEN** every appearance value returns to that successful Apply, and unsaved edits on other tabs are discarded

#### Scenario: Appearance commit fails
- **WHEN** persistence or activation fails and compensation succeeds
- **THEN** committed settings and nonappearance behavior remain at the previous successful commit, while the retained draft remains previewed for correction or retry and Cancel still restores the previous commit

#### Scenario: User enters an invalid color
- **WHEN** an appearance color cannot be parsed
- **THEN** the last valid appearance remains displayed, Apply/OK shows a validation error without saving, and the user can correct the draft

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

Feedback SHALL sit left of OK/Apply/Cancel in the same footer row. Routine saving feedback SHALL not reserve a separate row above the actions. Long errors and retry actions SHALL wrap and scroll within a bounded feedback area, keeping actions reachable at the 500 by 400 minimum window size.

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

### Requirement: Status theme colors
Settings SHALL preserve Success, Warning, and Danger theme values through import/export and Apply/OK, and Cancel SHALL restore the last successful commit. Warning and Danger SHALL color warning and error notifications respectively. Warning SHALL continue to color existing summary warnings. Success SHALL remain reserved for future status displays. These saved theme values need not have individual editors in the current palette layout.

#### Scenario: User imports status colors
- **WHEN** the user imports a theme with Success, Warning, or Danger values
- **THEN** Apply/OK preserves those values and Cancel restores the last successful commit, while open warning/error notifications follow the previewed theme

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
- **THEN** the previous saved and nonappearance active values are restored, the retained appearance draft is previewed in the open window, and the commit is reported as failed

#### Scenario: Restoration fails
- **WHEN** a commit fails after replacement and restoration cannot complete
- **THEN** Settings shows a recovery-required error, preserves the prior-state recovery data, blocks another Apply or OK, and offers a recovery retry without claiming which values are active

#### Scenario: Recovery is still pending at startup
- **WHEN** the application starts with an unfinished settings recovery record
- **THEN** it attempts recovery before accepting or activating that candidate as committed settings and reports a recovery-required state if recovery still fails

#### Scenario: User retries after failure
- **WHEN** the user corrects the cause of an ordinary failed commit, or completes required recovery, and retries Apply or OK
- **THEN** the same draft can be committed without reopening Settings

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
