## MODIFIED Requirements

### Requirement: Capture interval preserves session maintenance
The activity polling interval SHALL NOT change timer-display updates, break-reminder timing, or pause/stop/exit segment closure. Local-midnight splitting SHALL remain independent of foreground sampling cadence, with segments split at the local-day boundary (closing at 23:59:59 and restarting at 00:00:00) on the next maintenance opportunity. Applying an interval change SHALL NOT by itself close a segment, reset elapsed time, or initiate a new session.

#### Scenario: Midnight falls between foreground samples
- **WHEN** an active session crosses local midnight before its next foreground sample
- **THEN** the next one-second maintenance opportunity splits the segment at the day boundary without requesting an extra foreground sample

#### Scenario: Pause occurs before the next foreground sample
- **WHEN** the user pauses with a 60-second interval before the next sample is due
- **THEN** the active segment closes at pause time and buffered entries follow the existing persistence path

#### Scenario: Interval changes during a session
- **WHEN** the user successfully applies a different activity polling interval
- **THEN** the next sample is rescheduled using that interval and the current session, elapsed time, and open segment remain intact

#### Scenario: Timer remains responsive at a longer interval
- **WHEN** activity polling is configured to 60 seconds
- **THEN** the timer display continues updating each second and break reminders retain their current timing

### Requirement: Local Day Boundary Segmentation
The system SHALL prevent a persisted entry from spanning more than one local calendar date by closing an otherwise unchanged segment at 23:59:59 of its local day and starting the replacement segment at 00:00:00 of the next local day, so every persisted entry starts and ends on the same local calendar date.

#### Scenario: Timer remains running across midnight
- **WHEN** an active segment reaches local midnight without an application/window change
- **THEN** the closing segment ends at 23:59:59 with the day-boundary end reason, the replacement segment starts at 00:00:00, they have different entry IDs and the same session ID, and each is persisted to its own daily worklog

#### Scenario: Entries written before this rule
- **WHEN** a stored entry ends exactly at the next local midnight
- **THEN** it stays readable and counted on its start day
