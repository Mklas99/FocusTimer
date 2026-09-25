# Widget UI Specification

## Purpose
Defines the timer widget's visual modes and how the user resizes/adjusts its appearance.

## Requirements

### Requirement: Full Mode
The system SHALL provide a draggable, always-on-top full-mode window showing the timer, play/pause/reset controls, the project tag field, and settings/compact-toggle icon buttons, consuming shared semantic component roles and accessible hit targets.

#### Scenario: User drags the widget
- **WHEN** the user drags the full-mode widget
- **THEN** it moves with the cursor and remains always-on-top

#### Scenario: User interacts with primary timer controls in full mode
- **WHEN** the user activates the Start/Pause, Reset, or Toggle controls
- **THEN** the controls display shared semantic button states (hover, pressed, focus) and provide accessible hit targets of at least 24x24 pixels

### Requirement: Compact Mode
The system SHALL provide a narrow-bar compact mode showing the same timer data, state brushes, and theme as full mode as a density-adjusted variant of the shared component system, toggleable from the widget or Settings, and SHALL persist the selected mode across restarts.

#### Scenario: User toggles compact mode
- **WHEN** the user toggles compact mode from the widget or Settings
- **THEN** the widget switches display mode immediately, preserving identical semantic control roles (including Start/Pause styling) and the app remembers that mode on next launch

### Requirement: Scale and Opacity
The system SHALL provide a `WidgetScale` setting that resizes fonts and controls responsively anchored to canonical design-system base dimensions, and independent background, clock, and controls opacity settings plus an overall opacity multiplier.

#### Scenario: User adjusts widget scale
- **WHEN** the user changes `WidgetScale`
- **THEN** widget fonts and buttons scale responsively relative to defined design-system base metrics without requiring a restart

#### Scenario: User adjusts opacity
- **WHEN** the user changes background, clock, or controls opacity, or the overall multiplier
- **THEN** the corresponding widget element's transparency updates live

### Requirement: Material Layering and Fallback
The timer widget SHALL apply platform material backdrop effects using a deliberate fallback sequence (`Mica, AcrylicBlur, Blur, Transparent`) on a single composited surface and SHALL provide a solid or near-solid fallback surface ensuring high text legibility when transparency effects are unavailable or disabled.

#### Scenario: Widget renders on supported platform
- **WHEN** the widget opens on a platform supporting OS backdrop blur or Mica
- **THEN** the widget renders the single composited material backdrop without stacked translucent layers

#### Scenario: Material effects unavailable or disabled
- **WHEN** the operating system or environment does not support transparency effects or when composition is disabled
- **THEN** the widget falls back to a readable solid or near-solid background brush preserving full text contrast and control usability
