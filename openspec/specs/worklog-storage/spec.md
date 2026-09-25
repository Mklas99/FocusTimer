# Worklog Storage Specification

## Purpose
Defines the durable worklog record, query, validation, and mutation behavior that reporting, correction, attribution, and export features can use independently of the active storage backend.

## Requirements

### Requirement: Current Worklog Record
The system SHALL persist each worklog entry with a schema version, entry ID, session ID, offset-aware start and end timestamps, derived duration, application name, window title, optional project tag, project-assignment source, optional project-rule ID, activity kind, end reason, capture source, source platform, optional source device ID, revision, and UTC last-modified timestamp.

#### Scenario: Entry round-trips through storage
- **WHEN** a valid current-schema entry is persisted and read again
- **THEN** every stored field retains its value and the duration equals the difference between end and start timestamps

### Requirement: Worklog Entry Invariants
The system SHALL reject an entry whose entry ID or session ID is empty, whose end timestamp is not later than its start timestamp, whose revision is less than one, or whose timestamps span different local calendar dates.

#### Scenario: Invalid interval is appended
- **WHEN** an entry has an end timestamp equal to or earlier than its start timestamp
- **THEN** the append fails with a validation result and no worklog file is modified

#### Scenario: Cross-day interval is appended
- **WHEN** an entry begins and ends on different local calendar dates
- **THEN** the append fails with a validation result because activity tracking must split the interval first

### Requirement: Idempotent Append
The system SHALL treat entry ID as the durable uniqueness key and SHALL not add a second row when an entry with the same ID and content is appended again.

#### Scenario: Identical append is retried
- **WHEN** the store receives an entry whose ID and stored content match an existing entry
- **THEN** the operation succeeds idempotently and the existing row remains unchanged

#### Scenario: Entry ID is reused with different content
- **WHEN** the store receives an entry whose ID exists with different stored content
- **THEN** the operation returns a conflict and does not modify the worklog

### Requirement: Explicit Worklog Query
The system SHALL support querying a half-open time interval using entry overlap semantics, with optional exact case-insensitive filters for application and project plus optional filters for session ID, activity kind, and capture source, and SHALL return results in a requested chronological order.

#### Scenario: Entry overlaps a query interval
- **WHEN** an entry starts before the exclusive query end and ends after the inclusive query start
- **THEN** the entry is included if it also satisfies every supplied filter

#### Scenario: Entry only touches the exclusive boundary
- **WHEN** an entry starts exactly at the query's exclusive end or ends exactly at its inclusive start
- **THEN** the entry is not included

### Requirement: Read Outcomes and Diagnostics
The system SHALL distinguish an empty result from unreadable, malformed, conflicting, or unsupported worklog data and SHALL return valid entries together with structured warnings when partial reads are safe.

#### Scenario: Requested daily file does not exist
- **WHEN** a query covers a date for which no worklog file exists
- **THEN** that date contributes an empty successful result without an error warning

#### Scenario: Current-schema file contains a malformed record
- **WHEN** a record cannot be parsed but record boundaries remain recoverable
- **THEN** valid records are returned with a structured warning identifying the affected file and record

### Requirement: Current-Schema CSV Safety
The system SHALL read current CSV data by header name, correctly handle commas, quotes, and record-spanning line breaks in quoted fields, and SHALL refuse to append to or rewrite a file whose header identifies an unsupported schema.

#### Scenario: Window title contains CSV control characters
- **WHEN** an entry contains commas, quotes, or line breaks in a textual field
- **THEN** persisting and reading the entry reproduces the original text without creating extra records

#### Scenario: Existing file uses an unsupported schema
- **WHEN** an append, update, or delete targets a daily file that is not in the current schema
- **THEN** the operation returns an unsupported-schema result and leaves the file byte-for-byte unchanged

### Requirement: Revision-Checked Same-Day Update
The system SHALL update a single entry by ID only when the expected revision matches, SHALL increment the revision and last-modified timestamp, and SHALL atomically replace the affected daily file after validating the complete replacement.

#### Scenario: Matching revision is updated
- **WHEN** a same-day patch targets an existing entry with its current revision
- **THEN** only that entry changes, its revision increments once, and readers observe either the complete old file or the complete new file

#### Scenario: Stale revision is updated
- **WHEN** a patch supplies an older revision than the stored entry
- **THEN** the operation returns a conflict and leaves the file unchanged

#### Scenario: Update moves entry to another local date
- **WHEN** a patch would move an entry outside its existing local calendar date
- **THEN** the operation is rejected without changing either day's worklog

### Requirement: Atomic Delete
The system SHALL delete a single entry by ID only when the expected revision matches and SHALL atomically replace the affected daily file after validating the complete replacement.

#### Scenario: Existing entry is deleted
- **WHEN** a delete targets an existing entry with its current revision
- **THEN** exactly that entry is absent after completion and readers observe either the complete old file or the complete new file

#### Scenario: Delete replacement fails
- **WHEN** the replacement file cannot be fully written, validated, or activated
- **THEN** the original daily worklog remains available and unchanged

### Requirement: Serialized File Mutation
The system SHALL serialize append, update, and delete operations affecting the same daily worklog and SHALL report a file-in-use or conflict result rather than risk partial or lost data.

#### Scenario: Append races with an update
- **WHEN** an append and update target the same daily file concurrently
- **THEN** both operations are serialized or one returns a typed conflict, and the resulting file remains valid with no lost successful change
