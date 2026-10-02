## ADDED Requirements

### Requirement: Worklog Window From the Tray
The system SHALL provide a Worklog window opened by a "Worklog..." tray menu entry, SHALL keep at most one instance open, and SHALL bring the existing instance to the front when the entry is chosen again.

#### Scenario: User opens the Worklog window
- **WHEN** the user chooses "Worklog..." from the tray menu
- **THEN** the Worklog window is shown and activated with the Entries tab selected and Today as the selected day

#### Scenario: Worklog window is already open
- **WHEN** the user chooses "Worklog..." while the window is open
- **THEN** no second window is created and the existing window is activated

#### Scenario: Application exits with the window open
- **WHEN** the application exits while the Worklog window is open
- **THEN** the window closes with the application

### Requirement: Worklog Window Tabs
The system SHALL provide Entries, Timeline, and Summary tabs in the Worklog window, and the window SHALL NOT depend on the Settings window or its draft.

#### Scenario: Tabs are available
- **WHEN** the Worklog window is open
- **THEN** the Entries, Timeline, and Summary tabs are available

#### Scenario: Settings window is not required
- **WHEN** the Worklog window is open and the Settings window is closed
- **THEN** all Worklog tabs work and Settings edits are neither required nor affected

### Requirement: Single-Day Selection
The system SHALL show exactly one local calendar day at a time across all Worklog tabs, SHALL default to the current local day, and SHALL let the user step to the previous or next day, jump back to Today, and pick a date.

#### Scenario: Day changes
- **WHEN** the user selects a previous day
- **THEN** the Entries, Timeline, and Summary tabs all show that day, and the Summary keeps its selected grouping

#### Scenario: Future day
- **WHEN** the user tries to select a day after today
- **THEN** the selection is not changed

#### Scenario: Day has no entries
- **WHEN** the selected day has no worklog file or no entries
- **THEN** each tab shows an explicit empty state, not an error

### Requirement: Entries Table
The system SHALL list each stored entry of the selected day in start-time order with start, end, duration, application, window title, project, and capture source, and SHALL make entry ID, revision, and last-modified time available as details of a selected row.

#### Scenario: Entries are listed
- **WHEN** the selected day has stored entries
- **THEN** each entry appears once with its stored values and a duration equal to end minus start

#### Scenario: Entry fields contain special characters
- **WHEN** a window title contains commas, quotes, or line breaks
- **THEN** the table shows the original text without splitting the entry

#### Scenario: Read warnings
- **WHEN** a record of the selected day cannot be read
- **THEN** the valid entries are shown together with a visible warning that identifies how many records are not included

#### Scenario: Read failure
- **WHEN** the day's file cannot be read or has an unsupported schema
- **THEN** an error message is shown and the table is not presented as empty

### Requirement: Refresh Behavior
The system SHALL reload the selected day when the user selects a tab, presses Refresh, changes the day, or after a successful add, edit, or delete, and a newer reload SHALL supersede an older one.

#### Scenario: Tab selected
- **WHEN** the user switches to the Summary or Entries tab
- **THEN** its content is reloaded for the selected day

#### Scenario: Entry persisted while the window is open
- **WHEN** the tracker persists a new entry for the selected day and the user presses Refresh
- **THEN** the entry appears in Entries and in the Summary totals

#### Scenario: Slow reload superseded
- **WHEN** the user changes the day while a previous reload is still running
- **THEN** only the result for the latest selected day is shown

### Requirement: Timeline View
The system SHALL show the selected day's entries in chronological order along a time axis, SHALL distinguish manual entries from tracked entries without relying on color alone, SHALL show gaps between entries as empty space, and SHALL show the same read warnings as the Entries tab.

#### Scenario: Entries in time order
- **WHEN** a day has several entries
- **THEN** they are shown in start-time order with their time span, application, and window title

#### Scenario: Manual entry in the timeline
- **WHEN** a day contains a manual entry
- **THEN** it is visibly marked as manual by a label or shape in addition to any color

#### Scenario: Overlapping entries
- **WHEN** two entries overlap
- **THEN** both remain visible and are not merged or hidden

#### Scenario: Empty day
- **WHEN** the day has no entries
- **THEN** an empty-state message is shown
