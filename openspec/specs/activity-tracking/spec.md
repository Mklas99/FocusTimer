# Activity Tracking Specification

## Purpose
Defines how FocusTimer records what the user worked on while the timer runs, persists it to disk, and cleans it up over time.

## Requirements

### Requirement: Per-Application Time Segmentation
The system SHALL poll the foreground application/window once per second while the timer is Running, and SHALL start a new log segment whenever the active application/window changes.

#### Scenario: User switches active application
- **WHEN** the foreground window changes while the timer is Running
- **THEN** the current segment is closed and a new segment starts for the newly active application

### Requirement: Work-Logging On/Off Switch
The system SHALL provide a setting to disable work logging, and WHEN disabled SHALL stop tracking, discard in-flight/buffered entries, and stop growing "today" stats.

#### Scenario: User disables work logging
- **WHEN** the user turns off the work-logging setting
- **THEN** no new segments are tracked and the "today" total stops increasing until it is re-enabled

### Requirement: CSV Persistence
The system SHALL append logged entries to a per-day CSV file at `worklogs/yyyy/MM/yyyy-MM-dd-worklog.csv`, and SHALL flush buffered entries on pause/stop and periodically while running.

#### Scenario: Entry is flushed on pause
- **WHEN** the user pauses or stops the timer
- **THEN** any buffered entries for the current session are written to that day's CSV file

### Requirement: Data Retention Cleanup
The system SHALL delete worklog files older than the configured `DataRetentionDays` (default 90) once per day.

#### Scenario: Old worklog file exceeds retention
- **WHEN** a worklog file's date is older than `DataRetentionDays` relative to today
- **THEN** the file is deleted during the daily retention pass
