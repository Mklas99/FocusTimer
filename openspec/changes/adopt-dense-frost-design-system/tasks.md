# Tasks

## 1. Native material qualification and fallback

- [ ] 1.1 Prototype the Settings dense-frost shell with the pinned Avalonia 11.2.0 APIs; verify with native active/fallback screenshots over detailed desktop content and record requested/actual material state.
- [ ] 1.2 If Avalonia cannot visibly produce frost, implement an isolated `Platform.Windows` adapter with a minimal platform-neutral contract, unsupported-platform fallback, and Host DI; verify dependency direction and native screenshots, or document why this conditional adapter is unnecessary.
- [x] 1.3 Implement Settings/Worklog material lifecycle and solid fallback for unavailable effects, reduced transparency, and High Contrast; verify fallback-transition tests and native open/close/theme-switch checks without changing layout or widget backdrop behavior.
- [ ] 1.4 Record the qualified backend, tested Windows conditions, limitations, and native evidence in this change's verification notes; verify that no blur acceptance is marked complete solely from an API result or browser mockup.

## 2. Shared desktop tokens, typography, and control styles

- [x] 2.1 Add scoped desktop geometry and Segoe UI typography resources in `Tokens.axaml` and consume them through the authoritative styles; verify page/section/body/helper hierarchy and stable numeric alignment in Settings/Worklog while widget font resources remain unchanged.
- [x] 2.2 Implement shared soft-rounded buttons, text/read-only fields, numeric spinners, dropdowns/popups, checkboxes, sliders, and disclosure controls in `ControlStyles.axaml`; verify normal, hover, pressed, disabled, invalid, checked, selection/caret, and keyboard-focus states in the existing native appearance harness.
- [x] 2.3 Scope the new styles to desktop windows/editors and consolidate conflicting Worklog-local styles into the shared source; verify existing `WorklogWindowThemeTests`, theme tests, and both widget modes without selector leakage.
- [x] 2.4 Preserve semantic role wiring in `ThemeResources.axaml`/`ThemeManager.cs` and keep material brushes independent of widget opacity values; verify theme resource tests, unchanged serialized fields, and the existing Full/Compact foreground-opacity tests.
- [x] 2.5 Document the desktop typography/control/material boundaries in the appropriate root architecture/design documentation; verify the documented ownership and theme boundaries match the resulting styles and DI paths.

## 3. Icon-and-underline navigation and theme compatibility

- [x] 3.1 Update Settings TabControl headers to existing outlined Material icons plus labels and reorder them General, Logging, Appearance, Hotkeys, About; verify accessible tab names, correct selected content, keyboard switching, and the seven-click About unlock.
- [x] 3.2 Apply the same header/underline treatment to Worklog while retaining Entries, Timeline, Summary order; verify existing selection, shared day state, F5 behavior, and accessible tab identities.
- [x] 3.3 Map existing tab colors to normal/hover states and the selected underline, and color the selected label/icon with the contrast-adjusted accent; add targeted `SettingsThemeColorTests` assertions and verify a selected-background color different from AccentPrimary is rendered and survives live preview/import/export.
- [x] 3.4 Replace tests/locators that assume About index 3 or string-valued headers and cover distinct keyboard focus plus selected underline; verify the updated Settings/Worklog theme and native appearance tests pass at default and minimum widths.
- [x] 3.5 Document the selected-background-to-underline role mapping and unchanged theme-file contract; verify all existing built-in/imported values retain visible roles without a schema migration.

## 4. Settings organization and supported-size layout

- [x] 4.1 Restructure General and Logging into compact sections and flexible rows, retaining every control/binding and Browse/retention behavior; verify an inventory comparison and native default/minimum-size screenshots with no clipped fields or labels.
- [x] 4.2 Reorganize Appearance into theme tools, Widget layout/Widget opacity peer cards, palette groups, and available diagnostics/help; verify side-by-side/stacked card layouts, every existing editor, live preview, Off/Solid, scale, compact mode, and all opacity values.
- [x] 4.3 Add persistent labels and accessible action names to `ProjectRuleListEditor` and `WindowRuleListEditor`, with narrow-width reflow; verify populated fields remain identifiable and add/edit/reorder/remove plus validation retain their existing draft semantics.
- [x] 4.4 Restyle Hotkeys and About/Developer content while preserving read-only shortcuts, log level, polling constraints, developer unlock, and rule-list visibility; verify the existing Settings view-model tests and native content checks.
- [x] 4.5 Keep navigation/footer reachable around vertically scrolling content, preserve OK/Apply/Cancel commands and reserved feedback, and expose errors in collapsed groups; verify minimum-size reachability and existing delayed-save, IME/paste/input-shielding, failure/recovery, Cancel/close, and focus-restoration checks.
- [x] 4.6 Update OI-21/OI-24 documentation with implemented layout/control decisions and outstanding palette work; verify no remaining optional behavior, widget blur, or F03 manual walkthrough is marked delivered by this refresh.

## 5. Worklog chrome and app-owned editors

- [x] 5.1 Apply shared spacing/type/control resources and qualified material to the Worklog toolbar and content container; verify Entries, Timeline, and Summary remain readable at 900 by 620 and 640 by 560 logical pixels without changing data or presentation structure.
- [ ] 5.2 Adopt the shared control family in the color picker and Worklog entry forms; verify existing `WorklogEntryFormTests` and pointer/keyboard native checks for validation, save/cancel, popup placement, and focus return.
- [x] 5.3 Run the relevant Worklog theme/timeline/form tests and document the retained data-view boundaries; verify no new grouping, chart, column, timeline interaction, or data-management behavior was introduced.

## 6. Integration acceptance and specification reconciliation

- [ ] 6.1 Perform the combined native visual review across five Settings tabs, three Worklog tabs, app-owned dialogs, and both widget modes at default/minimum sizes and 100%, 125%, 150%, and 200% Windows display scaling; deliver annotated before/after captures and record any environment limitations.
- [ ] 6.2 Verify all seven built-in themes and an imported theme through pointer/keyboard states, contrast checks, preview, Apply/OK, Cancel after Apply, ordinary failure, and recovery; record results without changing palette values as part of this change.
- [x] 6.3 Reconcile the overlapping Settings Tabs delta with `worklog-data-management` before syncing specifications, then run `openspec validate adopt-dense-frost-design-system --strict`; verify the five-page Settings baseline, Worklog Summary split, and implemented navigation survive either archive order.
- [ ] 6.4 Run the repository-required build/test/format checks appropriate to the final changed files and complete the verification record; verify dense-frost native evidence is present, no required check remains unresolved, and OI-25/OI-28/widget blur and the F03 walkthrough retain their separate status.

## 7. Review round 3 (user feedback on the running app)

- [x] 7.1 Indent every option of non-card sections under its header: fix the checkbox margin that overrides the indent, indent the project/window rule editor bodies (not their headers), and keep the Appearance theme row flush with the cards.
- [x] 7.2 Drop the repeated "# Changelog" heading line from the About changelog text.
- [x] 7.3 Repair the accordion (Developer Options, diagnostics): replace the Fluent header chrome that shows black blocks with one template on the field surface, opaque hover, rotating chevron.
- [x] 7.4 Repair the Calendar date-picker popup so its header, day grid, and arrows use the desktop theme roles instead of Fluent defaults; verify by capturing the open popup on screen.
- [x] 7.5 Give every Settings and Worklog tab page the same left and right content inset so the tab baseline overhangs the content slightly.
- [x] 7.6 Remove the Success, Warning, and Danger color editors (theme values stay; they will be tuned per theme in code).
- [x] 7.7 Tighten the label-to-slider distance and shrink the slider thumb to a 6 px radius.
- [x] 7.8 Empty rule rows: a fully blank row no longer blocks OK/Apply, is marked in the warning color as "removed on apply", and is pruned after a successful commit; a partly filled invalid row stays blocked and is marked in the danger color. Cover rule lists with tests.
- [x] 7.9 Verify with the unit, headless, and native suites plus on-screen captures of the popups; update specs and docs; then tick this section.

## 8. Review round 4 (user feedback on the running app)

- [x] 8.1 Worklog: replace the Timeline "Ctrl + mouse wheel zooms the hours" caption with an info icon that shows the hint on hover.
- [x] 8.2 Worklog: one icon-only Refresh button at a fixed place on the right of the day row for every tab; remove the per-tab Refresh buttons.
- [x] 8.3 Worklog: rework the day-row arrows and the calendar popup arrows and look (icons instead of text glyphs, consistent size); check the result on screen.
- [x] 8.4 Worklog: drop the repeated day heading from the Summary tab (the day is shown above the tabs).
- [x] 8.5 Worklog: make the Entries table header stand out a little (semibold, subtle band, divider) without becoming heavy.
- [x] 8.6 Settings: shorten the distance between the sliders.
- [x] 8.7 Color picker: the Hex/R/G/B labels use the same look as the other field labels.
- [x] 8.8 Settings: fix the inverted opacity slider ("Overall fade" grows with opacity): rename it so a fuller bar always means more opaque.
- [x] 8.9 Settings: show the Appearance helper text above the opacity accordion as an info icon with a hover tip at the matching place.
- [x] 8.10 Settings: make the rule Up/Down/Remove buttons lighter and smaller.
- [x] 8.11 Widget: more space between the Project label and its text box; center the text vertically.
- [x] 8.12 Widget compact mode: less space between the buttons, slightly smaller icons, and a click target that uses the top and bottom padding.
- [x] 8.13 Backlog: record the option to place the widget buttons beside or below the clock (OI-35).
- [x] 8.14 Verify with unit, headless, and native suites and on-screen captures; update specs and docs; then tick this section.
