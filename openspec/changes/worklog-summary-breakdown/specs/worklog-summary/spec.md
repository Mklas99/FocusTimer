## Purpose

Defines how tracked worklog entries are summarized for a time range into grouped totals, and how the Today breakdown is presented, so users can see where their time went and future reports can reuse the same results.

## ADDED Requirements

### Requirement: Summary of a Time Range
The system SHALL summarize the worklog entries that overlap a requested half-open time range into rows of accumulated duration, each with its share of the total and its entry count, ordered from the largest duration to the smallest, together with the total duration of all rows.

#### Scenario: Entries are grouped by application
- **WHEN** a summary by application is requested for a day that contains entries for two applications
- **THEN** one row per application is returned, the row durations sum to the total, and the row with the longest duration is first

#### Scenario: Shares add up to the whole
- **WHEN** a summary contains rows with a non-zero total
- **THEN** each row's share is its duration divided by the total, and the shares of all rows sum to one within rounding

#### Scenario: Range contains no entries
- **WHEN** a summary is requested for a range whose worklog was read successfully and contains no entries
- **THEN** the result is successful with no rows and a zero total

### Requirement: Range Clipping
The system SHALL count only the part of an entry that lies inside the requested range, so an entry that starts before or ends after the range contributes only its overlapping duration.

#### Scenario: Entry crosses the start of the range
- **WHEN** an entry begins before the range start and ends inside the range
- **THEN** only the time from the range start to the entry end is counted

#### Scenario: Today total matches the summary
- **WHEN** the Today summary is produced for the local day
- **THEN** its total equals the Today total shown by the tray tooltip for the same worklog

### Requirement: Grouping Selection
The system SHALL support grouping a summary by application and by project, SHALL let the caller choose the grouping for each request, and SHALL treat the set of available groupings as extendable without changing how ranges, totals, shares, or diagnostics are computed.

#### Scenario: Same entries grouped by project
- **WHEN** the same range is summarized by project instead of by application
- **THEN** the total is identical and the rows are one per project

#### Scenario: Application name differs only by letter case
- **WHEN** entries for the same application are recorded with names that differ only by letter case
- **THEN** they are counted in a single application row

### Requirement: Unassigned Project Bucket
The system SHALL report entries that have no project as a single, clearly marked "Unassigned" row when grouping by project, and SHALL NOT drop them from the total.

#### Scenario: Some entries have no project
- **WHEN** a by-project summary includes entries with an empty or missing project tag
- **THEN** their combined duration appears in one Unassigned row that is distinguishable from any project actually named "Unassigned"

#### Scenario: Project name differs only by letter case or spacing at the ends
- **WHEN** entries carry the same project name with different letter case or surrounding spaces
- **THEN** they are counted in a single project row

### Requirement: Optional Filters
The system SHALL accept optional filters for application and project on a summary request, SHALL include only matching entries in rows, total, and shares, and SHALL behave as if no filter were supplied when a filter is omitted.

#### Scenario: Application filter is supplied
- **WHEN** a summary is requested with an application filter
- **THEN** only entries of that application contribute to the rows and the total

### Requirement: Visible Read Diagnostics
The system SHALL distinguish an empty worklog from an unreadable one, SHALL report a failed read as a failure and not as a zero total, and SHALL carry warnings for partly unreadable data on the summary result.

#### Scenario: Worklog cannot be read
- **WHEN** the worklog for the requested range cannot be read
- **THEN** the summary reports a failure with a message and does not present a zero total as if it were real data

#### Scenario: Some records are unreadable
- **WHEN** valid entries are returned together with warnings about unreadable records
- **THEN** the summary includes the valid entries and exposes the warnings so they can be shown to the user

### Requirement: Today Breakdown View
The system SHALL show the user the breakdown for the current local day, with the total, one row per group showing its label, duration, and share, a way to switch between application and project grouping, and a way to refresh, and SHALL show distinct states for loading, no data, unreadable data, and partial data warnings.

#### Scenario: User opens the breakdown
- **WHEN** the user opens the breakdown view
- **THEN** it shows today's total and rows grouped by application

#### Scenario: User switches grouping
- **WHEN** the user switches from application to project grouping
- **THEN** the rows are recalculated for the same day and the total stays the same

#### Scenario: No time is logged today
- **WHEN** nothing has been tracked today
- **THEN** the view shows a "no data" message and not a table of zero rows

#### Scenario: Worklog is unreadable
- **WHEN** the summary reports a read failure
- **THEN** the view shows an error message and no total

#### Scenario: Data changes while the view is open
- **WHEN** new entries are persisted while the breakdown is visible
- **THEN** the user can refresh to see them, and the view also refreshes when it is opened

### Requirement: Presentation Independent of Its Host Window
The breakdown view SHALL work the same regardless of which window hosts it, so it can be shown in the Settings window now and in a separate report window later without behavior changes.

#### Scenario: View is hosted in a different window
- **WHEN** the breakdown view is placed in a window other than Settings
- **THEN** it loads, refreshes, and switches grouping without requiring anything from the Settings window
