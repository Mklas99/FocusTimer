# Widget UI delta

## MODIFIED Requirements

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
- **WHEN** the user previews Frosted and tint, then applies, cancels, or encounters a failed activation
- **THEN** successful Apply/OK preserves the preview across restart, Cancel/title-bar close restores the last commit, and failed activation restores the previously active material
## ADDED Requirements

### Requirement: Widget frost geometry and lifetime
Frosted rendering SHALL follow the widget bounds and rounded shell in both modes and control placements at every supported scale. Foreground hit targets, dragging, topmost behavior and overall opacity SHALL retain their existing behavior. Hide/show, handle recreation and close SHALL not leave native resources or backdrop visuals behind.

#### Scenario: Native widget geometry changes
- **WHEN** the user switches mode or placement, changes scale or display DPI, drags the widget, or changes overall opacity
- **THEN** backdrop and shell remain aligned without clipping controls, revealing square corner artifacts, or leaving a backdrop outside the window
- **AND** the native material follows the overall fade with the foreground

#### Scenario: Window lifetime ends or restarts
- **WHEN** the widget hides, shows, recreates its handle, or closes repeatedly
- **THEN** the effect attaches only to the current window, releases obsolete resources, and preserves interaction behavior

### Requirement: Native widget frost qualification
Frosted SHALL be released only after native Windows evidence distinguishes it from clear transparency and solid fill over live desktop content, with sharp foreground and verified fallback. Qualification SHALL record OS, renderer, display scale, effect preferences, visual evidence and resource costs. A successful API result or headless rendering SHALL NOT count as proof of desktop blur.

#### Scenario: Visible native effect qualification
- **WHEN** Frosted is tested against Off and Solid at matched tint over controlled desktop patterns and moving content
- **THEN** evidence demonstrates visible background variation, reduced sharp edges, sharp foreground, and solid fallback across both widget modes and placements

#### Scenario: Invalid qualification environment
- **WHEN** capture is obscured, effects are disabled, or the control pattern is not visible
- **THEN** frost remains unverified and the feature is not accepted for release

#### Scenario: Resource qualification
- **WHEN** idle, drag, hide/show and effect-toggle workloads are compared against Off
- **THEN** recorded CPU, memory, rendering responsiveness and native lifetime measurements meet the design's release budgets