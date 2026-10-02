# Spec Delta

## ADDED Requirements

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

### Requirement: Exclusion rule changes apply from the next sample
A changed exclusion list SHALL take effect at the first foreground sample after it is applied. Applying it SHALL NOT
by itself close a segment, reset elapsed time, or start a session. Applying an identical list SHALL have no effect.

#### Scenario: Rule applied while the matching window is in front
- **WHEN** the user applies a rule matching the current foreground window
- **THEN** the open segment closes at the next sample and no new segment opens for that window

#### Scenario: Rule removed while its window is in front
- **WHEN** the user removes the rule for the current excluded window
- **THEN** a segment starts at the next sample that observes that window
