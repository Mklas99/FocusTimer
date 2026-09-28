# Settings delta

## MODIFIED Requirements

### Requirement: Hidden Developer Mode

The system SHALL unlock a Developer section, including a log-level picker and an activity polling interval control, when the user clicks the version label 7 times in the About tab, and SHALL persist the unlock via `DeveloperModeEnabled`.

#### Scenario: User clicks the version label 7 times
- **WHEN** the user clicks the version label 7 times in the About tab
- **THEN** the Developer section, including the log-level picker and activity polling interval control, becomes visible and stays unlocked across restarts

## ADDED Requirements

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
