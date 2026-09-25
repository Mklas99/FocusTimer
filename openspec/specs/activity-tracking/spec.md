# Activity Tracking Specification

## Purpose
Defines how FocusTimer records what the user worked on while the timer runs, persists it to disk, and cleans it up over time.

## Requirements

### Requirement: Per-Application Time Segmentation
The system SHALL poll the foreground application/window once per second while the timer is Running, SHALL close the current log segment whenever the active application/window changes, and SHALL start a replacement segment with a new entry identity in the same running session.

#### Scenario: User switches active application
- **WHEN** the foreground window changes while the timer is Running
- **THEN** the current segment is closed with an application-change reason and a new segment starts with a distinct entry ID and the same session ID

### Requirement: Work-Logging On/Off Switch
The system SHALL provide a setting to disable work logging, and WHEN disabled SHALL stop tracking, discard in-flight/buffered entries, and stop growing "today" stats.

#### Scenario: User disables work logging
- **WHEN** the user turns off the work-logging setting
- **THEN** no new segments are tracked and the "today" total stops increasing until it is re-enabled

### Requirement: CSV Persistence
The system SHALL persist completed entries through the worklog store to a per-day CSV file at `worklogs/yyyy/MM/yyyy-MM-dd-worklog.csv`, SHALL flush buffered entries on pause/stop and periodically while running, and SHALL make repeated persistence of the same entry identity idempotent.

#### Scenario: Entry is flushed on pause
- **WHEN** the user pauses or stops the timer
- **THEN** any buffered entries for the current running session are written to the appropriate per-day CSV file

#### Scenario: A completed entry is submitted more than once
- **WHEN** persistence is retried with an entry ID that is already stored
- **THEN** the worklog contains one copy of that entry and no duplicate duration is introduced

#### Scenario: A worklog append temporarily fails
- **WHEN** completed entries cannot be appended because the worklog is temporarily unavailable
- **THEN** the application retains them in memory, informs the user once, and retries the same entry identities while it remains open

### Requirement: Data Retention Cleanup
The system SHALL delete worklog files older than the configured `DataRetentionDays` (default 90) once per day.

#### Scenario: Old worklog file exceeds retention
- **WHEN** a worklog file's date is older than `DataRetentionDays` relative to today
- **THEN** the file is deleted during the daily retention pass

### Requirement: Running Session Identity
The system SHALL assign one session ID to every uninterrupted interval in which the timer is Running and SHALL assign that ID to every segment created during the interval.

#### Scenario: User resumes after pausing
- **WHEN** the timer transitions from Paused to Running
- **THEN** subsequently created segments receive a new session ID that differs from the session completed by the pause

#### Scenario: Window changes during a running interval
- **WHEN** one or more application/window changes occur without pausing the timer
- **THEN** all resulting segments retain the same session ID

### Requirement: Tracked Entry Metadata
The system SHALL assign each new tracked segment a stable entry ID, offset-aware start and end timestamps, activity kind, end reason, capture source, source platform, source device identity, project-assignment source, revision, and last-modified timestamp before persistence.

#### Scenario: Active-window segment is completed normally
- **WHEN** an active-window segment is closed
- **THEN** it is persisted as active work captured from the active window with non-empty entry, session, platform, and device identity metadata

#### Scenario: Idle detection pauses tracking
- **WHEN** the timer is paused because the user crossed the idle threshold
- **THEN** the active segment is closed with an idle-pause end reason without classifying the preceding active interval as idle work

### Requirement: Local Day Boundary Segmentation
The system SHALL prevent a persisted entry from spanning more than one local calendar date by closing and restarting an otherwise unchanged segment at the local day boundary.

#### Scenario: Timer remains running across midnight
- **WHEN** an active segment reaches local midnight without an application/window change
- **THEN** the segment ending at midnight and the replacement segment have different entry IDs, the same session ID, and are persisted to their respective daily worklogs
