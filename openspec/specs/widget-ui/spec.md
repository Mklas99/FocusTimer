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
The timer widget SHALL offer Off, Solid, and Frosted background choices in Settings on one effective shell surface. Off SHALL request clear transparency and apply the selected tint opacity. Solid SHALL use a fully opaque theme-colored shell regardless of the saved tint opacity. Saved Soft, Strong, and Blur choices SHALL load as Off. Frosted SHALL blur live desktop content behind sharp foreground content, with independent shell tint. The system SHALL provide a readable solid shell when the selected material is unavailable or disabled.
The rendering path SHALL NOT capture desktop pixels.

#### Scenario: Widget renders on supported platform
- **WHEN** the widget opens on a platform supporting the selected material
- **THEN** the widget renders one composited backdrop beneath sharp foreground content without stacked translucent shell layers

#### Scenario: User selects a blur choice
- **WHEN** the user changes between Off, Solid, and Frosted
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
- **THEN** the shell uses the fully opaque theme background, the slider is unavailable, and the saved tint value is retained for Off and Frosted

#### Scenario: High Contrast theme active
- **WHEN** the user selects the High Contrast theme
- **THEN** the widget renders an unblurred solid shell with high-contrast text and controls regardless of its stored blur or tint settings
#### Scenario: Frosted with no tint
- **WHEN** the user selects Frosted with background tint at 0% on a qualified supported Windows system
- **THEN** desktop windows behind the widget remain visible but blurred while foreground text and controls stay sharp
- **AND** raising tint to 100% hides the backdrop without changing foreground opacity

#### Scenario: Frosted unavailable
- **WHEN** Frosted is selected but native attachment fails, transparency is disabled, or the environment cannot support it
- **THEN** an opaque theme-colored fallback appears without changing layout, saved material, or tint
- **AND** existing diagnostics identify the requested material, effective fallback, and reason

#### Scenario: Material support changes
- **WHEN** effects become unavailable or available while Frosted is requested
- **THEN** the widget updates to the solid fallback or resumes supported frost after the change is observed, without rewriting preferences

#### Scenario: Frosted appearance commit and discard
- **WHEN** the user previews Frosted and tint, then successfully applies or discards the draft
- **THEN** successful Apply/OK preserves the appearance across restart, and Cancel/title-bar close restores the last successfully committed appearance

#### Scenario: Frosted appearance commit fails
- **WHEN** Apply or OK fails to write settings, or later activation fails and compensation succeeds
- **THEN** Settings remains open with an error, the prior saved settings and commit restore point remain intact, and the Frosted and tint draft preview is retained
- **AND** a later Cancel/title-bar close restores the last successfully committed appearance
- **AND** unavailable blur uses the material fallback independently of the commit result, without discarding the draft or rewriting its requested material or tint

#### Scenario: Frosted appearance compensation fails
- **WHEN** a failed commit cannot restore the prior settings
- **THEN** Settings retains recovery data, blocks new commits pending recovery, and reports recovery required without claiming which values are active, as required by the Settings contract

### Requirement: Widget control placement
The system SHALL let the user place widget controls beside or below the clock in both modes, defaulting to beside. Placement SHALL preview immediately, scale with the widget, persist through Apply/OK, and restore on Cancel or title-bar close. Full-mode controls SHALL remain a horizontal row; compact controls SHALL stack vertically beside the clock and horizontally below it. The project field SHALL remain below full-mode clock and controls.

#### Scenario: Preview and discard placement
- **WHEN** the user selects below-clock placement and closes Settings without applying
- **THEN** both modes preview the new placement and restore the last committed placement on close

#### Scenario: Persist and scale placement
- **WHEN** the user applies below-clock placement and scales either widget mode
- **THEN** the placement survives restart and clock, controls, and hit targets scale proportionally

### Requirement: Widget frost geometry and lifetime
Frosted rendering SHALL follow the widget bounds and rounded shell in both modes and control placements at every supported scale. Foreground hit targets, dragging, topmost behavior and overall opacity SHALL retain their existing behavior. Hide/show, handle recreation and close SHALL not leave native resources or backdrop visuals behind.

#### Scenario: Native widget geometry changes
- **WHEN** the user switches mode or placement, changes scale or display DPI, drags the widget, or changes overall opacity
- **THEN** backdrop and shell remain aligned without clipping controls, revealing square corner artifacts, or leaving a backdrop outside the window
- **AND** the native material follows the overall fade with the foreground

#### Scenario: Window lifetime ends or restarts
- **WHEN** the widget hides, shows, recreates its handle, or closes repeatedly
- **THEN** the effect attaches only to the current window, releases obsolete resources, and preserves interaction behavior

#### Scenario: Overall widget opacity reaches zero
- **WHEN** the user reduces overall widget opacity to zero while Frosted is effective
- **THEN** neither foreground nor native backdrop remains visible, and desktop content is not blurred by the invisible widget

#### Scenario: Widget backdrop updates are isolated
- **WHEN** the widget changes its rounded bounds or overall opacity while Settings or Worklog is open
- **THEN** only the widget backdrop follows those changes, and the existing Settings and Worklog material remains unchanged

### Requirement: Native widget frost qualification
Frosted SHALL be released only after native Windows evidence distinguishes it from clear transparency and solid fill over live desktop content, with sharp foreground and verified fallback. Qualification SHALL record OS, renderer, display scale, effect preferences, visual evidence and resource costs. A successful API result or headless rendering SHALL NOT count as proof of desktop blur.

#### Scenario: Visible native effect qualification
- **WHEN** Frosted is tested against Off and Solid at matched tint over controlled desktop patterns and moving content
- **THEN** evidence demonstrates edge widening after local contrast normalization, the design's minimum visible background detail, and backdrop changes corresponding to moving underlying windows while the widget remains stationary
- **AND** sharp foreground and solid fallback are verified across both widget modes and placements; lower raw edge contrast alone cannot qualify Frosted

#### Scenario: Invalid qualification environment
- **WHEN** capture is obscured, effects are disabled, or the control pattern is not visible
- **THEN** frost remains unverified and the feature is not accepted for release

#### Scenario: Resource qualification
- **WHEN** idle, drag, hide/show and effect-toggle workloads are compared against Off
- **THEN** recorded CPU, memory, rendering responsiveness and native lifetime measurements meet the design's release budgets
