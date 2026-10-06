## MODIFIED Requirements

### Requirement: Settings Tabs
The system SHALL provide General, Logging, Appearance, Hotkeys, and About tabs in that order, using icon-and-underline navigation. They SHALL cover startup/window behavior, break reminders, logging and project rules, appearance, read-only hotkeys, and version/changelog/repository links with the existing developer unlock. Worklog reporting SHALL remain in the Worklog window rather than Settings.

#### Scenario: User opens Settings
- **WHEN** the user opens the Settings window
- **THEN** General, Logging, Appearance, Hotkeys, and About are available in that order with their respective controls and labeled icons

#### Scenario: User opens the Summary tab
- **WHEN** the user looks for today's time breakdown
- **THEN** reporting is available in the Worklog Summary tab and Settings does not contain a Summary tab

#### Scenario: User opens Hotkeys
- **WHEN** the user selects Hotkeys
- **THEN** the existing shortcut values remain read-only and the refresh does not introduce shortcut recording or editing

## ADDED Requirements

### Requirement: Settings visual organization
Settings SHALL retain every existing setting and its draft binding while adopting compact rows and conditional peer-group cards. Appearance SHALL separate theme tools, widget layout, widget opacity, and palette editing into named groups. Every field SHALL have a persistent visible label. Collapsing a group or changing tabs SHALL NOT discard its draft or conceal an unresolved validation error without an indication.

#### Scenario: User scans Appearance
- **WHEN** Appearance opens
- **THEN** theme actions, widget layout, widget opacity, and palette editing are identifiable without expanding every color group

#### Scenario: User fills a rule
- **WHEN** an application pattern, title pattern, or project name contains a value
- **THEN** a persistent visible label still identifies the field independently of its watermark

#### Scenario: User collapses an edited group
- **WHEN** a group containing draft edits or invalid values is collapsed
- **THEN** its edits remain in the shared draft and its header indicates any unresolved validation issue

### Requirement: Settings layout at supported sizes
Settings SHALL remain usable at its existing 500 by 400 logical-pixel minimum and default size, including supported display scaling. Content SHALL scroll vertically as needed while navigation and the OK, Apply, Cancel, and feedback footer remain reachable. Labels, opacity controls, rules, and validation messages SHALL wrap or reflow without overlapping, horizontal content clipping, or obscuring commit actions.

#### Scenario: User resizes to the minimum
- **WHEN** Settings is resized to 500 by 400 logical pixels
- **THEN** every setting can be reached through the content scroll area and navigation and commit actions remain usable

#### Scenario: Feedback appears
- **WHEN** saving, validation, failure, or recovery feedback appears
- **THEN** it occupies the reserved footer region without moving the action buttons or disabling the presentation of the complete draft

#### Scenario: User operates a long labeled control
- **WHEN** a long label such as overall widget fade or activity polling interval is displayed
- **THEN** the complete label remains readable beside or above its control and does not overlap that control

### Requirement: Blank rule rows
Rule lists (project rules, excluded applications, segmentation rules) SHALL treat a row whose fields are all empty as a notice rather than an error. Such a row SHALL be marked in the warning color with a message that it is removed when settings are applied, SHALL NOT block OK or Apply, SHALL be left out of the saved rules, and SHALL be removed from the list after a successful commit. A row with some but not all required fields SHALL be marked in the danger color with its validation message and SHALL block OK and Apply until completed or removed.

#### Scenario: User leaves a rule empty
- **WHEN** a rule row has no application pattern, window title pattern, or project name and the user chooses Apply or OK
- **THEN** the row is shown with the warning color beforehand, the commit succeeds without it, and the row is gone afterwards

#### Scenario: User half-fills a rule
- **WHEN** a project rule has a pattern but no project name
- **THEN** the row is shown with the danger color and its message, and Apply and OK do not commit until it is completed or removed
