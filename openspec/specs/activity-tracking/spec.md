# Activity Tracking Specification

## Purpose
Defines how FocusTimer records what the user worked on while the timer runs, persists it to disk, and cleans it up over time.

## Requirements

### Requirement: Per-Application Time Segmentation
While the timer is Running and work logging is enabled, the system SHALL sample the foreground application/window at the configured activity polling interval, defaulting to ten seconds. When a sample detects a changed application or window title, the system SHALL close the current segment with an application-change reason and start a replacement segment with a distinct entry ID and the same running-session ID at the observation time. A window title change SHALL NOT close the segment when a segmentation rule applies (see the rule requirement below). Intervening time SHALL remain attributed to the last observed window; the system SHALL NOT reconstruct visits that occur entirely between samples.

#### Scenario: User switches active application
- **WHEN** a scheduled foreground sample detects a changed application while the timer is Running
- **THEN** the current segment closes with an application-change reason and a new segment starts for the observed application at the observation time with a distinct entry ID and the same session ID

#### Scenario: Window title changes within one application
- **WHEN** a scheduled sample detects a different window title within the same application
- **THEN** a new segment starts with that title even if the application name has not changed, unless a segmentation rule applies to both windows

#### Scenario: Existing configuration adopts the new default
- **WHEN** the saved configuration has no activity polling interval
- **THEN** scheduled foreground samples use the default ten-second interval

#### Scenario: Short visit falls between samples
- **WHEN** an application becomes foreground and loses foreground status entirely between two samples
- **THEN** no segment is invented for that unobserved visit and time remains attributed to the last observed window

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

### Requirement: Application exclusion during capture
While the timer is Running and work logging is enabled, the system SHALL NOT record activity whose sampled foreground
window matches an exclusion rule. When a sample observes an excluded window, the open segment SHALL close at the
observation time and no segment SHALL open. When a later sample observes a non-excluded window, a new segment SHALL
start at that observation time. Excluded time SHALL NOT be attributed to the preceding or following application.

#### Scenario: Switch to an excluded application
- **WHEN** a sample observes a window matching an exclusion rule while a segment is open
- **THEN** the segment closes at the observation time and nothing is recorded for the excluded window

#### Scenario: Return from an excluded application
- **WHEN** the next sample observes a non-excluded window after an excluded interval
- **THEN** a new segment starts at that observation time, and the excluded interval belongs to no segment

#### Scenario: Tracking starts on an excluded window
- **WHEN** tracking starts or resumes and the initial sample matches an exclusion rule
- **THEN** no segment opens until a non-excluded window is observed

#### Scenario: Excluded window across midnight
- **WHEN** an excluded window remains in front across local midnight
- **THEN** no entry is written for that time on either day

#### Scenario: First lookup fails while rules exist
- **WHEN** tracking starts, at least one exclusion rule is configured, and the first foreground lookup fails
- **THEN** no segment opens until a later lookup succeeds and shows a non-excluded window

#### Scenario: Lookup fails while excluded
- **WHEN** a foreground lookup fails after an excluded window was observed
- **THEN** no segment opens and the system remains in the excluded state

### Requirement: Exclusions do not alter existing data or the timer
Applying or editing exclusion rules SHALL NOT modify, delete, or hide entries already written. Excluded time SHALL
continue to count on the running timer and SHALL NOT change break-reminder timing.

#### Scenario: Rule added after work was recorded
- **WHEN** the user adds a rule matching an application that already has entries today
- **THEN** those entries remain unchanged and only subsequent samples are excluded

#### Scenario: Timer during an excluded interval
- **WHEN** an excluded window is in the foreground while the timer is Running
- **THEN** the timer display keeps counting and the worklog total does not increase for that time

### Requirement: Exclusion rule changes apply promptly
A changed exclusion list SHALL cause a foreground sample at the next one-second maintenance opportunity, without waiting
for the configured polling interval, and take effect at that sample. Applying it SHALL NOT
by itself close a segment, reset elapsed time, or start a session. Applying an identical list SHALL have no effect.

#### Scenario: Rule applied while the matching window is in front
- **WHEN** the user applies a rule matching the current foreground window
- **THEN** within one maintenance tick, even with a 60-second polling interval, the open segment closes and no new segment opens for that window

#### Scenario: Rule removed while its window is in front
- **WHEN** the user removes the rule for the current excluded window
- **THEN** a segment starts at the next maintenance tick that observes that window

### Requirement: Segmentation rules ignore title changes
The system SHALL NOT start a new segment when only the window title changes, provided the previous and the newly observed
window have the same process name and both match a segmentation rule. The segment SHALL keep the title observed when it
started. A change of process name, or a title change in a window that matches no segmentation rule, SHALL start a new
segment as usual. With no segmentation rules, segmentation SHALL behave as it did before the rules existed.

#### Scenario: Browser tab changes
- **WHEN** a rule matches the browser application and a sample observes a different title in the same browser
- **THEN** the open segment continues and keeps its original title

#### Scenario: Application changes
- **WHEN** a sample observes a different process name, even if both windows match segmentation rules
- **THEN** the open segment closes and a new one starts

#### Scenario: Title change in an unmatched application
- **WHEN** a title changes in an application no segmentation rule matches
- **THEN** a new segment starts with the new title

#### Scenario: Title-only rule
- **WHEN** a rule has only a title pattern and the title changes from one matching title to another within the same process
- **THEN** the open segment continues

#### Scenario: Excluded window
- **WHEN** a window matches both an exclusion rule and a segmentation rule
- **THEN** it is excluded and no segment is formed

### Requirement: Segmentation rule changes apply promptly and keep segments
A changed segmentation list SHALL take effect at the next foreground sample, using the same prompt-sample behavior as
exclusion changes. Applying it SHALL NOT close or merge existing segments. Applying an identical list SHALL have no effect.

#### Scenario: Rule added mid-session
- **WHEN** the user applies a rule matching the current application and the title then changes
- **THEN** no new segment starts for that title change, and segments written before the rule are unchanged
