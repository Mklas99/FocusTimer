# Design

## Context

Slider thumb fills derive from AccentPrimary with 3:1 contrast against the active track, while the outline retains DesktopFocusBrush. The accent tint remains stable through hover, pressed and focused states.

The theme dropdown keeps one observable list of preset names for the editor's lifetime. Imports add the current Custom/Imported entry and preset selection removes it under the existing selection guard. Refreshing appearance never replaces ItemsSource, so a control-originated choice cannot reselect the previous preset during source rebinding.

Expanded accordion headers explicitly retain desktop text colors through checked, hover and pressed states. Color swatches use a dedicated presenter so Fluent's button hover cannot replace their source fill; interaction changes only the outline. Project suggestions use AutoCompleteBox, whose field, popup border and ListBoxItem states need their own desktop selectors in addition to ComboBox styling.

The Worklog calendar popup uses the desktop card radius with rounded clipping, a 3:1 desktop border, and a divider directly below the month navigation. A compact CalendarItem template retains the named navigation, month and year parts and the empty month backdrop. Its width fits seven date columns instead of Fluent's fixed minimum, and its height fits the six date rows. Current-month days use the subtle derived card background; adjacent-month days retain the shell background. Hover and selection keep their distinct derived colors. The date field uses one derived field surface across normal, hover, pressed and focused states, with the shared readable selection pair and an outer focus outline. Date-range behavior remains intact.

See proposal.md for motivation. The baseline is branch `feature/F-05_Revamp-UI-and-usability`, HEAD `ebacbec`, with pre-existing staged UI-review documentation and a native material test. Preserve that work.

ThemeManager already derives opaque desktop fields, text, action fills, severity colors, and focus brushes. ThemeContrast adjusts toward white or black without rewriting Theme. The existing contrast tests passed on 2026-10-08 (15 tests), and selected headless control/Worklog/notification tests passed (16 tests). This is automated baseline evidence, not a native visual walkthrough.

Observed gaps:

- DesktopControlStateTests uses one sample palette. WorklogWindowThemeTests checks rendering and geometry, but not selected-row contrast across palettes.
- TextBox selection uses the raw TabSelectedBackgroundBrush. Its derived foreground is calculated against that fill composited over SettingsBackground, even though the field can have a different background.
- EnsureContrastAcross returns its white/black endpoint when no shared foreground qualifies. That result does not establish that every background meets the requested ratio.
- AvailableThemes lists only built-ins. Import selects Custom in the UI but saves the imported ThemeName as ActiveThemeName. Loading and restoring selection then use ActiveThemeName directly. Preset selection/reset currently leave CustomThemePath behind.
- Main theming specs still describe filled tabs and InputFocusBorder, while the active dense-frost delta describes underlines and accent-derived focus. Treat that active delta as the implemented role mapping and reconcile it before syncing specs.
- The old contrast audit covers earlier tab treatment; its Light control-chrome finding predates the delivered shared styles.

## Goals / Non-Goals

Use the existing theme model, settings draft/restore points, resource dictionary, and shared styles. Add state-specific brushes only when the measured backgrounds require them. Keep the source role identifiable in code and documentation.

Give each built-in theme a coherent palette across widget and desktop views. Keep palette choices easy to revisit for personal taste without changing control styles, contrast thresholds, or theme-file structure.

Do not rebuild the design system, alter widget transparency behavior, add notification-trigger features, or claim platform blur or Linux parity. No platform service or Linux-stub changes are expected.

## Decisions

### 1. Measure the rendered pair before changing a preset

Build a matrix of control/state, actual foreground, actual composited background, threshold, and result. Cover seven presets plus custom fixtures with all-white colors, pale accents, light/dark opposing surfaces, translucent input/selection fills, and a built-in-name collision. Headless tests must inspect template parts and popup children rather than only dictionary keys.

Extend the existing tests instead of adding a screenshot approval framework. Native Windows screenshots and the existing ManualUiWalkthrough complement numerical checks; antialiased pixels and OS composition cannot be proven by resource assertions alone.

### 2. Correct desktop rendering without mutating imported themes

Resolve alpha and brush opacity against the background before measuring. Derive opaque selection fills and paired readable text for fields and Worklog rows, or use separate paired brushes where their backgrounds differ. Keep TabSelectedBackground and TabSelectedText as the preferred sources, consistent with dense-frost underline navigation.

Use a shared foreground only when it qualifies against every actual surface. Add explicit feasibility coverage for EnsureContrastAcross; callers must use per-state foregrounds or normalized derived fills when no common foreground qualifies. Do not silently accept an endpoint below the threshold.

Retain hue where possible, but readability wins over preserving an exact displayed custom color. Source values still survive export. Fix contrast regressions before the palette harmony pass below, and rerun those checks after aesthetic adjustments. Do not update saved palettes at startup to match new factory defaults.

### 3. Harmonize source palettes and leave room for personal taste

Review each of the seven presets as a complete palette. Coordinate window/card/field backgrounds, primary and secondary text, accents, borders, selection, widget icons, and success/warning/error colors. Use a consistent neutral family and restrained emphasis within each theme; derive hover/pressed relationships from the same roles rather than introducing unrelated colors. Retain recognizable preset character, including High Contrast's stronger distinctions. Harmony does not require every role or every theme to use the same hue or saturation.

Record a per-theme role table with source values, visual intent, and the reason for any change. Compare swatches and representative views of the full/compact widget, Settings, Worklog, color picker, and notifications. Numerical checks establish readability; visual review establishes whether accents compete, borders dominate, status colors look unrelated, or text levels lose hierarchy. Do not invent a numerical harmony score or lock exact hex values in the behavior spec.

Keep authored palette values in the existing built-in definitions and Theme fields. Derived desktop brushes remain accessibility safeguards, not a second authored palette. A later request to make a particular theme warmer, quieter, or more saturated should change its source roles and repeat the relevant contrast/visual checks, without rewriting styles or replacing other themes. Existing editor and import/export flows remain available; code-owned severity colors stay tunable in preset definitions, with no new editor controls in this scope.

Saved edited built-in and imported snapshots take precedence over later factory tuning. The user opts into new factory values by selecting a preset or resetting. Readability correction must not erase those saved preferences or export corrected colors in place of the authored palette.

The alternative of adjusting only colors with a failing ratio would leave visually mismatched but readable palettes untouched. A new palette generator, fixed hue-distance rules, and a theme-library feature would add unnecessary scope. Use deliberate per-theme adjustments and review evidence instead.

### 4. Use existing settings fields for import identity

Use ActiveThemeName = Custom for newly imported themes and keep Theme.ThemeName as their original metadata. Keep CustomThemePath as provenance, not a requirement to reload the file. The dropdown includes Custom/Imported only when a current custom draft exists; displaying/selecting that entry preserves the current draft rather than loading a factory preset. This change does not add a library of saved custom presets.

Legacy settings with a nonempty CustomThemePath and an ActiveThemeName matching Theme.ThemeName are treated as an imported snapshot even when the name matches a built-in preset. Normalize their identity in the in-memory draft; persist through the ordinary successful commit, not on open. Old preset transitions can leave a stale path that is indistinguishable from a same-name import. Prefer preserving the saved snapshot and showing Custom/Imported; explicitly selecting a built-in or Reset clears provenance and resolves the ambiguity. Unrecognized saved names without a path also display Custom/Imported while retaining their stored snapshot.

Use the same identity rule in startup, Settings load, and restore-after-Cancel. Guard any existing mismatch repair so an imported snapshot cannot load factory defaults. Audit AppController startup as well as SettingsWindowViewModel; a dropdown-only fix is insufficient. Preserve edited built-in snapshots and clear custom provenance on explicit preset/reset transitions.

### 5. Keep the work separate from the existing UI refresh

Use additive theming requirements here to avoid replacing the active refresh's Settings color roles block. At sync/archive, apply or reconcile the dense-frost role delta first, then this change's additional guarantees. Record OI-24 palette completion separately from the shared native interaction checks for OI-21/OI-42. Link planning artifacts in the live backlog without claiming implementation is complete.

## Risks / Trade-offs

- Opposing custom surfaces can defeat a common foreground. Test that case directly and use paired state colors rather than accepting a failed ratio.
- Near-solid desktop frost still varies with the backdrop. Check compositing against light and dark extremes plus solid fallback; opaque controls provide stable measurement surfaces.
- Transparent widget readability depends on arbitrary desktop content and foreground fade settings. Verify representative light/dark content and solid fallback; no universal contrast guarantee is added for transparent widget settings.
- Legacy import provenance can be stale. Preserve saved values and document the conservative identity choice; add a regression for the ambiguous case.
- Popups may have separate resource scopes. Inspect rendered popup controls and repeat a live theme switch while open.
- Harmony and personal taste are subjective. Record per-theme visual intent and before/after views; keep later taste adjustments possible instead of treating the first palette pass as final.

## Migration Plan

No theme-file or settings-schema migration. Land contrast coverage and rendering fixes first, then harmonize built-in palettes and finish identity handling and lifecycle tests. Saved themes remain snapshots; factory palette adjustments affect explicit preset selection/reset only. Rollback restores previous rendering behavior while existing saved Custom identity and theme metadata remain usable by older builds; verify this with existing serialization tests.

Before marking the change complete, run targeted tests and the native Windows theme walkthrough, record the tested commit and any unavailable checks, and update the contrast audit and backlog. Existing display-scaling approval need not be repeated unless these changes affect geometry.
