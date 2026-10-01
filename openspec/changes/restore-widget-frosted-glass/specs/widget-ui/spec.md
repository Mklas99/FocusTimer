# Spec Delta

## MODIFIED Requirements

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

#### Scenario: User cancels a background preview
- **WHEN** the user changes background opacity in Settings and closes it without applying
- **THEN** the widget restores the last successfully applied shell tint opacity

#### Scenario: User cancels an appearance preview
- **WHEN** the user previews blur or tint changes in Settings and closes without applying
- **THEN** the widget restores the last successfully applied appearance

#### Scenario: User removes the shell tint
- **WHEN** the user sets background tint opacity to 0% while Off is selected
- **THEN** the app adds no theme color tint behind the unchanged foreground content

#### Scenario: User adjusts foreground or overall opacity
- **WHEN** the user changes clock or controls opacity, or the explicit overall multiplier
- **THEN** only the corresponding foreground element changes for clock or controls opacity, while the overall multiplier fades the widget as a whole

### Requirement: Material Layering and Fallback
The timer widget SHALL offer Off and Solid background choices in Settings on one effective shell surface. Off SHALL request clear transparency and apply the selected tint opacity. Solid SHALL use a fully opaque theme-colored shell regardless of the saved tint opacity. Saved Soft, Strong, and Blur choices SHALL load as Off. The system SHALL provide a readable solid shell when transparency is unavailable or disabled.

#### Scenario: Widget renders on supported platform
- **WHEN** the widget opens on a platform supporting the selected blur level
- **THEN** the widget renders one composited backdrop beneath sharp foreground content without stacked translucent shell layers

#### Scenario: User selects a blur choice
- **WHEN** the user changes between Off and Solid
- **THEN** the backdrop changes live in both widget modes without blurring the clock, buttons, or project field

#### Scenario: User cancels a blur preview
- **WHEN** the user changes blur in Settings and closes it without applying
- **THEN** the widget restores the last successfully applied blur choice

#### Scenario: User selects Off with no tint
- **WHEN** the user selects Off and sets background opacity to 0% on a transparency-capable system
- **THEN** the shell background is clear while the clock, buttons, and project field remain visible at their configured opacities

#### Scenario: User selects Solid with no saved tint
- **WHEN** the user selects Solid while background tint opacity is 0%
- **THEN** the shell uses the fully opaque theme background, the slider is unavailable, and the saved tint value is retained for Off

#### Scenario: Material effects unavailable or disabled
- **WHEN** the operating system or environment does not support transparency effects or disables composition
- **THEN** the widget uses a readable solid or near-solid shell without moving its content or changing foreground opacity settings

#### Scenario: High Contrast theme active
- **WHEN** the user selects the High Contrast theme
- **THEN** the widget renders an unblurred solid shell with high-contrast text and controls regardless of its stored blur or tint settings
