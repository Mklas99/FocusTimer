# Proposal

## Why

OI-24 still tracks palette readability after the dense-frost UI refresh. Themes also need coordinated backgrounds, accents, text, borders, and status colors so each palette feels coherent across the app, while remaining easy to adjust to personal taste later. Existing tests leave some rendered and custom-palette states incompletely checked; OI-29 leaves imported themes without a stable visible preset selection.

## What Changes

- Audit and correct rendered desktop contrast across all seven built-in themes and difficult imported palettes, including selection, focus, control states, popups, and notifications.
- Harmonize each built-in theme's backgrounds, accents, text, borders, and success/warning/error colors across the widget, dialogs, Worklog, and notifications. Balance saturation, brightness, and visual hierarchy while retaining each theme's identity and the contrast requirements.
- Preserve stored imported and edited theme colors while deriving readable desktop colors for each actual background. Permit built-in palette adjustments for visual harmony as well as measured readability failures; record the reason for each adjustment.
- Keep source palette values separate from derived accessibility corrections so individual themes can be tuned to personal taste later through existing preset definitions, color editing, and import/export. Later tuning must preserve readable controls and saved user edits; no new theme editor or preset library is introduced.
- Add a Custom/Imported preset entry and restore it through Apply/OK, reopening, restart, and Cancel without replacing the saved theme with factory defaults or reloading an import file.
- Extend resource and headless control tests, then record a native Windows walkthrough, a visual harmony review for each built-in theme, and the current contrast audit. Numerical contrast alone does not establish harmony.
- Keep widget opacity and color controls independent. Verify both widget modes on representative desktop backgrounds and their solid fallback.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `theming`: Coherent built-in palettes with support for later personal tuning; readable desktop colors across rendered states; stable imported-theme selection and lossless saved-theme restoration.

## Impact

Primarily FocusTimer.App: ThemeManager, ThemeContrast, shared styles, and SettingsWindowViewModel. FocusTimer.Core preset definitions may change for palette harmony and measured contrast. Existing settings fields and theme JSON remain compatible; no new dependency, storage migration, or platform integration is planned.

The existing `adopt-dense-frost-design-system` change owns layout, material, and action hierarchy. This change follows its implemented theme-role mapping and must reconcile that change's theming delta before spec synchronization. Native widget blur (OI-28), optional nonappearance preview (OI-44), notification configuration/test actions (OI-41/OI-43), and Linux platform parity (OI-06) remain separate.
