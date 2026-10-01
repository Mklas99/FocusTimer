# Settings theme color contrast check

The table records contrast ratios for the color pairs used by the Settings text, input, and tab styles at the normal 640 × 540 window size. Ratios use WCAG relative luminance. Translucent input backgrounds are composited over `SettingsBackground` before measurement. Field focus compares `InputFocusBorder` with the field surface; tab focus compares it with the unselected tab surface. The selected tab column compares its background with the unselected tab surface. Normal text and selected tab text need 4.5:1; the selected tab and focus indicators need 3:1.

| Built-in theme | Body | Label | Heading | Field text | Tab text | Hover tab text | Selected text | Selected tab | Field focus | Tab focus |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Dark | 12.59 | 8.55 | 12.59 | 10.01 | 8.55 | 10.38 | 4.51 | 3.04 | 5.04 | 6.90 |
| Light | 16.67 | 6.90 | 16.67 | 16.67 | 6.33 | 5.63 | 5.12 | 4.70 | 4.50 | 4.13 |
| Monokai | 15.54 | 10.53 | 15.54 | 10.27 | 9.45 | 6.96 | 6.81 | 6.81 | 5.02 | 6.81 |
| Solarized Dark | 4.86 | 4.86 | 4.86 | 4.86 | 5.61 | 4.86 | 5.71 | 4.08 | 3.53 | 4.08 |
| Nord | 8.73 | 7.45 | 8.73 | 8.73 | 9.25 | 7.45 | 4.54 | 3.10 | 5.03 | 6.24 |
| Dracula | 8.59 | 4.98 | 8.59 | 8.59 | 7.74 | 4.98 | 5.90 | 5.90 | 3.79 | 5.90 |
| High Contrast | 21.00 | 19.56 | 21.00 | 21.00 | 19.56 | 16.21 | 16.75 | 16.75 | 16.75 | 16.75 |

The `SettingsThemeColorTests` check these pairs against the thresholds. Isolated rendered Settings previews were inspected in all seven themes, including a Dark-to-Light switch in an open window, the focused Appearance field, hovered and keyboard-focused tabs, and Light/High Contrast. These visual checks confirm that the relevant Fluent template parts display the named resources. They do not measure antialiased text pixels or establish contrast for imported custom palettes.

To clear failing pairs, the built-in palettes now use a brighter Dark input focus border, a darker Light selected-tab background, lighter Solarized Dark label/tab text with black selected-tab text, a brighter Nord focus border with darker selected-tab text, and darker Dracula selected-tab text. Theme fields and control geometry did not change.

The Light preview still exposes weak checkbox and action button chrome. That broader control treatment remains under OI-24; it is separate from the checked text, field, and tab roles.
