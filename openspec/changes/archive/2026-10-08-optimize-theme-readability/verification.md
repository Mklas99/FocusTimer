# Theme readability verification

## Native review completion, 2026-10-08

On 2026-10-08, the user confirmed that the interactive review and verifications can be closed: "everything fine". This closes the combined native interaction walkthrough, the theme-focused checks across presets and imports, and the Full/Compact widget desktop-content checks. Completion is user-reported; no fresh automated run or additional captures were supplied with this confirmation. Exact build, Windows version, scale and opacity values were not supplied. Existing automated and capture evidence remains recorded below. OI-25/OI-28 widget blur, OI-44, and the separate F-03 and tracking-rule walkthroughs retain their own status.

## Palette polish follow-up, 2026-10-08

Tuned the six everyday factory presets at the user's request. Dark uses blue charcoal and pearl text; Light uses porcelain and sapphire; Monokai uses cream and amber; Solarized uses sea-glass teal; Nord aligns its frost-cyan accents; Dracula uses darker violet-gray fields and quieter lavender, rose, and mint. High Contrast retains its source values. Saved snapshots still require explicit preset selection/reset to adopt new factory colors.

Tested the working tree on HEAD `ebacbec` with pre-existing staged changes preserved. SHA-256 of ThemeService.cs: `B0A51FA51DFAD2EF8537C4F3F010001FC165881D1D400C1ED5E1D38EFCE170DC`.

- Core ThemeService filter: 24 passed, including serialization.
- App ThemeContrastTests, DesktopPaletteContrastTests, ThemeManagerTests, SettingsThemeColorTests filter: 34 passed. The initial Light secondary text failed hovered-tab contrast at 4.40:1; darkening it to `#57667A` resolved the failure.
- Headless RenderedThemeContrastTests, ThemeScreenshotRegressionTests, WorklogWindowThemeTests filter: 12 passed across factory palettes and difficult imports.
- Native DesktopShellEvidenceTests: before and after runs passed across six presets. Captures include Settings/Worklog pages at supported sizes, the picker, notifications, and live theme-switch examples.
- Native DesktopWidgetEvidenceTests: passed for Full/Compact and project rows across six presets.
- Changed C# formatting and whitespace checks passed.
- Full solution build passed with zero warnings/errors. An initial concurrent build hit assembly locks from the screenshot test process; rerunning after it exited passed. The isolated eight-case screenshot regression rerun also passed.

The targeted commands used `dotnet test <project> --no-restore --filter <filters above> --verbosity quiet`. Native fixtures ran separately with `--no-build`, FOCUSTIMER_NATIVE_APPEARANCE_TESTS=1, FOCUSTIMER_EVIDENCE_THEMES set to the six preset names, and FOCUSTIMER_EVIDENCE_DIR under artifacts/theme-polish/before or after. Widget capture also used FOCUSTIMER_THEME_FOCUSED=1 and FOCUSTIMER_EVIDENCE_PROJECT_ROWS=1. Formatting used `dotnet format FocusTimer.sln --no-restore --verify-no-changes --include src/FocusTimer.Core/Services/ThemeService.cs --verbosity quiet`.

Captures and review sheets are under `artifacts/theme-polish/`; regenerated source palettes and contrast measurements are under `artifacts/theme-polish/contrast/`. Reviewed the before/after Settings sheets, Dracula Timeline, Light picker, Solarized warning, Monokai full widget/project row, and Dracula compact widget. Updated ThemePaletteTuning and the contrast audit to match the final values. Fixture captures do not complete the remaining interactive Windows walkthrough or arbitrary desktop-backdrop checks.

## Accent-colored slider thumbs, 2026-10-08

Slider thumb fills now use DesktopSliderThumbBrush, derived from AccentPrimary and adjusted to reach 3:1 against the active track. The existing focus-colored outline remains. The rendered control matrix passed across 12 palettes; it now also checks the actual thumb fill in normal, hover, pressed and focused states against the derived role. Evidence is in `artifacts/theme-readability-followup/slider-accent.log`.

## Theme dropdown selection follow-up, 2026-10-08

A rendered control regression reproduced choosing High Contrast but ending with Dark still active. AvailableThemes allocated a replacement list during selection refresh, allowing the control's previous selection to feed back into the editor. The list is now one ObservableCollection for the editor's lifetime; guarded updates only add/remove Custom/Imported.

All 56 SettingsWindowViewModel tests passed. Five rendered cases passed, covering all seven control-originated preset choices and actual dropdown selection during import, Apply, Cancel/title-bar close, and reopening. Logs are under `artifacts/theme-readability-followup/theme-dropdown*`.

## Accordions, swatches and project suggestions, 2026-10-08

Two new actual-template regressions cover 12 palettes under both Fluent variants. Before the fixes, swatch hover replaced the Dark source `#91D2FF` with white, and the suggestion popup retained Fluent's `#2B2B2B` instead of the app shell. Accordion checked/hover/pressed text now explicitly uses the derived desktop foreground; its chevron is explicit too. A dedicated SwatchSurface presenter preserves the bound source fill and uses an outline for hover/pressed/focus feedback. AutoCompleteBox fields and suggestion borders/items now have their own desktop styles, including pressed and selected-unfocused states. Suggestions check actual role colors, placeholder contrast and live switching while the popup stays open.

The affected screenshot, rendered-theme and navigation tests passed: 28 tests. Both final cases passed again after adding exact item-role, placeholder and live-switch assertions. Changed test formatting passed, as did strict OpenSpec validation and whitespace checks. Native Light/Dark surfaces passed under both Fluent variants; expanded headers, hovered swatches and normal/selected suggestions were visually reviewed. Final focused checks and native captures are under `artifacts/theme-readability-followup/accordion-suggestions*`. One initial native Dark image was obscured by another application and was discarded; the fixture now keeps its temporary evidence window on top while capturing. Its focused capture mode is `FOCUSTIMER_EVIDENCE_THEME_SURFACES_ONLY=1`.

## Calendar columns and day backgrounds, 2026-10-08

Replaced Fluent's CalendarItem layout with a compact template retaining its named navigation buttons, populated month/year grids, and empty month backdrop. The popup fits seven date columns, and the separator sits directly below the month navigation. Current-month cells use DesktopCardBrush; adjacent-month cells retain DesktopShellBrush, with distinct hover and selected fills.

The six screenshot regressions passed; final focused checks cover trailing horizontal/vertical space, separator placement, shaded/hovered/selected days, previous-month navigation, and opening the year grid from the header. Native Light/Dark captures under both Fluent variants passed and were reviewed. Artifacts are under `artifacts/theme-readability-followup/calendar-columns*`.

## Compact calendar and date-field follow-up, 2026-10-08

Removed Fluent's month-grid minimum height so the popup fits its date rows. The regression checks the actual grid's trailing space, not only its MinHeight property. The date picker now owns the complete field background and outer border through hover, pressed and focus-within states; its embedded text box stays transparent and uses the shared selection/caret brushes. A new rendered case checks actual TextPresenter selection contrast across all 12 palettes and both Fluent variants.

All six screenshot regressions passed. Native Light/Dark evidence includes selected date fields and calendar popups under both Fluent variants, plus a fully available six-week month. Logs and captures are under `artifacts/theme-readability-followup/calendar-compact-field*`. The earlier full-suite results apply to their earlier tested trees.

## Calendar framing follow-up, 2026-10-08

Added a rounded, clipped calendar outline using DesktopCardRadius and DesktopBorderBrush, plus a divider below the weekday headings. The outline and divider are checked at 3:1 across the existing 12-palette/two-Fluent-variant matrix. Fluent binds its month backdrop and year grid to BorderBrush; activated selectors keep those backgrounds on DesktopShellBrush so the new outline color does not fill the date area. Native popup assertions now include the radius, clipping, outline and separator.

Validation and reviewed captures are recorded under `artifacts/theme-readability-followup/calendar-styling*`. All five screenshot regressions passed; the final calendar-only case also passed after adding month/year background assertions and the small inset protecting the outline. The Windows popup fixture passed for Light/Dark under both Fluent variants. Changed test formatting passed. Earlier full-suite results below belong to the earlier tested tree.

## Screenshot follow-up, 2026-10-08

User screenshots exposed gaps in the earlier audit. Parent/control brushes passed while actual template glyphs and watermarks did not; the original popup test also measured item text without checking the outer surface or cold construction under a different Fluent variant. The earlier evidence therefore did not establish complete template coverage.

Added `ThemeScreenshotRegressionTests` with five cases covering all seven presets and five difficult imports under forced Fluent Light/Dark variants. They inspect actual spinner Paths, watermark TextBlocks, popup border and text, selected/hover dropdown items, open-popup live switching, calendar navigation strokes, and tooltip text after the opening animation. The expanded widget project row also covers a saved Light snapshot with its old pale secondary text. Foreground checks composite brush alpha and each completed child group at its visual/ancestor opacity, including fading the project row over the shell. Enabled text uses 4.5:1; enabled meaningful glyphs use 3:1. Disabled month arrows remain visibly dimmed, with a regression minimum of 1.5:1; this is an app-specific visibility check, not the enabled-control threshold.

Reproduced before correction:

| Consumer | Failure | Correction |
|---|---|---|
| Read-only hotkey watermark | Dark placeholder at 50% opacity, 3.837:1 | Derived secondary text at full placeholder opacity |
| Numeric spinner | Light glyph white on white, 1:1; Solarized pressed glyph 1.779:1 | Reach the nested PathIcon template and use matched desktop normal/hover/pressed surfaces |
| Dropdown popup | Outer PopupBorder retained Fluent's `#2B2B2B` surface instead of the app palette | Activated template selector overrides the Fluent theme surface |
| Calendar month navigation | Enabled Light arrow white on white, 1:1; disabled arrows retained OS variant colors | Reach the arrow in button content, use derived strokes, preserve the open chevron contour, and dim disabled arrows consistently |
| Widget project row | Dim watermark at 4.359:1; saved pale Light label uses the desktop secondary role | Full-opacity project watermark and a separate label derived from widget WindowForeground |

Tooltips passed the expanded checks after advancing the opening animation. Calendar dates outside DisplayDateStart/End intentionally become disabled/hidden in Avalonia; tests preserve the date-range constraint rather than treating these unavailable dates as enabled text. No schema, backdrop, date-selection, or layout behavior was changed.

The native popup fixture now selects the real Off/Solid control, constructs the editor with the requested palette, brings that control into view, runs both Fluent variants, asserts popup/glyph brushes, and captures the actual native popup at its DPI scale. The widget fixture can capture the expanded project row through `FOCUSTIMER_EVIDENCE_PROJECT_ROWS=1`.

Validation artifacts are under `artifacts/theme-readability-followup/`:

- Five new regressions and 18 existing rendered/control/layout tests: 23 passed.
- Full App suite: 411 passed, five opt-in native cases skipped.
- Native popup fixture: passed for Light/Dark palettes under both Fluent variants; eight captures reviewed, including corrected Light Off/Solid text and the calendar arrow.
- Native widget fixture: passed for Light/Dark Full/Compact modes and expanded project rows; six captures, with both project rows reviewed.
- Full headless suite: 115 passed in 5.82 minutes. The subsequent disabled-arrow/chevron check and compositing-helper refinement passed all five new regressions again on the final rendering change.
- Changed C# formatting passed. Final solution build passed with zero warnings and errors. Follow-up file hashes and native capture hashes are recorded alongside the logs.

The complete native interaction and transparent-widget desktop-content walkthrough remains open under tasks 4.2/4.3. These focused regressions and captures do not close that checklist.

## Scope and tested tree

Implemented on 2026-10-08 in the working tree based on `ebacbec5c329f15356f494facdb2154e154c4c82`. Existing staged planning, walkthrough, and native-material work was preserved; nothing was staged or committed. The final file hashes and test artifacts are stored locally under `artifacts/theme-readability/` (ignored by Git).

Environment: Windows NT 10.0.26300.0, .NET SDK 10.0.204, Debug/net8.0, Avalonia 11.2.0. Native evidence uses Windows Skia rendering. No backdrop, platform-service, schema, dependency, or geometry changes were made.

The fixture matrix contains Dark, Light, Monokai, Solarized Dark, Nord, Dracula, High Contrast, and five valid imports: White on white, Pale accents, Opposing surfaces, Translucent, and an author-defined theme named Dark. Imported source values, alpha, opacities, and metadata round-trip without modification.

## Control/state matrix

| Consumer | Coverage and evidence | Remaining native check |
|---|---|---|
| Primary, secondary, destructive buttons | Resolved ContentPresenter text/fill/focus pairs in normal, hover, pressed, focused states; disabled styling and geometry checks retained | Pointer/keyboard walkthrough |
| Text/numeric fields | Actual templates, normal/hover/focus/invalid/read-only states, caret, selected fill/text, selection distinction; switching preserves selection and focus | Native selection, IME, and commit interaction |
| Checked controls | Checkbox label, outline, checked glyph; unchecked/checked/indeterminate states and hover/pressed/focus styles | Native pointer/keyboard transitions |
| Sliders | Both actual track templates, thumb fill/outline, hover/pressed/focus states | Native drag/keyboard interaction |
| Tabs | Normal/hover labels, selected label/underline, focus ring; existing geometry/navigation regressions retained | Native focus with hover |
| Dropdown/date popups | Actual popup roots, item ContentPresenters and enabled calendar TextBlocks, selected/hover styles, switching with both popups open | Native opening, navigation, dismissal |
| Worklog selection/timeline | Actual row presenters/TextBlocks, selected/unselected distinction, live selection retention, tracked/manual block labels | Native selection/keyboard and desktop review |
| Color picker | Numeric/hex fields, labels, preview code chip, actions, including gray and translucent values | Pointer gestures and visual marker review |
| Notifications | All severities and live switching across the matrix; derived heading/body/action/focus pairs; geometry retained | Native display/dismissal and live switching |
| Shell/material | Text and meaningful indicators checked against opaque surfaces and actual 95% tint composited over black and white; notification shell remains solid | Theme-focused native frost interaction |
| Widgets | Source brush preservation; seven-preset Full/Compact Windows bitmap captures and geometry assertions at scale 1 | Real light/dark desktop content, Off/Solid, tested opacity values and High Contrast fallback |

Existing coverage checked individual desktop control states, selected tabs, notification structure, worklog selection, and widget isolation. This change adds the full palette/import matrix, actual popup/calendar text, alpha-composited selection, opposing-surface feasibility, checked/slider states, picker preview text, and real Settings close/reopen import lifecycle. Disabled controls retain established styling; normal-size enabled text requires 4.5:1 and meaningful indicators require 3:1.

## Reproduced failures and fixes

| Failure | Concrete consumer/fix |
|---|---|
| Shared foreground returned a failing black/white endpoint for an infeasible set | `ThemeContrast.TryEnsureContrastAcross` distinguishes failure; desktop surfaces normalize before shared colors are derived |
| A feasible middle-gray foreground was missed for black/white surfaces | Shared derivation searches intermediate candidates rather than only endpoints |
| Translucent selected fill used Settings background instead of its field | Matched opaque `DesktopSelectionBrush`/`DesktopSelectedTextBrush` at TextBox, Worklog rows, and timeline blocks |
| Dropdown template rendered black text on a dark fill (2.301:1) | Explicit popup item presenter text/fill and selected/hover selectors |
| Calendar template retained black text (1.530:1) | Explicit calendar content presenter selectors, including inactive enabled days and selected states |
| Gray picker preview code used white text (3.949:1) | Readable field-colored code chip within the preserved source swatch |
| Pale imported tab focus lost contrast while hovered (1.297:1) | Dedicated `DesktopTabFocusBrush` qualifies against both shell and hover surfaces; color-swatch focus also uses a derived indicator |
| Import dropdown refresh fed old Dark selection back into the draft | Guard programmatic entry/selection notifications; rendered window regression checks import, Apply, Cancel/title-bar close, reopening, and unavailable source file |

The selection/solver regressions failed before their fixes. Actual template tests exposed dropdown, calendar, and picker failures during implementation. Legacy assertions expecting raw source colors were updated to the derived rendering contract. The first new window test harness also stalled on ReactiveUI scheduling and changed UI threads after file I/O; it now pumps the UI dispatcher under an Avalonia synchronization context and uses a persisted snapshot provider. Those harness failures were corrected, not treated as successful validation.

## Validation commands and results

- `dotnet format FocusTimer.sln --no-restore --include <changed C# files> --verbosity minimal`: passed. Included new files; excluded pre-existing staged `DesktopMaterialNativeTests.cs`. Workspace loading emitted an informational warning.
- `dotnet build FocusTimer.sln --no-restore -v minimal`: passed, zero warnings and errors. Sonar integration targets are absent locally; this was not a Sonar analysis.
- `dotnet test tests/FocusTimer.Core.Tests --no-build --no-restore -v minimal`: 339 passed.
- `dotnet test tests/FocusTimer.App.Tests --no-build --no-restore -v minimal`: 411 passed, five opt-in native tests skipped. Native capture fixtures were run separately below.
- `dotnet test tests/FocusTimer.App.HeadlessTests --no-restore -v minimal --blame-hang-timeout 45s --logger "console;verbosity=normal"`: 110 passed in 4.88 minutes. The earlier full attempt was interrupted after stalling; rerunning with progress logging and a hang timeout completed successfully.
- Final targeted follow-up after adding swatch focus and the hovered-tab focus correction: all four rendered/navigation tests and four derived-palette tests passed. One attempted concurrent rebuild failed because the full headless test process held its DLL; retrying after that process ended passed. This was a validation scheduling error, not a product failure.
- `openspec validate --specs`: 21 specifications passed. Both `optimize-theme-readability` and `adopt-dense-frost-design-system` passed strict validation. Existing long-requirement informational notices and Node JSON-module warnings remain.
- `git diff --check`: passed.

TRX results, build logs, derived contrast rows, source snapshots, and capture manifests are under `artifacts/theme-readability/`. [The contrast audit](../../../../design/SETTINGS_THEME_COLOR_CONTRAST_CHECK.md) records current measured minima; [palette tuning](../../../../docs/versions/current/ThemePaletteTuning.md) records each preset's source decisions and how to personalize a saved snapshot.

## Windows palette evidence and limits

`FOCUSTIMER_NATIVE_APPEARANCE_TESTS=1`, `FOCUSTIMER_EVIDENCE_THEMES="Dark,Light,Monokai,Solarized Dark,Nord,Dracula,High Contrast"`, and an evidence directory enable the existing desktop capture fixture. `DesktopShellEvidenceTests` passed for both current and previous source palettes, producing 273 PNGs per set with layout assertions. The previous-source run uses `FOCUSTIMER_EVIDENCE_PALETTES=.../source-palettes-before.json` with the current renderer; it compares factory source harmony, not an untouched historical application binary.

`DesktopWidgetEvidenceTests` passed for both source sets with `FOCUSTIMER_THEME_FOCUSED=1`, Full/Compact modes, and scale 1 (14 PNGs per set). The wider display-scaling review was already confirmed by the user and was not repeated. Overall window and timer opacity are 1. Source backdrop mode is Off, with High Contrast forcing the established solid fallback. The fixture renders bitmaps; it does not exercise real desktop content through transparent Off mode.

| Preset | Background tint opacity | Button opacity |
|---|---:|---:|
| Dark | 0.80 | 0.90 |
| Light | 0.90 | 0.95 |
| Monokai | 0.80 | 0.88 |
| Solarized Dark | 0.80 | 0.90 |
| Nord | 0.80 | 0.89 |
| Dracula | 0.80 | 0.92 |
| High Contrast | 1.00 | 1.00 |

Reviewed all seven Settings General captures and the seven before/current Full widget pairs, plus representative Appearance, picker, Worklog, and notification captures. Dark keeps charcoal/azure, Light uses cool neutrals/blue, Monokai keeps olive/orange with cyan timer text, Solarized keeps deep teal, Nord keeps cool frost, Dracula uses lavender, and High Contrast retains deliberate bright separation. No palette character or status label meaning was lost. Source swatches are in `palette-comparison.svg`; the source-role tables distinguish taste choices from measured defects. Native bitmap inspection is static visual evidence, not native interaction completion.

Tasks 4.2 and 4.3 were open at the time of the capture run and are now closed by the user confirmation above. Their scope was: the theme-focused [native walkthrough](../../../../docs/versions/current/ManualUiWalkthrough.md) for every preset and a difficult import, focused fields/open popups, notifications, picker, commit/discard, and Full/Compact widgets over representative light/dark desktop content with recorded opacity values, Off/Solid behavior, and High Contrast fallback. Native computer interaction is unavailable in this session. No universal contrast guarantee is claimed for freely transparent widgets.

## Documentation and specifications

Reconciled the dense-frost Settings role delta first, retaining all scenarios while clarifying preferred source values versus contrast-adjusted rendered brushes. Synced its theming role requirement into the main spec, then added this change's guarantees. Other dense-frost capabilities were not synced. Updated architecture, development guidance, token mapping, historical audit current-status section, source palette tuning, and native checklist. OI-21/OI-24/OI-29/OI-42 are completed following the user-confirmed native review. OI-44 remains open. The Features collection reflects the same evidence boundary.

## F-03 walkthrough completion, 2026-10-08

The user subsequently confirmed that the separate Worklog data-management walkthrough had already been performed and was fine. Task 10.5 in worklog-data-management is complete, including the carried-over Summary check in Light and High Contrast. This supersedes earlier notes describing the F-03 walkthrough as open. Tracking-rule walkthroughs retain their separate status.
