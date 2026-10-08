## ADDED Requirements

### Requirement: Manual Entry Creation
The system SHALL let the user add an entry for a chosen local day with a start time, a duration, an optional window text, and an optional project, SHALL store it with the fixed application label "Manual entry", the `Manual` capture source, and a new entry ID and session ID, and SHALL apply the standard worklog entry invariants. Manual entries SHALL be allowed regardless of whether automatic work logging is enabled.

#### Scenario: Valid manual entry
- **WHEN** the user adds an entry for a past day at 14:00 with a duration of 45 minutes
- **THEN** the entry is stored from 14:00 to 14:45 on that day with application "Manual entry" and capture source Manual, and it appears in Entries, Timeline, and Summary

#### Scenario: Project assigned to manual entry
- **WHEN** the user enters a project
- **THEN** the entry stores that project with the editor assignment source; with no project, the entry is unassigned

#### Scenario: Window text is optional
- **WHEN** the user leaves the window text empty
- **THEN** the entry is stored with an empty window title

#### Scenario: Entry would leave the day
- **WHEN** the start time plus duration is later than 23:59:59 of the chosen day
- **THEN** the entry is rejected with a message explaining the day limit and nothing is stored

#### Scenario: Zero or missing duration
- **WHEN** the duration is zero, negative, or missing
- **THEN** the entry is rejected with a message and nothing is stored

#### Scenario: Future time
- **WHEN** the end of the entry is later than the current time
- **THEN** the entry is rejected with a message and nothing is stored

#### Scenario: Work logging is disabled
- **WHEN** automatic work logging is turned off and the user adds a valid manual entry
- **THEN** the entry is stored

#### Scenario: Day older than retention
- **WHEN** the chosen day is earlier than today minus (data retention days minus one) and retention is enabled
- **THEN** the entry is rejected with a message that the day is outside the retention window and nothing is stored

### Requirement: Duration Input
The system SHALL accept a duration written as hours and minutes (such as "2h 30m", "45m", or "1h") or as decimal hours (such as "2.5h"), SHALL work in whole minutes without seconds, and SHALL reject text it cannot read with a message that shows an accepted example.

#### Scenario: Hours and minutes
- **WHEN** the user enters "2h 30m"
- **THEN** the duration is 150 minutes

#### Scenario: Decimal hours
- **WHEN** the user enters "2.5h"
- **THEN** the duration is 150 minutes

#### Scenario: Unreadable text
- **WHEN** the user enters text that is not a duration
- **THEN** the entry is not saved and the message shows an accepted example

#### Scenario: Duration is elapsed time
- **WHEN** an entry spans a daylight-saving change
- **THEN** its duration is the elapsed time between start and end, not the difference of the wall-clock times

### Requirement: Local Time Resolution
The system SHALL turn the chosen local day and start time into an instant using the local time zone, SHALL reject a start time that does not exist because of a daylight-saving gap with a message, and SHALL use the earlier occurrence when a start time occurs twice.

#### Scenario: Nonexistent local time
- **WHEN** the user enters a start time that falls inside a spring-forward gap
- **THEN** the entry is rejected with a message and nothing is stored

#### Scenario: Ambiguous local time
- **WHEN** the user enters a start time that occurs twice because clocks go back
- **THEN** the earlier occurrence is used

### Requirement: Editable Entry Fields
The system SHALL allow editing only an entry's window title, project, and duration, SHALL keep its application, start time, entry ID, session ID, and capture source unchanged, and SHALL set the end time to the start time plus the new duration only when the duration was changed.

#### Scenario: Duration changed
- **WHEN** the user changes an entry's duration from 30 to 20 minutes
- **THEN** the start stays unchanged, the end moves 10 minutes earlier, and the revision increases by one

#### Scenario: Duration not changed
- **WHEN** the user edits only the window title or project of an entry whose duration includes seconds
- **THEN** the end time is exactly as stored and the duration is not rounded

#### Scenario: Project changed
- **WHEN** the user changes or clears an entry's project
- **THEN** the project and its assignment source (editor when set, unassigned when cleared) are stored and any project rule ID is cleared

#### Scenario: Project not changed
- **WHEN** the user edits only the window title or duration
- **THEN** the existing project, assignment source, and rule ID are preserved

#### Scenario: Edit would leave the day
- **WHEN** the new duration would put the end later than 23:59:59 of the entry's local day
- **THEN** the edit is rejected with a message and the entry is unchanged

#### Scenario: Tracked entry
- **WHEN** the user edits an entry that was captured from the active window
- **THEN** the edit is allowed and its capture source remains ActiveWindow

### Requirement: Project Input
The system SHALL accept any project text typed by the user, SHALL trim it and limit it to 100 characters, and SHALL offer a dropdown of the distinct, non-empty projects already used on the selected day and on today, without requiring the user to choose from it.

#### Scenario: Project chosen from the dropdown
- **WHEN** the user opens the project dropdown and selects an entry
- **THEN** that project text is used

#### Scenario: New project typed
- **WHEN** the user types a project that is not in the dropdown
- **THEN** the typed text is accepted and stored

#### Scenario: Surrounding whitespace
- **WHEN** the project text has leading or trailing spaces
- **THEN** the stored project has them removed

#### Scenario: Project too long
- **WHEN** the project text is longer than 100 characters
- **THEN** the entry is not saved and a message states the limit

### Requirement: Entry Deletion
The system SHALL delete an entry only after the user confirms, and SHALL delete exactly that entry.

#### Scenario: Delete confirmed
- **WHEN** the user confirms deleting an entry
- **THEN** the entry is removed from storage and from Entries, Timeline, and Summary

#### Scenario: Delete cancelled
- **WHEN** the user cancels the confirmation
- **THEN** no entry is changed

### Requirement: Dismissible Overlap Warning
The system SHALL save an added or edited entry even if it overlaps another persisted entry of the same day and SHALL show a warning that names the overlapping entries and that the user can dismiss by click or by keyboard, and the warning SHALL NOT block further actions.

#### Scenario: Added entry overlaps a tracked entry
- **WHEN** a manual entry overlaps a tracked entry
- **THEN** the manual entry is saved, a warning identifies the overlap, and the user can dismiss it by clicking it or by using its keyboard-reachable close control

#### Scenario: No overlap
- **WHEN** the saved entry overlaps nothing
- **THEN** no warning is shown

#### Scenario: Entries only touch
- **WHEN** an entry ends exactly when another begins
- **THEN** this is not an overlap

#### Scenario: Overlap with the running segment
- **WHEN** an entry overlaps only the currently running, not yet persisted segment
- **THEN** it is saved without a warning

### Requirement: Safe Concurrent Changes
The system SHALL submit each edit and delete with the revision the user saw, SHALL report a stale revision, a missing entry, a file in use, or an unsupported schema in a clear message, SHALL leave the stored entry unchanged in those cases, and SHALL reload the day after a conflict.

#### Scenario: Entry changed since it was loaded
- **WHEN** the stored revision is newer than the one the user edited
- **THEN** the edit is not applied, the user is told the entry changed, and the day is reloaded

#### Scenario: Entry persisted by tracking while the user edits today
- **WHEN** the tracker appends entries to today's file while the user saves an edit
- **THEN** both changes are kept or the edit reports a typed conflict, and the file stays valid

#### Scenario: Entry deleted elsewhere
- **WHEN** the entry no longer exists when the user saves or deletes
- **THEN** the user is told it no longer exists and the day is reloaded

### Requirement: Running Segment Is Not Editable
The system SHALL show and edit only persisted entries and SHALL NOT include or modify the currently running, not yet persisted segment.

#### Scenario: Timer is running
- **WHEN** the timer is running and the user opens Entries for today
- **THEN** only persisted entries are shown and the running segment is not editable

### Requirement: Tray Total Follows Worklog Changes
The system SHALL recompute the tray "Today" total and update the tray tooltip after any successful add, edit, or delete that affects today.

#### Scenario: Manual entry added for today
- **WHEN** the user adds a 30 minute manual entry for today
- **THEN** the tray tooltip's Today total increases by 30 minutes

#### Scenario: Entry deleted or shortened
- **WHEN** the user deletes or shortens an entry of today
- **THEN** the tray tooltip's Today total decreases accordingly

#### Scenario: Entry of another day changed
- **WHEN** the user changes an entry of a previous day
- **THEN** the tray total is unchanged
