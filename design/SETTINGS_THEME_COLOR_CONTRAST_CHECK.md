# Settings theme color contrast check

Measured on 2026-10-08 from ThemeManager resources after the palette polish follow-up. Enabled normal-size text needs 4.5:1; meaningful control/focus/selection indicators need 3:1. These are WCAG relative-luminance measurements of rendered colors, not antialiased screenshot pixels.

DesktopPaletteContrastTests emits `artifacts/theme-polish/contrast/derived-contrast.tsv` when `FOCUSTIMER_THEME_EVIDENCE_DIR` is set. Values below use current derived opaque surfaces. Headless tests separately resolve actual Fluent template parts and popup text, including alpha and brush opacity. The 95% shell tint is checked over black and white extremes separately from the solid table.

| Theme | Text/status minimum | Selected tab label | Hover tab label | Selection text | Selection indicator | Action text minimum | Action focus minimum | Notification body |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Dark | 4.50 | 4.91 | 6.10 | 8.10 | 3.01 | 4.51 | 3.02 | 4.74 |
| Light | 4.51 | 5.15 | 4.88 | 5.57 | 5.15 | 4.51 | 3.19 | 4.77 |
| Monokai | 4.50 | 7.53 | 6.36 | 7.53 | 6.19 | 4.50 | 3.02 | 5.02 |
| Solarized Dark | 4.50 | 7.77 | 6.05 | 7.77 | 6.73 | 4.51 | 3.04 | 4.54 |
| Nord | 4.50 | 6.24 | 5.51 | 9.15 | 5.03 | 4.50 | 3.02 | 4.55 |
| Dracula | 4.51 | 6.16 | 6.17 | 6.16 | 5.10 | 4.50 | 3.00 | 4.85 |
| High Contrast | 4.50 | 16.75 | 16.21 | 16.75 | 16.75 | 4.51 | 5.89 | 19.56 |

## Current rendered pairs

- Tabs have transparent normal/selected fills. The selected label/icon is derived from AccentPrimary on DesktopShellBrush; the underline is derived from TabSelectedBackground and needs 3:1. Hover uses DesktopTabHoverBrush with DesktopTabTextBrush. Focus uses a separate ring, not the underline.
- Fields and selected Worklog rows use DesktopSelectionBrush with DesktopSelectedTextBrush, derived from TabSelectedBackground/TabSelectedText. The opaque selection fill contrasts with fields, cards, and the shell. Translucent imports are composited over the field before correction. Selected timeline blocks use the same matched pair.
- Primary and destructive actions have normal/hover/pressed fills with paired text and a focus brush checked against each fill and its adjacent desktop surfaces. Neutral actions use field/hover/pressed surfaces and desktop text. Disabled controls remain identifiable without the enabled-text threshold.
- Dropdown popup items and calendar day labels have explicit template text/selection brushes, including inactive and hovered days. DesktopTabFocusBrush meets 3:1 against both the shell and hovered-tab fill; the pale import combined focus/hover regression passes. Color-swatch focus uses DesktopFocusBrush. Slider active/inactive tracks and thumb outline use derived indicator colors. Worklog Summary bars use DesktopFocusBrush on DesktopFieldBrush.
- Notifications use the derived shell, heading/body/status text, action colors, and focus indicators. Warning/error severity is explicit in the title. Color-picker fields use the same desktop brushes; its hex label has an opaque field background so mid-gray or translucent swatches cannot defeat shared text contrast.

## Evidence and limits

The screenshot follow-up adds cold-open checks under both Fluent variants, actual spinner Paths in normal/hover/pressed states, read-only placeholder TextBlocks with visual opacity, actual popup text/backgrounds, calendar navigation strokes, tooltips after their opening animation, and the expanded widget project row with a legacy Light snapshot. A popup ContentPresenter or a field's parent Foreground alone is insufficient evidence. Native captures now target the popup bounds at the window's DPI scale and include the Off/Solid dropdown and expanded project row; [verification](../openspec/changes/archive/2026-10-08-optimize-theme-readability/verification.md) records the reproduced failures.

All seven presets and five deterministic imports cover white-on-white, pale accents, opposing surfaces, translucent input/selection, and a Dark name collision. Source serialization is unchanged. The original regressions failed for field-composited translucent selection, infeasible shared foregrounds, a missed feasible gray, dark dropdown/calendar text, and the gray picker preview.

Native Windows captures cover Settings/Worklog pages, picker, notifications, and both widget modes. Captures establish representative visual review and layout evidence; the mouse/keyboard walkthrough and representative desktop-content widget checks were subsequently confirmed complete by the user on 2026-10-08. See [verification](../openspec/changes/archive/2026-10-08-optimize-theme-readability/verification.md) for commands, results, reviewed captures, and the user-confirmed native completion. The earlier Light control-chrome finding is superseded by the delivered shared styles.

Palette harmony and later personal tuning are documented in [Theme palette tuning](../docs/versions/current/ThemePaletteTuning.md). Transparent-widget readability remains dependent on desktop content and user opacity choices; no universal widget contrast guarantee is made.
