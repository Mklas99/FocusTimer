# Settings control state audit prompt

Audit the current Settings window before its visual redesign. This is a read-only investigation. Do not edit code, documentation, specs, or theme files.

## Goal

Identify where tabs, accordions, input fields, checkboxes, sliders, color swatches, and action buttons look or behave inconsistently across Settings pages. Give the redesign a verified baseline for shared control states and layout rules. Keep the timer widget layout outside this audit.

## Inspect

- `src/FocusTimer.App/Views/SettingsWindow.axaml` and `SettingsWindow.axaml.cs`.
- `src/FocusTimer.App/ViewModels/SettingsWindowViewModel.cs` and relevant settings models.
- `src/FocusTimer.App/Styles/ControlStyles.axaml`, `Tokens.axaml`, `ThemeResources.axaml`, and `App.axaml`.
- `design/UI_DESIGN_STANDARD.md`, `design/FOCUSTIMER_UI_ADDENDUM.md`, relevant OpenSpec specs, and `docs/versions/current/OI-21-SettingsRedesign.md`.

Trace each visible control to its effective style, including Avalonia Fluent defaults, local property values, shared selectors, inherited properties, and theme resources. Inspect all Settings tabs: General, Logging, Appearance, Hotkeys, and About, including the developer section when unlocked. If a running UI or screenshots are available, compare rendered states with the code; otherwise label visual conclusions as hypotheses.

## Questions to answer

1. For each control type, what are its normal, hover, pressed, selected/checked, disabled, invalid, and keyboard focus states where applicable? Which states are missing or visually inconsistent?
2. Which controls use one-off widths, margins, padding, corner radii, fonts, or colors? Where do alignments or spacing change between tabs without a clear reason?
3. Are focus order and focus indicators usable with Tab, Shift+Tab, Enter, Space, and arrow keys? Are labels, tooltips, and pointer targets clear enough to identify each action?
4. Are the footer actions (Save/OK, Apply, Cancel) visually distinct and consistently placed? Compare them with Browse, Open, Import, Export, Reset, and color swatches.
5. Do accordion headers clearly show expanded, collapsed, hover, and focus states? Do tabs clearly show selected and focused states separately?
6. Does the Appearance tab remain scannable with the theme preset actions, color fields, opacity controls, and diagnostics visible? Identify hierarchy problems without designing a compact-window layout.
7. Where does current behavior affect visual state, such as validation errors, save failures, immediate theme updates, or disabled settings? Note behavior that needs a separate decision rather than treating it as a pure styling issue.

## Deliverable

Return:

1. A short diagnosis with file and line references.
2. A matrix by control type and state, showing current appearance source, inconsistency, and recommended shared rule. Mark unverified rendered behavior clearly.
3. A page-by-page list of concrete alignment, spacing, hierarchy, and interaction issues, prioritized by impact.
4. A focused design checklist for implementation and a manual verification checklist across all tabs, built-in themes, an imported theme, and keyboard interaction.

Do not implement changes. Keep the recommendations within the Settings redesign scope in OI-21.
