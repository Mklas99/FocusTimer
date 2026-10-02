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
