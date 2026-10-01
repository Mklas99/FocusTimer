# Tasks

## 1. Theme appearance and compatibility

- [x] 1.1 Add Off/Solid backdrop values to `Theme`, migrate saved Soft/Strong/Blur values to Off, default missing values compatibly, preserve the choice in `Clone()` and built-in presets, and verify focused model tests.
- [x] 1.2 Include the blur value in `.fttheme` export/import, reject unsupported supplied values without changing the active theme, and verify round-trip, old-file, and invalid-value tests pass.
- [x] 1.3 Persist the blur value through the existing settings path and verify a saved selection survives restart while older settings files retain their colors and opacity values.
- [x] 1.4 Preserve a persisted built-in theme's edited blur and background opacity at startup, using a factory preset only for new/default settings or explicit preset selection; verify restart tests cover edited Dark values and first-run Dark defaults.

## 2. Widget material and opacity

- [x] 2.1 Replace the opaque base/panel pair with one widget-specific shell tint and an independent opaque fallback; remove duplicate background and Overall Fade alpha application, then verify focused resource/binding tests show background opacity changes no clock, button, or project-field opacity.
- [ ] 2.2 Map Off/Solid to clear transparency or an opaque shell, apply selection live, observe the achieved level for fallback reporting, and verify a running Windows widget switches modes without moving foreground content.
- [ ] 2.3 Enforce solid, unblurred High Contrast and unavailable-transparency fallbacks, and verify tests plus a Windows rendering check cover 0% tint, Off, unsupported blur, and readable foreground controls.
- [ ] 2.4 Bridge Settings appearance preview to the live widget, restore blur and tint after Cancel or window close, and advance the restore point after successful Apply; verify both modes update before saving and restore correctly after a canceled preview.
- [x] 2.5 Update the widget material guidance in `design/` and the affected OpenSpec/main-doc descriptions when implementation settles, and verify they describe the shipped fallback order and single-opacity ownership consistently.

## 3. Appearance control

- [ ] 3.1 Add the Off/Solid selector beside Background tint opacity, disable the slider for Solid without discarding its value, and verify selection and slider updates live in both widget modes.
- [x] 3.2 Correct or replace stale Appearance transparency diagnostics so they report the requested and achieved mode, and verify the displayed text matches each Windows fallback path.
- [x] 3.3 Update OI-25 in `docs/versions/current/OpenIssues.md` after implementation and verify its status and notes match the delivered behavior without claiming OI-21 or OI-24 is complete.

## 4. Integration verification

- [x] 4.1 Run the focused Core and App tests and a Host `win-x64` build; verify all pass without new warnings attributable to this change.
- [ ] 4.2 On Windows, check full and compact modes at Off/Solid and 0%, middle, and 100% saved background tint opacity over light and dark desktop content; verify Solid ignores the saved tint value and foreground opacity remains independent.
- [ ] 4.3 Verify Dark, Light, High Contrast, a saved custom theme, and an older imported theme after Apply and restart; record platform fallback behavior and confirm the widget remains readable when transparency is disabled.
