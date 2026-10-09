# Theme palette tuning

Factory palettes live in `src/FocusTimer.Core/Services/ThemeService.cs`. These are source values, not derived desktop brushes. The following comparison records the 2026-10-08 palette polish, including the follow-up surface and accent tuning; numerical contrast evidence is in [the contrast audit](../../../design/SETTINGS_THEME_COLOR_CONTRAST_CHECK.md).

The widget project label derives from `WindowForeground` against the widget shell, rather than using the desktop `SecondaryText` role. `ProjectTagText` supplies the project field and its placeholder. Personalize these roles in the preset source or an imported theme file; the existing color editor does not expose `WindowForeground`. Source snapshots are preserved. Desktop placeholders, spinner glyphs, and popup surfaces use derived desktop brushes independently of Fluent's Light/Dark variant.

Before columns retain the original pre-harmony source values; Current columns reflect the latest factory palettes. Native before/after sheets are under `artifacts/theme-polish/before-overview.png` and `artifacts/theme-polish/after-overview.png`. The native fixture captured all Settings and Worklog pages, the picker, and notifications for the six tuned presets, plus Full/Compact widgets. Interactive walkthrough items in ManualUiWalkthrough were confirmed complete by the user on 2026-10-08.

## Palette intent and source roles

Slider thumbs derive their fill from AccentPrimary, adjusted toward a lighter or darker tint to remain visible against the active track. Their outline retains the derived focus accent.

Expanded accordion headings use desktop text colors. Swatches retain their source fill when hovered or pressed; the outline supplies feedback. Project suggestions use the same derived shell, hover, pressed and selection roles as desktop dropdowns.

The Worklog calendar fits seven date columns with a divider below its month navigation. Current-month days use the subtle derived card background, while adjacent-month days keep the shell color. Hover and selection use the shared desktop state brushes.

### Dark

Blue charcoal surfaces, pearl text, and a soft periwinkle-blue accent. Lift fields just above the shell; use dusty rose, sage, and sand for statuses.

| Source role | Before | Current |
|---|---|---|
| WindowBackground | `#1E1E1E` | `#1B2029` |
| SettingsBackground | `#2D2D30` | `#1F2631` |
| InputBackground | `#1AFFFFFF` | `#262E3B` |
| PrimaryText | `#F5F5F5` | `#E9EDF5` |
| SecondaryText | `#CCCCCC` | `#AAB7CA` |
| AccentPrimary | `#0078D7` | `#8ABAF4` |
| InputBorder | `#35FFFFFF` | `#465369` |
| TabSelectedBackground | `#007ACC` | `#8ABAF4` |
| TabSelectedText | `#FFFFFF` | `#1B2029` |
| TimerText | `#FFFFFF` | `#E9EDF5` |
| ButtonNormal | `#4CDEFFBD` | `#8ABAF4` |
| ButtonHover | `#5DEFFCE7` | `#ACD0FF` |
| ButtonPressed | `#3BBD99AC` | `#6B9EDB` |
| SuccessColor | `#4CAF50` | `#94C6AC` |
| WarningColor | `#FFA726` | `#DFC08A` |
| DangerColor | `#D9534F` | `#E58B98` |

Shells, fields, text levels, and interaction states use the palette described above. Hover and pressed colors stay in the accent family. Severity colors remain distinct and notifications spell out warning/error.

### Light

Cool porcelain surfaces, white fields, ink-blue text, and a restrained sapphire accent. Blue-gray tags and hover states keep the controls in the same neutral family.

| Source role | Before | Current |
|---|---|---|
| WindowBackground | `#F5F5F5` | `#F4F6FA` |
| SettingsBackground | `#FFFFFF` | `#F4F6FA` |
| InputBackground | `#FFFFFF` | `#FFFFFF` |
| PrimaryText | `#1E1E1E` | `#273449` |
| SecondaryText | `#5A5A5A` | `#57667A` |
| AccentPrimary | `#0078D7` | `#3569AC` |
| InputBorder | `#CCCCCC` | `#C9D2E0` |
| TabSelectedBackground | `#006FC7` | `#3569AC` |
| TabSelectedText | `#FFFFFF` | `#FFFFFF` |
| TimerText | `#1E1E1E` | `#273449` |
| ButtonNormal | `#0078D7` | `#3569AC` |
| ButtonHover | `#1084DD` | `#285A98` |
| ButtonPressed | `#006CBE` | `#204B80` |
| SuccessColor | `#388E3C` | `#357859` |
| WarningColor | `#F57C00` | `#8B651E` |
| DangerColor | `#D32F2F` | `#AD465D` |

Shells, fields, text levels, and interaction states use the palette described above. Hover and pressed colors stay in the accent family. Severity colors remain distinct and notifications spell out warning/error.

### Monokai

Warm olive charcoal, cream timer text, and amber actions. Softer rose, lime, and ochre statuses retain the Monokai character without competing with the timer.

| Source role | Before | Current |
|---|---|---|
| WindowBackground | `#272822` | `#272822` |
| SettingsBackground | `#1E1F1C` | `#272822` |
| InputBackground | `#3E3D32` | `#35362F` |
| PrimaryText | `#F8F8F2` | `#F8F8F2` |
| SecondaryText | `#CFCFC2` | `#BCBCAE` |
| AccentPrimary | `#FD971F` | `#E9AD68` |
| InputBorder | `#75715E` | `#5D5F50` |
| TabSelectedBackground | `#FD971F` | `#E9AD68` |
| TabSelectedText | `#272822` | `#272822` |
| TimerText | `#66D9EF` | `#F2E9D5` |
| ButtonNormal | `#A6E22E` | `#E9AD68` |
| ButtonHover | `#B7F33F` | `#F4C38B` |
| ButtonPressed | `#8FBF1D` | `#CD9252` |
| SuccessColor | `#A6E22E` | `#B5CC7D` |
| WarningColor | `#E6DB74` | `#D9C27A` |
| DangerColor | `#F92672` | `#ED829A` |

Shells, fields, text levels, and interaction states use the palette described above. Hover and pressed colors stay in the accent family. Severity colors remain distinct and notifications spell out warning/error.

### Solarized Dark

Deep blue-green surfaces, pale mineral text, and sea-glass teal emphasis. Match desktop and widget accents; use muted coral, olive, and gold statuses.

| Source role | Before | Current |
|---|---|---|
| WindowBackground | `#002B36` | `#002B36` |
| SettingsBackground | `#073642` | `#002B36` |
| InputBackground | `#073642` | `#073642` |
| PrimaryText | `#93A1A1` | `#D3DFDC` |
| SecondaryText | `#657B83` | `#A0B5B5` |
| AccentPrimary | `#268BD2` | `#83C7BD` |
| InputBorder | `#586E75` | `#45676E` |
| TabSelectedBackground | `#268BD2` | `#83C7BD` |
| TabSelectedText | `#000000` | `#002B36` |
| TimerText | `#2AA198` | `#83C7BD` |
| ButtonNormal | `#859900` | `#83C7BD` |
| ButtonHover | `#9FB300` | `#A2DBD1` |
| ButtonPressed | `#6B7A00` | `#60ADA3` |
| SuccessColor | `#859900` | `#ADC184` |
| WarningColor | `#B58900` | `#D8BC7F` |
| DangerColor | `#DC322F` | `#DF8D87` |

Shells, fields, text levels, and interaction states use the palette described above. Hover and pressed colors stay in the accent family. Severity colors remain distinct and notifications spell out warning/error.

### Nord

Keep the polar slate and snow base. Use one frost-cyan accent across desktop and widget controls, with quieter blue-gray secondary text and lifted fields.

| Source role | Before | Current |
|---|---|---|
| WindowBackground | `#2E3440` | `#2E3440` |
| SettingsBackground | `#3B4252` | `#2E3440` |
| InputBackground | `#3B4252` | `#3B4252` |
| PrimaryText | `#ECEFF4` | `#ECEFF4` |
| SecondaryText | `#D8DEE9` | `#B3C1D4` |
| AccentPrimary | `#5E81AC` | `#88C0D0` |
| InputBorder | `#4C566A` | `#56647B` |
| TabSelectedBackground | `#5E81AC` | `#88C0D0` |
| TabSelectedText | `#10151D` | `#10151D` |
| TimerText | `#88C0D0` | `#88C0D0` |
| ButtonNormal | `#A3BE8C` | `#88C0D0` |
| ButtonHover | `#B1CC9D` | `#ADD4DF` |
| ButtonPressed | `#8FA876` | `#74ADBE` |
| SuccessColor | `#A3BE8C` | `#A3BE8C` |
| WarningColor | `#EBCB8B` | `#EBCB8B` |
| DangerColor | `#BF616A` | `#BF616A` |

Shells, fields, text levels, and interaction states use the palette described above. Hover and pressed colors stay in the accent family. Severity colors remain distinct and notifications spell out warning/error.

### Dracula

Deep violet-gray surfaces, pale lilac timer text, and lavender controls. Lower field brightness and soften rose, mint, and butter-yellow statuses to reduce competing emphasis.

| Source role | Before | Current |
|---|---|---|
| WindowBackground | `#282A36` | `#282A36` |
| SettingsBackground | `#44475A` | `#282A36` |
| InputBackground | `#44475A` | `#343746` |
| PrimaryText | `#F8F8F2` | `#F8F8F2` |
| SecondaryText | `#BFBFBF` | `#BEB8D3` |
| AccentPrimary | `#BD93F9` | `#BD9CE8` |
| InputBorder | `#6272A4` | `#595D7A` |
| TabSelectedBackground | `#BD93F9` | `#BD9CE8` |
| TabSelectedText | `#282A36` | `#282A36` |
| TimerText | `#8BE9FD` | `#DDD0F3` |
| ButtonNormal | `#50FA7B` | `#BD9CE8` |
| ButtonHover | `#6BFB8C` | `#D5BAF7` |
| ButtonPressed | `#3FE96A` | `#A583D0` |
| SuccessColor | `#50FA7B` | `#9BD3AF` |
| WarningColor | `#F1FA8C` | `#E3D69C` |
| DangerColor | `#FF5555` | `#ED8FAD` |

Shells, fields, text levels, and interaction states use the palette described above. Hover and pressed colors stay in the accent family. Severity colors remain distinct and notifications spell out warning/error.

### High Contrast

Retain black/white structure, cyan emphasis/timer, yellow labels, and explicit red/green/yellow statuses. Strong distinctions are intentional; no source color changed.

| Source role | Before | Current |
|---|---|---|
| WindowBackground | `#000000` | `#000000` |
| SettingsBackground | `#000000` | `#000000` |
| InputBackground | `#000000` | `#000000` |
| PrimaryText | `#FFFFFF` | `#FFFFFF` |
| SecondaryText | `#FFFF00` | `#FFFF00` |
| AccentPrimary | `#00FFFF` | `#00FFFF` |
| InputBorder | `#FFFFFF` | `#FFFFFF` |
| TabSelectedBackground | `#00FFFF` | `#00FFFF` |
| TabSelectedText | `#000000` | `#000000` |
| TimerText | `#00FFFF` | `#00FFFF` |
| ButtonNormal | `#00FF00` | `#00FF00` |
| ButtonHover | `#00FF00` | `#00FF00` |
| ButtonPressed | `#008000` | `#008000` |
| SuccessColor | `#00FF00` | `#00FF00` |
| WarningColor | `#FFFF00` | `#FFFF00` |
| DangerColor | `#FF0000` | `#FF0000` |

Backgrounds, text levels, borders, and selection values not changed here were retained deliberately. Hover and pressed colors stay in the same accent family. Severity colors remain distinct and notifications spell out warning/error.

## Adjusting one preset later

1. Change only that preset in `ThemeService.CreateBuiltInThemes`, or select it in Appearance and edit its supported Widget colors and Dialogs & co. colors. Import/export preserves all existing Theme fields, including opacity and backdrop values.
2. Tune backgrounds and text levels together, then accent, borders, selection, timer, and icon states. `PlayPauseColor` is optional and inherits `ButtonNormal` when absent. `SuccessColor`, `WarningColor`, and `DangerColor` are code-owned source roles with no new editor controls in this change.
3. Run Core theme serialization tests, App contrast/settings tests, and headless rendered control tests. Review Full/Compact, Settings, Worklog, the picker, and notifications with focus, selection, hover, pressed, and open popups. Text needs 4.5:1; meaningful indicators need 3:1 after compositing.
4. Apply/OK commits the source snapshot. Cancel or close restores the last successful commit. Reopen, restart, and export must preserve edits. Explicitly selecting a different preset and returning, or Reset, opts into current factory colors.

ThemeManager derives opaque desktop fields, matched selection pairs, action fills, focus, and readable text. Infeasible shared surfaces are normalized toward the desktop shell; source fields, widget colors, and opacity remain unchanged. Do not move authored palette choices into styles or relax contrast thresholds to change personal taste.

## Imported preset identity

A current custom draft adds `Custom/Imported` to the dropdown's stable list of choices. Selecting a preset updates the visible selection and active draft together; appearance refreshes keep the same list. New imports save `ActiveThemeName = Custom`; `Theme.ThemeName` and author/version remain import metadata. `CustomThemePath` records provenance and is never needed to reopen or restart.

Legacy snapshots with a nonempty path and matching active/metadata names are treated as custom, including imports named Dark. A stale matching path is ambiguous, so the saved palette wins. Unknown names without a path also become custom. This normalization is in memory until an ordinary successful commit; opening Settings does not save. A known preset with mismatched metadata uses the existing factory repair and clears its stale provenance so future restore/startup stays consistent.

Explicit preset selection or Reset clears provenance. Color and opacity edits to a built-in retain its identity and saved snapshot. There is one current custom draft, not a library: switching to a factory preset removes the custom entry; Cancel can restore the previously committed custom snapshot.
