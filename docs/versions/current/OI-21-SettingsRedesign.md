# OI-21: Settings redesign and theme consistency

## Outcome

Make the Settings window feel like one coherent desktop interface. Tabs, accordions, fields, checkboxes, action controls, and their interaction states should share consistent spacing, typography, geometry, and color roles. Theme colors should behave predictably across Settings, the timer widget, and the context menu without coupling Settings actions to widget button colors. This task does not redesign the widget layout.

## Decisions

These are the target behaviors for OI-21; the current implementation may differ.

The shared draft, full Apply/OK commit, Cancel/title-bar discard, temporary appearance preview, and visible load/commit/recovery errors are implemented by `make-settings-edits-consistent`. An unfinished commit is recovered before startup activates settings; a failed load stops activation and opens Settings for retry. Windows start-on-login registration is compared with the saved value and reconciled on Apply/OK, with the original Run value preserved for recovery. The immediate-change option remains open; appearance previews immediately by default, while nonappearance settings take effect on commit. OI-24 covers the remaining visual polish.

- Treat edits on every Settings tab as one draft. Changing tabs must not save or apply that draft.
- Keep the existing distinction between Save/OK and Apply: Save/OK persists the full draft and closes the window; Apply persists the full draft and keeps it open. The last successful Save/Apply becomes the new restore point.
- Every Appearance control previews immediately by default, including theme colors, background choice, all opacity controls, scale, and compact mode. Preview does not persist the draft or activate reminders, logging, hotkeys, or polling changes. Any future immediate-change option applies only to eligible nonappearance settings.
- Cancel discards all edits since the last successful Save/OK or Apply, across every tab. It restores any immediate previews to that restore point before closing. Closing the window through its title-bar control follows the same discard behavior, including when there are new edits after an earlier Apply.
- A failed save does not advance the restore point or close the window. Show a useful error, and keep the draft available for correction or retry.
- Keep the default desktop window and the existing 500 by 400 logical-pixel minimum usable. Reflow labels and peer cards as needed, scroll content vertically, and keep navigation and commit actions reachable. This does not introduce a mobile Settings layout.

## Selected visual direction

The user selected the following direction on 2026-10-05. The [OpenSpec proposal](../../../openspec/changes/adopt-dense-frost-design-system/proposal.md), [design](../../../openspec/changes/adopt-dense-frost-design-system/design.md), and [screenshot references](../../../openspec/changes/adopt-dense-frost-design-system/references/README.md) define the remaining visual work. The layout, navigation, typography, and control refresh is implemented; native verification of the frost itself is still open (see Implemented decisions).

| Area | Decision |
|---|---|
| Material | Dense frost with sharp foreground content and a solid accessible fallback; require native Windows evidence |
| Layout | Compact rows by default; cards only for two or more meaningful peer groups, including Widget layout and Widget opacity |
| Typography | Segoe UI desktop hierarchy; preserve the timer widget's font |
| Navigation | Uploaded icon-and-underline reference; General, Logging, Appearance, Hotkeys, About |
| Controls | Shared soft-rounded fields, buttons, dropdowns, checkboxes, sliders, and disclosures |

### Implemented decisions

- **Navigation.** Settings tabs are General, Logging, Appearance, Hotkeys, About with outlined Material icons beside the labels, a 2 px shared baseline, and a 3 px underline under the selected tab, drawn directly on the window background (TabSelectedBackground). The selected label and icon use the theme accent, adjusted only as far as needed to reach 4.5:1 on the tab background, because built-in TabSelectedText values were chosen for the old filled tab. Worklog uses the same tabs. At 500 logical pixels all five labels fit; narrower strips scroll horizontally.
- **Layout.** General and Logging are compact sections with restrained dividers. Appearance has the theme tools, then Widget layout and Widget opacity as peer cards, then Timer widget colors and Dialogs and notifications colors as peer cards (all side by side from 560 px of content width, stacked below), then an opacity-diagnostics disclosure that only appears in developer mode. The dialogs card also edits the field background, border, and text colors; Changelog is a card; the rule-list descriptions show on hover of a small ? next to their headers. A palette card holding an invalid color is flagged in its header. Rule editors carry persistent Application pattern, Window title pattern, and Project labels, named reorder/remove actions, and wrap at narrow widths. Page content scrolls; the tab row and the OK/Apply/Cancel footer stay reachable at 500 by 400.
- **Controls and type.** One soft-rounded control family (25 px height, 8 px radius; focus is the accent, adjusted to 3:1; fields, buttons, and disclosure headers share one opaque surface derived from the field-background role) covers buttons, text and read-only fields, numeric fields, dropdowns, checkboxes, sliders, and disclosures; all desktop selectors are scoped under Window.settings-window. Segoe UI with page/section/body/helper sizes of 24/18/14/12.
- **Action and notification styling.** OI-42 adds primary commit, secondary Cancel, and destructive Delete roles. Notifications follow the supplied rounded-dialog reference with muted readable body text, compact close controls, and shared acknowledgement actions. Native captures and focused tests are recorded in the change verification notes.
- **Material.** Settings and Worklog request AcrylicBlur and use a 95% tint only when the platform reports a blur level and Windows transparency effects and contrast preferences allow it; otherwise they keep the solid background with identical layout. Frost has **not** been visually verified. Initial captures used transparency effects off; later verification notes record effects enabled, but another window obscured the test. The native material test still needs a clear-desktop rerun. The widget backdrop (OI-25, OI-28) is unchanged.
- **Still open.** Theme palette work, the optional nonappearance immediate-change setting, native frost verification with transparency effects on, the 100/125/150/200% display-scale review, and the F-03 manual walkthrough.
Theme palette changes are separate. Keep OK, Apply, Cancel, the implemented draft/preview/recovery behavior, read-only hotkeys, and widget Off/Solid choices. The optional nonappearance immediate-change setting and native widget blur remain outside this proposal.

## Investigation before design

- Run [the theme color wiring audit prompt](../../../design/SETTINGS_THEME_COLOR_WIRING_AUDIT_PROMPT.md). Verify actual theme consumers in Settings, both widget modes, and context menus. Identify system-controlled native menu colors and decide what can realistically follow the app theme.
- Run [the control state audit prompt](../../../design/SETTINGS_CONTROL_STATE_AUDIT_PROMPT.md). Record the current visual and keyboard states of tabs, accordions, fields, checkboxes, sliders, color swatches, and action buttons across the Settings pages.
- Trace settings draft, runtime preview, persistence, Apply, Save/OK, Cancel, reset, theme import, and window-close behavior. Distinguish values merely shown in the form from values already applied to the running app.

## Design and implementation tasks

- [x] Preserve the implemented Settings semantic color roles while refreshing remaining control states and mapping the selected-tab background role to the underline. Keep widget button colors independent and retain existing theme-file values without migration.
- [x] Keep the native tray menu system-controlled, as recorded by the theme-wiring audit. Native keyboard and theme walkthrough verification remains open.
- [x] Establish one Settings control system for tabs, accordions, inputs, checkboxes, sliders, and buttons. Specify normal, hover, pressed, selected/checked, disabled, invalid, and keyboard focus states where applicable. Preserve readable contrast in built-in and custom themes.
- [x] Redesign the Settings page hierarchy and spacing. Give the footer a clear boundary and distinguish its primary action from secondary actions. Keep the Appearance editor scannable; group detailed theme fields without hiding essential controls.
- [x] Implement one draft and restore-point model shared by all tabs. Make Save/OK, Apply, Cancel, and title-bar close follow the decisions above. Treat import, theme reset, and preset selection as draft changes until saved or applied.
- [ ] Add the optional immediate-change setting and define which nonappearance settings can preview safely.
- [x] Preserve the implemented validation, load/save-failure, and recovery feedback during the refresh.
- [ ] Complete the combined native keyboard-order, focus-visibility, persistent-label, and pointer-target checks across built-in and imported themes.
- [x] Update OpenSpec, design references, and user-facing documentation to match the final behavior and visual roles.

## Acceptance criteria

1. A theme color's name matches what it changes. Settings text and controls use consistent roles across all tabs. Editing widget button colors does not recolor Settings action buttons unless an explicit shared accent role calls for it.
2. Each control type has consistent sizing, alignment, and interaction states throughout Settings, including keyboard focus and disabled states.
3. Edits on any mix of tabs are persisted only by a successful Save/OK or Apply. All Appearance controls preview immediately; the committed appearance matches that preview. A future nonappearance immediate-change option must not control persistence.
4. Cancel and title-bar close restore the state from the last successful Save/OK or Apply, including any previewed theme or behavior changes. Apply sets a new restore point without closing.
5. Invalid values and save failures are visible. They do not close the window or replace the last successful restore point.
6. Both widget modes retain their layout and remain legible under built-in and imported themes. The actual context menu follows the documented native or themeable behavior.

## Verification

- Exercise draft edits across multiple tabs, then Cancel; repeat after Apply and after a failed save. Check all Appearance controls and title-bar close; check both nonappearance immediate-change modes if that future option is implemented.
- Check all Settings control states with pointer and keyboard input, including focus order, disabled controls, and invalid input.
- Check each built-in theme and an imported custom theme in Settings, both widget modes, and the actual context menu. Record any native menu styling that remains controlled by the operating system.
- Check the normal window size and current minimum size for clipping or unreachable actions.
