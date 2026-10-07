# Design

## Context

The user selected dense frost, compact rows with cards for multiple useful groups, native clarity typography, the uploaded icon-and-underline navigation, and soft-rounded controls. The [reference gallery](references/README.md) records the original screens and target compositions. Theme colors are outside this proposal.

FocusTimer already has palette, semantic, and component resources in `FocusTimer.App/Styles/`. Settings uses a shared draft with immediate appearance preview, transactional Apply/OK, commit input shielding, and recovery handling. Worklog shares the `settings-window` class, so changing global selectors can affect both windows. Rule editors and dialogs also need consistent controls.

The app uses Avalonia 11.2.0 and already references Material.Icons.Avalonia. `FocusTimer.App` does not reference `FocusTimer.Platform.Windows`; Host owns platform DI. The timer widget currently offers Off/Solid backdrop choices. Earlier native widget blur did not visibly work, so a successful compositor call cannot establish that the selected frost exists.

The implemented `worklog-data-management` change moved Summary into Worklog. Its remaining manual verification is independent. This proposal uses five Settings tabs; it does not repeat that move or close the active change.

## Goals / Non-Goals

**Goals**

- Deliver one coherent visual language across Settings, Worklog chrome, and their app-owned editors.
- Keep compact rows readable, use cards only for meaningful peer groups, and repair clipping at supported sizes.
- Match the uploaded navigation structure, including outlined icons, labels, baseline, and selected underline.
- Establish a verified Windows frost path with a solid accessible fallback.
- Preserve theme-file compatibility, existing controls, keyboard access, and all save/preview/recovery behavior.

**Non-goals**

- Palette selection, contrast-driven palette redesign, new presets, or serialized theme/settings fields.
- Broader widget typography, backdrop choices, or native widget blur. Requested widget corrections retain the full-mode project-field gap and centered text, put compact buttons in a vertical column at every scale, balance visible side gaps, remove duplicate window chrome, and shrink shell corners below 1x while capping them at 12 px above it. Placing the whole button group below the clock is backlog item OI-35.
- New Worklog charts, table columns, summary metrics, timeline interaction, or data-management behavior.
- Hotkey editing/recording, immediate commits for nonappearance settings, custom window decorations, or a framework upgrade.

## Decisions

### 1. Extend the shared styles with desktop scope

Keep `Tokens.axaml`, `ControlStyles.axaml`, and `ThemeResources.axaml` authoritative. Add desktop-specific component tokens and classes to existing resources. Apply them to Settings, Worklog, the color picker, and Worklog entry editors. Do not broaden selectors onto the timer widget or native file dialogs/tray menus.

Move relevant Worklog-local control overrides into the shared source before applying the new family. Preserve platform-native window decorations and their resize/close behavior; the uploaded header is a composition reference, not a request for a replacement title bar.

| Element | Initial geometry target | Application |
|---|---|---|
| Outer content padding | 16 px minimum; 24 px where space permits | Settings/Worklog content bounds |
| Row gap / section gap | 8 / 24 px | Compact forms and section separation |
| Field and button height | 25 px | Desktop inputs and actions; grow for wrapping/content |
| Control / card radius | 8 / 12 px | Soft-rounded controls and peer-group cards |
| Tab row / icons / underline | 44 / 18 / 3 px (2 px baseline) | Labeled navigation and selected indicator |
| Page / section / body / helper text | 24 / 18 / 14 / 12 px | Shared hierarchy, normal body weight, semibold headings |
| Minimum icon hit target | 24 by 24 px | Rule actions and secondary tools; prefer 32 px when space permits |

Use existing spacing/radius/font-size scale values wherever possible. These are logical pixels and implementation starting points, not theme-editable values. Use Segoe UI for desktop views with a system sans-serif fallback; preserve `TabularTimerFontFamily` and widget typography. Use tabular numerals where supported and right-aligned numeric columns otherwise. Do not add font packages.

This keeps the existing token architecture and avoids a parallel collection of view-local styles. Fully replacing Fluent controls or adding another UI framework would increase maintenance without improving this proposal's scope.

### 2. Use one frost layer and qualify native rendering first

Settings and Worklog use a dense-frost outer client surface with an approximately 92–97% tint as a starting target. The result should read as a near-solid window with subtle background softening. Text, fields, cards, and dense Worklog content remain sharp. Use a fine edge and restrained depth; do not blur foreground controls or stack translucent cards.

Derive material brushes from existing semantic Settings/window resources. Keep this treatment independent of widget background, clock, control, and overall fade values. Do not change the stored palette or expose a new backdrop setting.

Start with the pinned Avalonia window material APIs. Current [Avalonia window documentation](https://github.com/avaloniaui/avalonia-docs/blob/main/docs/how-to/window-how-to.md) describes a transparent window background, requested transparency levels, and a fallback brush. [TopLevel documentation](https://github.com/avaloniaui/avalonia-docs/blob/main/api/avalonia/controls/toplevel.mdx) distinguishes the requested level from the actual level. [Windows guidance](https://github.com/avaloniaui/avalonia-docs/blob/main/docs/platform-specific-guides/windows.md) describes platform and system-setting restrictions. Verify API availability against 11.2.0 before use; these links are current documentation, not proof of this project's runtime behavior.

Qualification sequence:

1. Render a Settings shell using the existing Avalonia material path on Windows, over a detailed desktop pattern. Capture requested/actual effect state and a native screenshot with the effect active.
2. Capture the same shell using the solid fallback. The comparison must visibly demonstrate softened background detail with sharp foreground content. Browser screenshots only show the target composition.
3. Check High Contrast, reduced transparency, effect unavailable/disabled, and window creation/close/theme-switch lifecycle. Fallback must retain dimensions and readable controls.
4. If Avalonia cannot produce verified frost, isolate a Windows adapter behind a minimal platform-neutral contract in the existing Core abstractions and wire it in Host with the existing unsupported-platform pattern. Keep native calls in `Platform.Windows`; do not reference it from App or capture the desktop.
5. If neither path produces visible frost, record the limitation and keep the solid path. Do not mark dense-frost acceptance or the implementation complete based only on an API result.

Use actual effect state for runtime fallback and react to supported platform setting changes. Avoid material animation; reduced-motion mode must not introduce an animated transition. A widget blur implementation is a separate OI-28 decision.

### 3. Make rows the default and cards conditional

| Surface | Proposed organization | Preserved behavior |
|---|---|---|
| General | Startup, Window, and Break reminders as compact sections with restrained dividers | Existing checkbox values, reminder enablement/dependencies, interval validation |
| Logging | Work logging and storage/retention rows; project rules as a labeled list | Read-only path plus Browse, ordered rule actions, first-match semantics |
| Appearance | Theme tools; two peer cards for Widget layout and Widget opacity; palette groups below | All presets/tools, Off/Solid, all opacity layers, scale, compact mode, diagnostics, every color editor |
| Hotkeys | Labeled, consistently rounded read-only fields | Display-only shortcuts and existing help text |
| About | App/version/link content, then unlocked Developer options and rule lists | Seven-click unlock, log level, polling, exclusions and segmentation |
| Worklog | Restyled date toolbar, tabs, existing data content and app-owned editors | Entries/Timeline/Summary contents, F5 and editing/data-management behavior |

Appearance cards group multiple related controls; neither an individual slider nor every General section receives a card. At approximately 560 px of available content width, two cards may sit side by side. Below that they stack with the same compact internal rows. Judge fit from content bounds rather than physical monitor pixels.

Keep theme selection and import/export/reset discoverable above the cards. Palette sections can use disclosures to reduce initial density, but labels and edited values remain accessible. A collapsed group containing an error must indicate that error and expand/focus it when validation requires correction. Keep existing diagnostics/help available and distinguish widget settings from desktop material treatment.

Use flexible label columns with wrapping or stacked labels at narrow widths instead of the current fixed 140 px opacity label column. Give rule fields persistent Application pattern, Window title pattern, and Project labels, plus accessible names for reorder/remove buttons. Rule lists may stack per-row fields at the minimum width; action controls must remain attached to their row. Preserve the source control inventory rather than treating illustrative prototype fields as new features.

### 4. Keep native TabControl behavior with icon headers and underlines

Retain TabControl selection and content behavior. Replace header content with an existing Material.Icons outlined glyph and a text label, and provide explicit accessible names. Settings order becomes General, Logging, Appearance, Hotkeys, About. Worklog retains Entries, Timeline, Summary.

The active tab has a 2 px underline on a subtle shared baseline and semibold label/icon treatment. It does not gain a large selected fill. All states reserve the same underline space to prevent movement. Keyboard focus remains a distinct visible outline rather than sharing the selected underline.

Prefer fitting all five Settings labels at 500 logical pixels with compact header padding. If increased text sizing makes that impossible, allow horizontal tab scrolling with the selected/focused tab brought into view. Do not truncate labels, wrap them into multiple rows, or replace the navigation with a sidebar.

Theme roles keep their current values and serialization:

| Existing role | New visible use |
|---|---|
| Tab background | Not drawn: tabs sit on the window background, so the strip matches the window (the role stays in the theme file) |
| Tab hover background | Hover feedback |
| Tab text | Normal label and icon |
| Tab selected background | Selected underline |
| Tab selected text | Text drawn on the selected-background fill, such as the selected Worklog row; not the tab strip |
| Accent | Selected label and icon, adjusted only as far as needed to reach 4.5:1 on the Settings background |
| Input focus border | Not used by desktop controls: focus uses the accent, lightened or darkened only as far as needed to reach 3:1 |

Built-in themes chose `TabSelectedText` for text on the old selected fill (for example near-black on Monokai orange, white on Light blue). On the plain tab surface that value is unreadable in six of the seven built-ins, so the selected label and icon are colored by the theme accent instead, mixed toward white or black in small steps only when the accent alone is below 4.5:1 on the Settings background (tabs have no fill of their own). The adjustment is derived at runtime (`TabSelectedLabelBrush` in `ThemeManager`, `ThemeContrast` helper); stored theme values, the theme-file schema, and the underline role are unchanged. The selected label stays semibold, and the underline already marks selection without color alone.

Update tests that assume About is index 3 or that `Header.ToString()` is the tab label. Assert stable names/selected content instead of depending on header visuals. Reordering must preserve the developer unlock and valid focus restoration after commits.

### 5. Restyle controls without changing their contracts

Use one moderately rounded family for buttons, text fields, read-only fields, numeric inputs/spinners, dropdowns and popup items, checkboxes, sliders, and disclosure headers. Retain existing control types and commands. Provide shared normal, hover, pressed, disabled, invalid, and focus states, including readable selection/caret and popup states.

Keep OK, Apply, Cancel labels, order, commands, and transaction semantics. Keep the reserved feedback region and commit input shielding, including IME/paste, open popup/editor handling, deferred results, and focus restoration. Do not implement locking by reducing opacity or globally applying disabled styling to the draft.

Place feedback left of the action buttons in the same footer row. Saving status fits the normal action height; long errors and retry actions wrap and scroll within a 120 px maximum feedback height. Keep actions pinned at the bottom so feedback does not move them.

Use a scrollable content region with fixed reachable navigation/footer at Settings 640 by 540 default and 500 by 400 minimum. Worklog retains 900 by 620 default and 640 by 560 minimum. Validate logical bounds at common Windows display scales rather than shrinking controls below accessible hit targets.

## Risks / Trade-offs

| Risk | Mitigation / acceptance condition |
|---|---|
| Native blur may be unavailable or visually ineffective | Qualify Avalonia first, compare native active/fallback captures, use an isolated adapter only if necessary, record unresolved support rather than claiming completion |
| Dense tint hides the blur | Tune tint using a detailed desktop comparison while preserving readability; references communicate composition, not exact compositor strength |
| Shared selectors alter widget controls | Scope desktop selectors explicitly and run existing Full/Compact theme/opacity checks |
| Cards consume too much width | Use only the Appearance peer groups by default, compact rows inside, stack below available-width threshold |
| Existing custom themes lose visible selected colors | Map the existing selected-background role to the underline and test a value different from AccentPrimary |
| Header/content reorganization breaks focus or save shielding | Preserve bindings/names used by SettingsWindow code-behind and run delayed-save, failure, recovery, and focus tests |
| Theme colors chosen later are unreadable | Keep semantic roles and existing contrast acceptance; report failing palettes for the separate theme work rather than silently changing them here. The selected tab label is the one derived exception (accent, contrast-adjusted) because built-in `TabSelectedText` values target the old fill |
| F03 archive overwrites enhanced Settings Tabs | Reconcile the overlapping requirement when syncing/archiving either change; retain the Worklog reporting split and this change's navigation when implemented |

## Migration Plan

No settings or `.fttheme` migration is required. Existing values, widget Off/Solid behavior, and theme import/export stay intact.

Implement the material qualification first, then shared tokens/styles, navigation, page layouts, and dialog adoption. Keep the material path replaceable so failed qualification does not force a view rewrite. Synchronize the design-system/theming/Settings deltas only after implementation and verification.

Correct the main Settings specification's stale Summary tab now to the already-implemented F03 baseline; keep new icon/underline and layout requirements in this change until implementation. Link the proposal from OI-21/OI-24 and retain OI-25/OI-28 as open widget work. Do not archive `worklog-data-management` or check off its remaining manual walkthrough as part of this proposal.

## Validation and Reference Evidence

- Use the existing Settings view-model, theme, Worklog, editor, and native appearance suites for behavior and resource regressions. Add targeted assertions for new tab names/icons/underlines, persistent rule labels, selected-role mapping, and fallback transitions.
- Manually inspect all five Settings pages and all three Worklog pages at default/minimum sizes, common display scales, keyboard focus, open popups, validation, saving, ordinary failure, and recovery states.
- Check all built-in themes for text/focus/selection readability without changing their palettes. Verify imported theme round trips and theme changes while a field remains focused.
- Capture native before/after screenshots for the ten original surfaces, plus material active/fallback and key minimum-size layouts. Store the resulting evidence with implementation verification; the proposal screenshots are labeled browser mockups.
- Keep Full/Compact widget layout, controls, foreground opacity, Off/Solid choices, theme behavior, and tray operation unchanged, apart from the two round-4 spacing exceptions listed under Non-Goals.
