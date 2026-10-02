## ADDED Requirements

### Requirement: Manual Entry Creation
The system SHALL let the user add an entry for a chosen local day with a start time, a duration, an optional window text, and an optional project, SHALL store it with the fixed application label "Manual entry", the `Manual` capture source, and a new entry ID and session ID, and SHALL apply the standard worklog entry invariants.

#### Scenario: Valid manual entry
- **WHEN** the user adds an entry for a past day at 14:00 with a duration of 45 minutes
- **THEN** the entry is stored from 14:00 to 14:45 on that day with application "Manual entry" and capture source Manual, and it appears in Entries, Timeline, and Summary

#### Scenario: Project assigned to manual entry
- **WHEN** the user enters a project
- **THEN** the entry stores that project with the editor assignment source; with no project, the entry is unassigned

#### Scenario: Window text is optional
- **WHEN** the user leaves the window text empty
- **THEN** the entry is stored with an empty window title

#### Scenario: Entry would cross midnight
- **WHEN** the start time plus duration is later than the end of the chosen day
- **THEN** the entry is rejected with a message explaining the day limit and nothing is stored

#### Scenario: Zero or missing duration
- **WHEN** the duration is zero, negative, or missing
- **THEN** the entry is rejected with a message and nothing is stored

#### Scenario: Future time
- **WHEN** the start time or the end of the entry is later than the current time
- **THEN** the entry is rejected with a message and nothing is stored

### Requirement: Editable Entry Fields
The system SHALL allow editing only an entry's window title, project, and duration, SHALL keep its application, start time, entry ID, session ID, and capture source unchanged, and SHALL set the end time to the start time plus the new duration.

#### Scenario: Duration changed
- **WHEN** the user changes an entry's duration from 30 to 20 minutes
- **THEN** the start stays unchanged, the end moves 10 minutes earlier, and the revision increases by one

#### Scenario: Project changed
- **WHEN** the user changes or clears an entry's project
- **THEN** the project and its assignment source (editor when set, unassigned when cleared) are stored and any project rule ID is cleared

#### Scenario: Project not changed
- **WHEN** the user edits only the window title or duration
- **THEN** the existing project, assignment source, and rule ID are preserved

#### Scenario: Edit would leave the day
- **WHEN** the new duration would move the end past the end of the entry's local day
- **THEN** the edit is rejected with a message and the entry is unchanged

#### Scenario: Tracked entry
- **WHEN** the user edits an entry that was captured from the active window
- **THEN** the edit is allowed and its capture source remains ActiveWindow

### Requirement: Entry Deletion
The system SHALL delete an entry only after the user confirms, and SHALL delete exactly that entry.

#### Scenario: Delete confirmed
- **WHEN** the user confirms deleting an entry
- **THEN** the entry is removed from storage and from Entries, Timeline, and Summary

#### Scenario: Delete cancelled
- **WHEN** the user cancels the confirmation
- **THEN** no entry is changed

### Requirement: Dismissible Overlap Warning
The system SHALL save an added or edited entry even if it overlaps another entry of the same day and SHALL show a warning that names the overlapping entries and that the user can dismiss, and the warning SHALL NOT block further actions.

#### Scenario: Added entry overlaps a tracked entry
- **WHEN** a manual entry overlaps a tracked entry
- **THEN** the manual entry is saved, a warning identifies the overlap, and the user can dismiss it by clicking it or its close control

#### Scenario: No overlap
- **WHEN** the saved entry overlaps nothing
- **THEN** no warning is shown

#### Scenario: Entries only touch
- **WHEN** an entry ends exactly when another begins
- **THEN** this is not an overlap

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
