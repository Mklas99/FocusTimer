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
