# Settings Specification

## Purpose
Defines the Settings window's structure and its hidden developer-mode unlock.

## Requirements

### Requirement: Settings Tabs
The system SHALL provide a Settings window with General, Logging, Appearance, Hotkeys, and About tabs, covering auto-start, start-minimized, always-on-top, break reminders, worklog directory and retention, theme/opacity, hotkey display, and version/changelog/repo link.

#### Scenario: User opens Settings
- **WHEN** the user opens the Settings window
- **THEN** the General, Logging, Appearance, Hotkeys, and About tabs are available with their respective controls

### Requirement: Hidden Developer Mode
The system SHALL unlock a Developer section, including a log-level picker, when the user clicks the version label 7 times in the About tab, and SHALL persist the unlock via `DeveloperModeEnabled`.

#### Scenario: User clicks the version label 7 times
- **WHEN** the user clicks the version label 7 times in the About tab
- **THEN** the Developer section (including the log-level picker) becomes visible and stays unlocked across restarts
