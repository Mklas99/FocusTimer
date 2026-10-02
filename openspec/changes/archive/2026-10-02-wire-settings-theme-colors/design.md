# Design

## Context

See [proposal.md](proposal.md). `ThemeManager` publishes existing color and brush resources, but several Settings controls still take text, field, and tab colors from Avalonia Fluent. It also assigns `AccentPrimary` to the `InputFocusBorderColor` and `TabSelectedBackgroundColor` keys. `App.axaml` uses the system's default Fluent variant; a Light palette can therefore coexist with dark Fluent defaults. The audit reports are useful for mapping, but their widget and Cancel findings predate recent code changes.

## Goals / Non-Goals

**Goals:** Make the existing theme values authoritative for Settings text, field, and tab roles; preserve live updates and theme-file compatibility; verify rendered contrast in all seven built-in themes.

**Non-Goals:** Redesign Settings geometry or action buttons, add new serialized palette fields, change widget or native tray styling, or implement the full OI-21 draft/save workflow. Current appearance-preview restoration on close remains in place.

## Decisions

1. **Correct resource sources in `ThemeManager`.** Populate `InputFocusBorderColor` from `Theme.InputFocusBorder` and `TabSelectedBackgroundColor` from `Theme.TabSelectedBackground`. Keep the established color/brush key names so existing theme files remain compatible. An alias from `AccentPrimary` would retain the mismatch users observe.
2. **Scope control styles to Settings.** Add a Settings-window style marker and use shared selectors in `FocusTimer.App/Styles/ControlStyles.axaml` for ordinary and disabled text, editable/read-only `TextBox` and `NumericUpDown`, `ComboBox` and its displayed selection, and tab states. Preserve the existing heading and label roles. Do not make these selectors global: the widget project field and other windows have distinct palette roles.
3. **Make state colors reach rendered parts.** Use dynamic brush resources for normal, hover, selected, and keyboard-focus states. Inspect the active Fluent template parts during implementation where a control property alone is overridden, particularly tab headers and input focus borders. Prefer scoped template-part styles to changing `RequestedThemeVariant` globally, which would affect unrelated windows and widget controls.
4. **Measure built-in colors against rendered surfaces.** Check Dark, Light, Monokai, Solarized Dark, Nord, Dracula, and High Contrast at the normal Settings size. Use 4.5:1 for normal text and 3:1 for selected-tab and focus indicators. If an existing built-in palette fails, adjust only the necessary built-in color values while preserving serialized property names and geometry. Record measured pairs and verify live switching, rather than inferring contrast from hex values alone.

## Risks / Trade-offs

- [Fluent template state brushes may cover project values] → Inspect the winning rendered value and target the relevant Settings template part; include a focused UI check for normal, hover, selected, disabled, and keyboard-focus states.
- [A shared `PrimaryText` or `DisabledText` color may be unsuitable on some Settings surfaces] → Measure against the actual surface used by each control and adjust built-in palette values where necessary. Do not silently substitute widget button colors.
- [Changing built-in colors can alter a user's chosen preset] → Limit adjustments to failing pairs, document them in the theme mapping, and keep custom `.fttheme` serialization unchanged.

## Migration Plan

No data migration is needed. Existing theme files already contain these fields. Rollback is a code/style revert; saved custom values remain valid.
