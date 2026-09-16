# Widget UI Specification

## Purpose
Defines the timer widget's visual modes and how the user resizes/adjusts its appearance.

## Requirements

### Requirement: Full Mode
The system SHALL provide a draggable, always-on-top full-mode window showing the timer, play/pause/reset controls, the project tag field, and settings/compact-toggle icon buttons.

#### Scenario: User drags the widget
- **WHEN** the user drags the full-mode widget
- **THEN** it moves with the cursor and remains always-on-top

### Requirement: Compact Mode
The system SHALL provide a narrow-bar compact mode showing the same data and theme as full mode, toggleable from the widget or Settings, and SHALL persist the selected mode across restarts.

#### Scenario: User toggles compact mode
- **WHEN** the user toggles compact mode from the widget or Settings
- **THEN** the widget switches display mode immediately and the app remembers that mode on next launch

### Requirement: Scale and Opacity
The system SHALL provide a `WidgetScale` setting that resizes fonts/buttons responsively, and independent background/clock/controls opacity settings plus an overall opacity multiplier.

#### Scenario: User adjusts widget scale
- **WHEN** the user changes `WidgetScale`
- **THEN** widget fonts and buttons resize accordingly without requiring a restart

#### Scenario: User adjusts opacity
- **WHEN** the user changes background, clock, or controls opacity, or the overall multiplier
- **THEN** the corresponding widget element's transparency updates live
