# Tasks

## 1. Rendered contrast audit and regressions

- [x] 1.1 Create deterministic custom fixtures for white-on-white, pale accents, opposing surfaces, translucent inputs/selections, and a theme named Dark; verify import/serialization accepts the fixtures and preserves their values.
- [x] 1.2 Extend headless control-state coverage across seven presets and the custom fixtures for buttons, text/numeric fields, checked controls, sliders, tabs, selected Worklog rows, and date/dropdown popups; inspect resolved template brushes and verify contrasts after alpha/opacity compositing against actual adjacent backgrounds, with failures naming theme/control/state/pair.
- [x] 1.3 Add focused regression cases for translucent text selection on a field different from SettingsBackground and an infeasible shared foreground in ThemeContrastTests/DesktopPaletteContrastTests; verify each new case reproduces the observed gap before the corresponding fix.
- [x] 1.4 Record the control/state matrix and existing-versus-missing coverage in this change's verification.md; verify every failed pair maps to a concrete resource/style consumer rather than an assumed palette defect.

## 2. Desktop color derivation and palette harmony

- [x] 2.1 Make contrast derivation distinguish feasible shared foregrounds from infeasible sets; add state-specific foregrounds or normalized derived surfaces at affected consumers and verify opposing-surface and already-readable-color tests.
- [x] 2.2 Derive matched readable selection fill/text brushes for text fields and Worklog rows, preserving TabSelectedBackground/TabSelectedText as source roles; verify selected/unselected distinction, alpha cases, and live switching while selected in rendered controls.
- [x] 2.3 Fix audited control, popup, tab, notification, and focus pair failures in ThemeManager/shared styles; verify every supported rendered state meets 4.5:1 text and 3:1 meaningful-indicator thresholds across the fixture matrix without geometry changes or widget selector leakage.
- [x] 2.4 Record a per-theme source-role table and visual intent for backgrounds, text levels, accents, borders, selection, widget icons, and success/warning/error colors; verify all seven presets are covered and distinguish harmony decisions from measured contrast failures.
- [x] 2.5 Update design/SETTINGS_THEME_COLOR_CONTRAST_CHECK.md with current underline-tab, selection, action, popup, and notification pairs and measured results; verify values come from the current resources/composited surfaces and distinguish automated evidence from native review.
- [x] 2.6 Harmonize built-in source palettes for coherent neutral families, balanced saturation/brightness, and clear emphasis across widget and desktop views, preserving each preset's character and semantic status distinctions; verify before/after swatches and representative views against task 2.4's intent, rerun contrast/serialization checks, and confirm derived rendering leaves saved imported and edited snapshots unchanged.
- [x] 2.7 Document how to adjust an individual preset to taste later through existing source roles, supported color editing, and import/export, including code-owned severity values and the required contrast/visual checks; verify the documented adjustment path does not require new styles, a new theme schema, or changes to other presets.
- [x] 2.8 Add a regression for factory colors changing after a user saves personal palette edits; verify load/reopen/restart/export preserve saved source colors, and explicit preset selection/reset alone previews the new factory palette.

## 3. Imported preset identity and lifecycle

- [x] 3.1 Add regressions for a newly imported arbitrary name, an import named Dark, legacy path/name identity, ambiguous stale provenance, and an unknown saved name without a path; verify the tests reproduce current selection errors while preserving the saved palette.
- [x] 3.2 Add the conditional Custom/Imported dropdown entry, canonical Custom identity for new imports, and a consistent legacy interpretation in Settings load/restore and AppController startup; verify reopening/restart never substitutes factory values or reads an unavailable source file.
- [x] 3.3 Clear import provenance on explicit built-in selection/reset, and preserve edited built-in snapshots; verify import-to-built-in-to-restart, Reset, and built-in color/opacity edits across restart.
- [x] 3.4 Verify import and preset changes through Apply, OK, Cancel, title-bar close, Cancel after Apply, and failed commit/retry using existing isolated settings tests; assert palette, identity, source path, and runtime preview restore to the last successful commit and that opening Settings does not save.
- [x] 3.5 Document Custom/Imported behavior and the conservative legacy identity rule in the relevant theme/settings documentation; verify docs agree with regression cases and retain the single-current-draft boundary.

## 4. Windows integration and completion evidence

- [x] 4.1 Run the affected Core, App, and headless test suites plus repository-required formatting/build checks; record commands, tested tree, results, and any failures in verification.md without modifying pre-existing staged work.
- [x] 4.2 Complete the theme-focused native Windows checks from docs/versions/current/ManualUiWalkthrough.md across all presets and a difficult import, including live switches with focused fields/open popups, notifications, color picker, and Settings commit/discard; include each preset's visual harmony review and record build, Windows environment, captures, and failures, leaving unperformed checks open.
- [x] 4.3 Verify Full/Compact widget legibility over representative light/dark desktop content, Off/Solid behavior, and High Contrast fallback; record the tested opacity values and limitations without changing backdrop behavior or claiming universal transparent-widget contrast.
- [x] 4.4 Reconcile the implemented dense-frost Settings role delta before syncing this change's additional theming guarantees; update OI-24/OI-29 and the relevant Features collection only to the status supported by evidence, retain OI-21/OI-42 native checks that remain open, and verify OpenSpec validation and documentation consistency.

No Platform.Windows service or Linux-stub work is planned. If audit evidence requires a platform integration change, revise the scope explicitly and record the missing Linux equivalent under OI-06 before implementing it.

Native tasks 4.2 and 4.3 were closed by user confirmation on 2026-10-08 ("everything fine"). Environment details and additional captures were not supplied; see verification.md.
