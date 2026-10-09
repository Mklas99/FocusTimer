# Widget UI Specification

## Purpose
Defines the timer widget's visual modes and how the user resizes/adjusts its appearance.

## Requirements

### Requirement: Full Mode
The system SHALL provide a draggable, always-on-top full-mode window showing the timer, play/pause/reset controls, the project tag field, and a compact-mode toggle, consuming shared semantic component roles and accessible hit targets. Settings SHALL be accessible through the system tray menu, not a widget button.

#### Scenario: User drags the widget
- **WHEN** the user drags the full-mode widget
- **THEN** it moves with the cursor and remains always-on-top

#### Scenario: User interacts with primary timer controls in full mode
- **WHEN** the user activates the Start/Pause, Reset, or Toggle controls
- **THEN** the controls display shared semantic button states (hover, pressed, focus); at 1x the targets are 24x24 pixels, and user-selected widget scaling scales them with the clock

### Requirement: Compact Mode
The system SHALL provide a narrow-bar compact mode showing the same timer data, state brushes, and theme as full mode as a density-adjusted variant of the shared component system, toggleable from the widget or Settings, and SHALL persist the selected mode across restarts. Compact mode SHALL show Start/Pause and Expand controls; Reset remains available after expanding to full mode.

The shell tint and outline SHALL share the window bounds in both modes, with the borderless window following the scaled content height. The widget SHALL own a single outline without additional window-template chrome or extended native Windows decoration. Shell corners SHALL scale continuously with WidgetScale, using an 8 px base radius in compact mode and a 12 px base radius in full mode at 1x, including live appearance preview, without nested clipping that crops the outline or background. Compact buttons SHALL remain in a vertical column at every scale. Clock and icon side gaps SHALL be equal, accounting for the icon's click target.

Compact icon bounds SHALL meet vertically without extra button padding between them. Each button SHALL retain a separate click target whose height scales with the icon. Fixed minimum target rows SHALL NOT add extra top and bottom space around the clock at small scales.

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

### Requirement: Widget icon colors
Widget icons SHALL use Button Normal for their normal state, with an optional dedicated Play/Pause color for Start/Pause. Shared Hover, Pressed, and Disabled colors SHALL override the corresponding icon foreground in both modes, with Disabled taking precedence over Pressed and Pressed over Hover. Existing background highlights and focus indicators SHALL remain.

#### Scenario: User previews a widget icon color
- **WHEN** the user edits the normal Play/Pause color or a shared interaction-state color
- **THEN** the corresponding icon state updates immediately in both widget modes, Apply/OK saves it, and Cancel restores the last successful commit

### Requirement: Scale and Opacity
The system SHALL provide a `WidgetScale` setting that resizes fonts and controls responsively anchored to canonical design-system base dimensions, independent background, clock, and controls opacity settings, and an explicit overall opacity multiplier. Background opacity SHALL control only the widget shell tint and SHALL NOT change the clock, buttons, project field, or their interaction states.

#### Scenario: User adjusts widget scale
- **WHEN** the user changes `WidgetScale`
- **THEN** widget fonts and buttons scale responsively relative to defined design-system base metrics without requiring a restart

#### Scenario: User adjusts opacity
- **WHEN** the user changes background, clock, or controls opacity, or the overall multiplier
- **THEN** the corresponding widget element's transparency updates live, with the background slider affecting only the shell tint and the overall multiplier affecting the whole widget

#### Scenario: User adjusts background opacity
- **WHEN** the user moves the background opacity slider between 0% and 100%
- **THEN** only the widget shell tint changes live in full and compact modes, while the clock, buttons, project field, and their configured opacities remain unchanged

#### Scenario: User cancels an appearance preview
- **WHEN** the user previews colors, background choice, opacity, overall fade, scale, or compact mode in Settings and closes without applying
- **THEN** the widget restores the last successfully applied appearance

#### Scenario: User cancels a background preview
- **WHEN** the user changes background opacity in Settings and closes it without applying
- **THEN** the widget restores the last successfully applied shell tint opacity

#### Scenario: User removes the shell tint
- **WHEN** the user sets background tint opacity to 0% while Off is selected
- **THEN** the app adds no theme color tint behind the unchanged foreground content

#### Scenario: User adjusts foreground or overall opacity
- **WHEN** the user changes clock or controls opacity, or the explicit overall multiplier
- **THEN** only the corresponding foreground element changes for clock or controls opacity, while the overall multiplier fades the widget as a whole

### Requirement: Material Layering and Fallback
The timer widget SHALL offer Off and Solid background choices in Settings on one effective shell surface. Off SHALL request clear transparency and apply the selected tint opacity. Solid SHALL use a fully opaque theme-colored shell regardless of the saved tint opacity. Saved Soft, Strong, and Blur choices SHALL load as Off. The system SHALL provide a readable solid shell when transparency is unavailable or disabled.
OI-28 tracks a future verifiably see-through custom blur without desktop capture.

#### Scenario: Widget renders on supported platform
- **WHEN** the widget opens on a platform supporting the selected blur level
- **THEN** the widget renders one composited backdrop beneath sharp foreground content without stacked translucent shell layers

#### Scenario: User selects a blur choice
- **WHEN** the user changes between Off and Solid
- **THEN** the backdrop changes live in both widget modes without blurring the clock, buttons, or project field

#### Scenario: Material effects unavailable or disabled
- **WHEN** the operating system or environment does not support transparency effects or disables composition
- **THEN** the widget uses a readable solid or near-solid shell without moving its content or changing foreground opacity settings

#### Scenario: User cancels a blur preview
- **WHEN** the user changes blur in Settings and closes it without applying
- **THEN** the widget restores the last successfully applied blur choice

#### Scenario: User selects Off with no tint
- **WHEN** the user selects Off and sets background opacity to 0% on a transparency-capable system
- **THEN** the shell background is clear while the clock, buttons, and project field remain visible at their configured opacities

#### Scenario: User selects Solid with no saved tint
- **WHEN** the user selects Solid while background tint opacity is 0%
- **THEN** the shell uses the fully opaque theme background, the slider is unavailable, and the saved tint value is retained for Off

#### Scenario: High Contrast theme active
- **WHEN** the user selects the High Contrast theme
- **THEN** the widget renders an unblurred solid shell with high-contrast text and controls regardless of its stored blur or tint settings
