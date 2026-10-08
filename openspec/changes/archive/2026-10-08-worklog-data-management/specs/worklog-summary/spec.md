## ADDED Requirements

### Requirement: Group By Window
The system SHALL offer a by-window grouping that counts each entry under its window title, case-insensitively and ignoring surrounding whitespace, SHALL label the group with the original text of the first entry, and SHALL count entries with an empty window title in one distinct "No window title" group that cannot collide with a window of that name.

#### Scenario: Same window, different case
- **WHEN** entries have the titles "Report.docx" and "report.docx "
- **THEN** they are counted in one group

#### Scenario: Entries without a window title
- **WHEN** an entry has an empty window title, such as a manual entry without window text
- **THEN** its time is shown in the "No window title" group

#### Scenario: Grouping selection
- **WHEN** the user switches the Summary to by window
- **THEN** rows, shares, and total are recomputed for the selected day

### Requirement: Summary Follows the Selected Day
The system SHALL summarize the day selected in its host window, SHALL keep the chosen grouping when the day changes, and SHALL label the range with the selected date (or "Today").

#### Scenario: Previous day selected
- **WHEN** the host selects a previous day
- **THEN** the Summary shows that day's totals and rows and an empty state if there are none

#### Scenario: Entry added for the selected day
- **WHEN** a manual entry is added for the selected day and the Summary reloads
- **THEN** the entry's duration is included in the total and in its group
