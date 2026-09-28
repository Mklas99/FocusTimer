# Activity tracking delta

## MODIFIED Requirements

### Requirement: Per-Application Time Segmentation

While the timer is Running and work logging is enabled, the system SHALL sample the foreground application/window at the configured activity polling interval, defaulting to ten seconds. The system SHALL close the current segment and start a new segment when a sample detects a changed application or window title. Segment boundaries SHALL use observation time; intervening time SHALL remain attributed to the last observed window. The system SHALL NOT reconstruct visits that occur entirely between samples.

#### Scenario: User switches active application
- **WHEN** a scheduled foreground sample detects a changed application while the timer is Running
- **THEN** the current segment is closed and a new segment starts for the newly observed application at the observation time

#### Scenario: Window title changes within one application
- **WHEN** a scheduled sample detects a different window title within the same application
- **THEN** a new segment starts with that title even if the application name has not changed

#### Scenario: Existing configuration adopts the new default
- **WHEN** the saved configuration has no activity polling interval
- **THEN** scheduled foreground samples use the default ten-second interval

#### Scenario: Short visit falls between samples
- **WHEN** an application becomes foreground and loses foreground status entirely between two samples
- **THEN** no segment is invented for that unobserved visit and time remains attributed to the last observed window

## ADDED Requirements

### Requirement: Bounded capture scheduling

The system SHALL begin foreground capture immediately when tracking starts or resumes, or when logging is re-enabled during a running session, unless an earlier lookup is still in progress. In that case it SHALL begin capture for the latest active session at the first opportunity after that lookup finishes. The system SHALL allow at most one foreground lookup in progress and SHALL NOT accumulate periodic requests or issue catch-up bursts after delays. Lookup results from stopped, disabled, or superseded sessions SHALL NOT reopen or modify those sessions.

#### Scenario: Tracking begins before the first scheduled interval
- **WHEN** tracking starts with a 60-second interval and no lookup is in progress
- **THEN** the current foreground window is captured immediately without waiting 60 seconds

#### Scenario: Lookup is slower than the interval
- **WHEN** several timer updates occur while a foreground lookup is in progress
- **THEN** no overlapping lookup or queue of periodic lookups is created and subsequent capture resumes at the configured cadence after completion

#### Scenario: Stop and restart occur during a lookup
- **WHEN** an old lookup completes after tracking has stopped and restarted
- **THEN** its result is discarded and the latest session receives its initial capture without reopening the old session

#### Scenario: Logging is disabled during a lookup
- **WHEN** logging is disabled before a lookup finishes
- **THEN** the result produces no new segments and the existing disable behavior discards non-persisted tracking entries

### Requirement: Capture interval preserves session maintenance

The activity polling interval SHALL NOT change timer-display updates, break-reminder timing, or pause/stop/exit segment closure. Local-midnight splitting SHALL remain independent of foreground sampling cadence, with segments split at the exact local-day boundary on the next maintenance opportunity. Applying an interval change SHALL NOT by itself close a segment, reset elapsed time, or initiate a new session.

#### Scenario: Midnight falls between foreground samples
- **WHEN** an active session crosses local midnight before its next foreground sample
- **THEN** the next one-second maintenance opportunity splits the segment at midnight without requesting an extra foreground sample

#### Scenario: Pause occurs before the next foreground sample
- **WHEN** the user pauses with a 60-second interval before the next sample is due
- **THEN** the active segment closes at pause time and buffered entries follow the existing persistence path

#### Scenario: Interval changes during a session
- **WHEN** the user successfully applies a different activity polling interval
- **THEN** the next sample is rescheduled using that interval and the current session, elapsed time, and open segment remain intact

#### Scenario: Timer remains responsive at a longer interval
- **WHEN** activity polling is configured to 60 seconds
- **THEN** the timer display continues updating each second and break reminders retain their current timing
