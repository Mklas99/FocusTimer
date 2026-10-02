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
The system SHALL show exactly one local calendar day at a time across all Worklog tabs, SHALL default to the current local day, and SHALL let the user step to the previous or next day, jump back to Today, and pick a date. The selectable days SHALL run from today minus (data retention days minus one) up to today, and SHALL be unlimited into the past when retention is turned off.

#### Scenario: Day changes
- **WHEN** the user selects a previous day
- **THEN** the Entries, Timeline, and Summary tabs all show that day, and the Summary keeps its selected grouping

#### Scenario: Future day
- **WHEN** the user tries to select a day after today
- **THEN** the selection is not changed

#### Scenario: Day outside the retention window
- **WHEN** the user tries to select a day earlier than the retention window allows
- **THEN** the selection is not changed and the earliest available day is indicated

#### Scenario: Window open past midnight
- **WHEN** the window stays open across local midnight
- **THEN** the selected day does not change by itself, and the Today control selects the new current day

#### Scenario: Day has no entries
- **WHEN** the selected day has no worklog file or no entries
- **THEN** each tab shows an explicit empty state, not an error

### Requirement: Entries Table
The system SHALL list each stored entry of the selected day in start-time order with start, end, duration, application, window title, and project, SHALL show start and end to the minute and the duration in hours and minutes (rounded to the nearest minute, "<1m" under one minute), and SHALL make the exact start, end, and duration to the second, the capture source, entry ID, session ID, revision, last-modified time, end reason, project source, platform, and device available as details of a selected row. The table SHALL NOT have a separate capture source column; manual entries are recognizable by their application "Manual entry".

#### Scenario: Entries are listed
- **WHEN** the selected day has stored entries
- **THEN** each entry appears once with its stored values, with times to the minute and a duration that agrees with them

#### Scenario: Times without seconds
- **WHEN** an entry runs from 10:00:07 to 11:30:00
- **THEN** the table shows 10:00, 11:30, and 1h 30m, and the details of the selected row show 10:00:07 to 11:30:00 with its exact duration

#### Scenario: Very short entry
- **WHEN** an entry lasts less than one minute
- **THEN** its duration is shown as "<1m"

#### Scenario: Manual entry in the table
- **WHEN** the day contains a manual entry
- **THEN** its application column reads "Manual entry" and the details of the selected row name the source as Manual

#### Scenario: Entry fields contain special characters
- **WHEN** a window title contains commas, quotes, or line breaks
- **THEN** the table shows the original text without splitting the entry

#### Scenario: Read warnings
- **WHEN** a record of the selected day cannot be read
- **THEN** the valid entries are shown together with a visible warning that identifies how many records are not included

#### Scenario: Read failure
- **WHEN** the day's file cannot be read or has an unsupported schema
- **THEN** an error message is shown and the table is not presented as empty

### Requirement: Entries Search
The system SHALL provide a search field next to the Entries action buttons that keeps only the entries in which every typed word appears, ignoring case, in the application, window title, project, source, start, end, or duration, SHALL show how many of the day's entries match while a search is active, SHALL keep the search when the day is reloaded, and SHALL let the user clear it with a button, with Escape, and focus it with Ctrl + F from anywhere in the Worklog window.

#### Scenario: Words must all match
- **WHEN** the user types "alpha plan"
- **THEN** only entries that contain both "alpha" and "plan" in any of the searched columns are listed

#### Scenario: Result count
- **WHEN** a search keeps 2 of 5 entries
- **THEN** the text "2 of 5 entries" is shown beside the search field

#### Scenario: Nothing matches
- **WHEN** no entry matches the search
- **THEN** the table is replaced by the message that no entries match, not by the empty-day message, and clearing the search restores the table

#### Scenario: Selection hidden by the search
- **WHEN** the selected entry no longer matches
- **THEN** the selection is cleared

#### Scenario: Matches are highlighted
- **WHEN** a search is active
- **THEN** the parts of the start, end, duration, application, window, and project cells that match a typed word are bold and underlined (not only colored), also on the selected row and in every theme, and clearing the search shows plain text again

#### Scenario: Search does not change other tabs
- **WHEN** a search is active
- **THEN** the Timeline and Summary still show every entry of the day

#### Scenario: Keyboard
- **WHEN** the user presses Ctrl + F on any tab of the Worklog window
- **THEN** the Entries tab opens and the search field has focus with its text selected

### Requirement: Refresh Behavior
The system SHALL reload the selected day when the user selects a tab, presses Refresh, changes the day, activates the window (unless an add or edit dialog is open), or after a successful add, edit, or delete, and a newer reload SHALL supersede an older one.

#### Scenario: Tab selected
- **WHEN** the user switches to the Summary or Entries tab
- **THEN** its content is reloaded for the selected day

#### Scenario: Entry persisted while the window is open
- **WHEN** the tracker persists a new entry for the selected day and the user presses Refresh
- **THEN** the entry appears in Entries and in the Summary totals

#### Scenario: Window activated
- **WHEN** the user returns to the Worklog window from another window and no add or edit dialog is open
- **THEN** the selected day is reloaded

#### Scenario: Slow reload superseded
- **WHEN** the user changes the day while a previous reload is still running
- **THEN** only the result for the latest selected day is shown

### Requirement: Timeline Grouping
The system SHALL let the user switch the timeline between "All entries together" and a split into one column per group using the same groupings as the Summary (by application, by project, by window), SHALL show a header above each column with the group's name, total time, and entry count, SHALL order the columns by total time, largest first, with the group of entries without a value last, SHALL draw overlapping entries side by side only inside their own column, SHALL keep zoom, the selected day, and the full day's entries (a search does not remove blocks) in the grouped view, SHALL scroll sideways when the columns do not fit while the hour labels and the column headers stay in place, and SHALL remember the chosen grouping between runs together with the zoom.

#### Scenario: Switch to grouped
- **WHEN** the user chooses "By application" in the Group by list
- **THEN** the timeline shows one column per application with a header naming the application, its total time, and its number of entries, and every entry's block sits in its application's column

#### Scenario: Column order
- **WHEN** the day has entries of applications with different totals
- **THEN** the columns run from the largest total to the smallest, equal totals are ordered by name, and the column for entries without a value (such as no project) is last

#### Scenario: Overlaps in different columns
- **WHEN** entries of two different groups happen at the same time
- **THEN** they are drawn in their own columns at full column width and are not split into lanes against each other

#### Scenario: Overlaps in the same column
- **WHEN** two entries of one group overlap in time
- **THEN** they share that column's width side by side

#### Scenario: Many groups
- **WHEN** there are more groups than fit at the narrowest column width
- **THEN** the timeline scrolls sideways, the column headers scroll with the columns, and the hour labels stay at the left

#### Scenario: Switch back
- **WHEN** the user chooses "All entries together"
- **THEN** the timeline is one column without headers again

#### Scenario: Day, zoom, and reload keep the grouping
- **WHEN** the user changes the day, zooms, or the day is reloaded
- **THEN** the timeline stays grouped by the same grouping and the columns are recomputed for the new data

#### Scenario: Grouping is remembered
- **WHEN** the user chose a grouping and opens the Worklog window again, even after restarting the application
- **THEN** the timeline starts with that grouping, and an unknown or missing remembered grouping means "All entries together"

### Requirement: Timeline Zoom
The system SHALL let the user change the vertical scale of the timeline so an hour has more or less height, with Ctrl + mouse wheel (up zooms in), with Ctrl + plus, Ctrl + minus, and Ctrl + 0, and with zoom out, zoom in, and reset buttons that show the current zoom as a percentage of the default, SHALL limit the scale to a minimum and a maximum, SHALL keep the moment under the mouse pointer (or the middle of the visible area for buttons and keys) in place while zooming, and SHALL redraw overlapping entries at the new scale.

#### Scenario: Zoom in with the wheel
- **WHEN** the user turns the mouse wheel up while holding Ctrl over the timeline
- **THEN** every hour becomes taller, the zoom text increases, and the moment under the pointer stays under the pointer

#### Scenario: Plain wheel scrolls
- **WHEN** the user turns the mouse wheel without Ctrl
- **THEN** the timeline scrolls and the scale does not change

#### Scenario: Limits
- **WHEN** the user keeps zooming in or out
- **THEN** the scale stops at the maximum or minimum and the matching button is disabled

#### Scenario: Reset
- **WHEN** the user presses Reset or Ctrl + 0
- **THEN** the scale returns to the default of 100%

#### Scenario: Zoom is remembered
- **WHEN** the user changes the zoom and later opens the Worklog window again, even after restarting the application
- **THEN** the timeline starts at the zoom the user last chose, clamped to the allowed range, and a missing or damaged remembered value means 100%

#### Scenario: Remembering never blocks
- **WHEN** the remembered zoom cannot be read or written
- **THEN** the timeline still works at the current zoom and nothing else is affected

#### Scenario: Short entries at a larger scale
- **WHEN** two very short neighbouring entries shared lanes at the default scale and the user zooms in enough to separate them
- **THEN** they are drawn in one lane at full width

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
