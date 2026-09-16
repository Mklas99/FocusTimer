# System Tray Specification

## Purpose
Defines the tray icon's state feedback and the quick-action menu it exposes.

## Requirements

### Requirement: State-Aware Icon and Tooltip
The system SHALL swap the tray icon art to reflect Running/Paused/Idle state, and SHALL show a tooltip with the current state and "Today: Xh Ym" total.

#### Scenario: Timer state changes
- **WHEN** the timer transitions between Running, Paused, and Idle
- **THEN** the tray icon updates to match the new state and the tooltip reflects it

### Requirement: Tray Menu Actions
The system SHALL provide a tray menu with Show/Hide, Start/Pause, Settings, and Exit, each driving the same behavior as the equivalent widget/hotkey action.

#### Scenario: User opens the tray menu
- **WHEN** the user left-clicks the tray icon
- **THEN** a menu with Show/Hide, Start/Pause, Settings, and Exit is shown
