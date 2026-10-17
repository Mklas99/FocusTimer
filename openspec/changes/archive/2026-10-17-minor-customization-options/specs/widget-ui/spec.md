# Spec Delta

## ADDED Requirements

### Requirement: Widget control placement
The system SHALL let the user place widget controls beside or below the clock in both modes, defaulting to beside. Placement SHALL preview immediately, scale with the widget, persist through Apply/OK, and restore on Cancel or title-bar close. Full-mode controls SHALL remain a horizontal row; compact controls SHALL stack vertically beside the clock and horizontally below it. The project field SHALL remain below full-mode clock and controls.

#### Scenario: Preview and discard placement
- **WHEN** the user selects below-clock placement and closes Settings without applying
- **THEN** both modes preview the new placement and restore the last committed placement on close

#### Scenario: Persist and scale placement
- **WHEN** the user applies below-clock placement and scales either widget mode
- **THEN** the placement survives restart and clock, controls, and hit targets scale proportionally

## MODIFIED Requirements

### Requirement: Compact Mode
The system SHALL provide a narrow-bar compact mode showing the same timer data, state brushes, and theme as full mode as a density-adjusted variant of the shared component system, toggleable from the widget or Settings, and SHALL persist the selected mode across restarts. Compact mode SHALL show Start/Pause and Expand controls; Reset remains available after expanding to full mode.

The shell tint and outline SHALL share the window bounds in both modes, with the borderless window following the scaled content height. The widget SHALL own a single outline without additional window-template chrome or extended native Windows decoration. Shell corners SHALL scale continuously with WidgetScale, using an 8 px base radius in compact mode and a 12 px base radius in full mode at 1x, including live appearance preview, without nested clipping that crops the outline or background. Compact buttons SHALL remain in a vertical column beside the clock and a horizontal row below it at every scale. Clock and icon side gaps SHALL be equal, accounting for the icon's click target.

With controls beside the clock, compact icon bounds SHALL meet vertically without extra button padding between them. Each button SHALL retain a separate click target whose height scales with the icon. Fixed minimum target rows SHALL NOT add extra top and bottom space around the clock at small scales. With controls below the clock, clock and controls SHALL be horizontally centered with equal outer side gaps.

Full-mode button margins and widget content insets SHALL scale with WidgetScale. The compact clock-to-icon gap and button side padding SHALL scale with the icon size. Compact clock and icon outer gaps SHALL remain equal, with the entire target inside the window. User-selected WidgetScale SHALL scale both button areas and clock dimensions proportionally in both modes, including below 1x. The full-mode clock-to-controls width ratio SHALL remain constant; the compact clock's vertical breathing room SHALL remain proportional without a fixed button-height floor.

#### Scenario: User reduces compact widget scale
- **WHEN** the user previews compact mode at 0.5 or 0.75 scale
- **THEN** the shell covers the window, its corner radius scales with the content, and its tint and outline remain aligned

#### Scenario: User toggles compact mode with Settings closed
- **WHEN** the user toggles compact mode from the widget with Settings closed
- **THEN** the widget switches display mode immediately, preserving identical semantic control roles (including Start/Pause styling) and the app remembers that mode on next launch

#### Scenario: User toggles compact mode with Settings open
- **WHEN** the user toggles compact mode from the widget or Settings while the Settings draft is editable
- **THEN** the widget and Settings checkbox immediately reflect the same draft value without saving; Apply or OK persists it, and Cancel or title-bar close restores the last successful commit

#### Scenario: User toggles compact mode while Settings is blocked
- **WHEN** the user activates the widget compact-mode toggle during a Settings commit or required recovery
- **THEN** neither the draft nor committed mode changes and no separate save occurs


