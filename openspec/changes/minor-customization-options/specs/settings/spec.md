# Spec Delta

## MODIFIED Requirements

### Requirement: Hidden Developer Mode
The system SHALL toggle the Developer section, including a log-level picker and activity polling interval control, after each seven version-label clicks in About. The change SHALL belong to the Settings draft, persist through Apply/OK, and restore on Cancel/title-bar close. Hiding the section SHALL preserve its configured values. Clicks while the draft cannot be edited SHALL have no effect.

#### Scenario: User clicks the version label 7 times
- **WHEN** the user clicks the version label seven times with Developer mode disabled
- **THEN** the Developer section becomes visible and a successful Apply/OK keeps it visible across restarts

#### Scenario: Enable and disable Developer mode
- **WHEN** the user clicks the version label seven times and then seven more times
- **THEN** the Developer section becomes visible and then hidden without saving the draft

#### Scenario: Save or discard Developer mode
- **WHEN** the user changes Developer mode and applies or cancels
- **THEN** Apply/OK persists the choice across restart and Cancel restores the last successful commit

#### Scenario: Blocked version gesture
- **WHEN** the user clicks the version label while loading, committing, recovering, or after closure
- **THEN** Developer mode and the gesture's click count remain unchanged

## ADDED Requirements

### Requirement: Notification preferences draft
General settings SHALL provide information and warning visibility controls and whole-second dismissal durations by severity from 1 through 120. Missing fields SHALL default to enabled and 5/15/15 seconds. Apply/OK SHALL validate, save, and activate preferences; Cancel SHALL restore the last successful commit. A failed save SHALL retain the previously active policy.

#### Scenario: Notification preference commit
- **WHEN** the user edits notification preferences and successfully applies
- **THEN** newly opened notifications follow them without restart and saved preferences survive restart

#### Scenario: Invalid or uncommitted duration
- **WHEN** a duration is fractional or outside 1 through 120, or the draft is cancelled or fails to save
- **THEN** no invalid or uncommitted policy is activated
