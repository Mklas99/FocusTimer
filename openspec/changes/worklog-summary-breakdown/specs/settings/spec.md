## MODIFIED Requirements

### Requirement: Settings Tabs
The system SHALL provide a Settings window with General, Logging, Summary, Appearance, Hotkeys, and About tabs, covering auto-start, start-minimized, always-on-top, break reminders, worklog directory and retention, today's time breakdown, theme/opacity, hotkey display, and version/changelog/repo link.

#### Scenario: User opens Settings
- **WHEN** the user opens the Settings window
- **THEN** the General, Logging, Summary, Appearance, Hotkeys, and About tabs are available with their respective controls

#### Scenario: User opens the Summary tab
- **WHEN** the user selects the Summary tab
- **THEN** today's breakdown is shown and refreshed
