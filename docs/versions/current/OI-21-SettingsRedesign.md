# OI-21: Settings redesign and theme consistency

## Outcome

Make the Settings window feel like one coherent desktop interface. Tabs, accordions, fields, checkboxes, action controls, and their interaction states should share consistent spacing, typography, geometry, and color roles. Theme colors should behave predictably across Settings, the timer widget, and the context menu without coupling Settings actions to widget button colors. This task does not redesign the widget layout.

## Decisions

These are the target behaviors for OI-21; the current implementation may differ.

The shared draft, full Apply/OK commit, Cancel/title-bar discard, temporary appearance preview, and visible load/commit/recovery errors are implemented by `make-settings-edits-consistent`. An unfinished commit is recovered before startup activates settings; a failed load stops activation and opens Settings for retry. Windows start-on-login registration is compared with the saved value and reconciled on Apply/OK, with the original Run value preserved for recovery. The immediate-change option remains open; appearance previews immediately by default, while nonappearance settings take effect on commit. OI-24 covers the remaining visual polish.

- Treat edits on every Settings tab as one draft. Changing tabs must not save or apply that draft.
- Keep the existing distinction between Save/OK and Apply: Save/OK persists the full draft and closes the window; Apply persists the full draft and keeps it open. The last successful Save/Apply becomes the new restore point.
- Add a Settings option for immediate changes. When off, edits remain a draft and do not change running behavior or appearance until Save/OK or Apply succeeds. When on, eligible edits may preview in the running app immediately, but they are not persisted until Save/OK or Apply succeeds. Document any setting that cannot preview safely.
- Cancel discards all edits since the last successful Save/OK or Apply, across every tab. It restores any immediate previews to that restore point before closing. Closing the window through its title-bar control follows the same discard behavior, including when there are new edits after an earlier Apply.
- A failed save does not advance the restore point or close the window. Show a useful error, and keep the draft available for correction or retry.
- Smaller window sizes are not a priority for this redesign. Avoid new clipping at the current minimum size, but prioritize the normal desktop window size and do not spend this task on a compact settings layout.

## Investigation before design

- Run [the theme color wiring audit prompt](../../../design/SETTINGS_THEME_COLOR_WIRING_AUDIT_PROMPT.md). Verify actual theme consumers in Settings, both widget modes, and context menus. Identify system-controlled native menu colors and decide what can realistically follow the app theme.
- Run [the control state audit prompt](../../../design/SETTINGS_CONTROL_STATE_AUDIT_PROMPT.md). Record the current visual and keyboard states of tabs, accordions, fields, checkboxes, sliders, color swatches, and action buttons across the Settings pages.
- Trace settings draft, runtime preview, persistence, Apply, Save/OK, Cancel, reset, theme import, and window-close behavior. Distinguish values merely shown in the form from values already applied to the running app.

## Design and implementation tasks

- [ ] Define a small Settings-specific set of semantic colors for page surfaces, primary and muted text, input text, borders, selection, focus, and action states. Map every visible Settings control to these roles. Keep widget button colors independent. Preserve existing theme file compatibility or document a deliberate migration.
- [ ] Decide whether to keep the native tray menu's system styling or use a themeable app menu. Base the decision on the audit and keep menu behavior accessible.
- [ ] Establish one Settings control system for tabs, accordions, inputs, checkboxes, sliders, and buttons. Specify normal, hover, pressed, selected/checked, disabled, invalid, and keyboard focus states where applicable. Preserve readable contrast in built-in and custom themes.
- [ ] Redesign the Settings page hierarchy and spacing. Give the footer a clear boundary and distinguish its primary action from secondary actions. Keep the Appearance editor scannable; group detailed theme fields without hiding essential controls.
- [x] Implement one draft and restore-point model shared by all tabs. Make Save/OK, Apply, Cancel, and title-bar close follow the decisions above. Treat import, theme reset, and preset selection as draft changes until saved or applied.
- [ ] Add the optional immediate-change setting and define which nonappearance settings can preview safely.
- [ ] Provide visible validation and save-failure feedback without silently discarding edits. Check keyboard order, focus visibility, labels, and pointer targets.
- [ ] Update OpenSpec, design references, and user-facing documentation to match the final behavior and visual roles.

## Acceptance criteria

1. A theme color's name matches what it changes. Settings text and controls use consistent roles across all tabs. Editing widget button colors does not recolor Settings action buttons unless an explicit shared accent role calls for it.
2. Each control type has consistent sizing, alignment, and interaction states throughout Settings, including keyboard focus and disabled states.
3. Edits on any mix of tabs are persisted only by a successful Save/OK or Apply. The immediate-change option controls runtime preview, not persistence.
4. Cancel and title-bar close restore the state from the last successful Save/OK or Apply, including any previewed theme or behavior changes. Apply sets a new restore point without closing.
5. Invalid values and save failures are visible. They do not close the window or replace the last successful restore point.
6. Both widget modes retain their layout and remain legible under built-in and imported themes. The actual context menu follows the documented native or themeable behavior.

## Verification

- Exercise draft edits across multiple tabs, then Cancel; repeat after Apply and after a failed save. Check both immediate-change modes and title-bar close.
- Check all Settings control states with pointer and keyboard input, including focus order, disabled controls, and invalid input.
- Check each built-in theme and an imported custom theme in Settings, both widget modes, and the actual context menu. Record any native menu styling that remains controlled by the operating system.
- Check the normal window size and current minimum size for clipping or unreachable actions.
