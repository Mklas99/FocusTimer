## MODIFIED Requirements

### Requirement: Tray Menu Actions
The system SHALL provide a tray menu with Show/Hide, Start/Pause, Worklog, Settings, and Exit, in that order, each driving the same behavior as the equivalent widget/hotkey action, where Worklog opens the Worklog window.

#### Scenario: User opens the tray menu
- **WHEN** the user left-clicks the tray icon
- **THEN** a menu with Show/Hide, Start/Pause, Worklog, Settings, and Exit is shown, with Worklog directly above Settings

#### Scenario: User chooses Worklog
- **WHEN** the user chooses Worklog from the tray menu
- **THEN** the Worklog window opens
