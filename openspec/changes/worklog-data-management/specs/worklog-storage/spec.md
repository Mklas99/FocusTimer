## ADDED Requirements

### Requirement: Manual Capture Source
The system SHALL support a `Manual` capture source for entries created by the user rather than by active-window tracking, SHALL persist and read it by a stable text value, and SHALL allow it in query filters.

#### Scenario: Manual entry round-trips
- **WHEN** a manual entry is appended and read again
- **THEN** its capture source is Manual and every other stored field retains its value

#### Scenario: Query by capture source
- **WHEN** a query filters on the Manual capture source
- **THEN** only manual entries are returned

#### Scenario: Existing files stay readable
- **WHEN** a day file contains only active-window entries
- **THEN** it is read, appended to, updated, and deleted from without change to its schema version

## MODIFIED Requirements

### Requirement: Revision-Checked Same-Day Update
The system SHALL update a single entry by ID only when the expected revision matches, SHALL increment the revision and last-modified timestamp, and SHALL atomically replace the affected daily file after validating the complete replacement. An entry belongs to the local day of its start; an update SHALL be rejected when it changes that day or places the new end on a different local date than the start, except that an entry already stored with an end exactly at the next local midnight remains valid and updatable.

#### Scenario: Matching revision is updated
- **WHEN** a same-day patch targets an existing entry with its current revision
- **THEN** only that entry changes, its revision increments once, and readers observe either the complete old file or the complete new file

#### Scenario: Stale revision is updated
- **WHEN** a patch supplies an older revision than the stored entry
- **THEN** the operation returns a conflict and leaves the file unchanged

#### Scenario: Update moves entry to another local date
- **WHEN** a patch would move an entry outside its existing local calendar date
- **THEN** the operation is rejected without changing either day's worklog

#### Scenario: Older entry ending at midnight is shortened
- **WHEN** a patch shortens a stored entry whose end is exactly the next local midnight so that it ends on its start date
- **THEN** the patch is accepted
