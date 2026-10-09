# Manual UI walkthrough

The native interaction review for OI-21, OI-24, OI-29, and OI-42 is complete based on user confirmation on 2026-10-08. The checked items below record that confirmation, rather than a new agent-run walkthrough. This checklist remains available for future regression reviews.

The display-scaling review is complete based on your confirmation on 2026-10-08. It does not need repeating. The native Settings frost test also passed on that date. Optional nonappearance preview is a separate feature candidate, OI-44, and is outside this walkthrough.

## Completion record

On 2026-10-08, the user confirmed that the interactive review and verifications can be closed: "everything fine". This closes the combined native interaction walkthrough, the theme-focused checks across presets and imports, and the Full/Compact widget desktop-content checks. Completion is user-reported; no fresh automated run or additional captures were supplied with this confirmation. Exact build, Windows version, scale and opacity values were not supplied. Existing automated and capture evidence remains recorded below. OI-25/OI-28 widget blur, OI-44, and the separate F-03 and tracking-rule walkthroughs retain their own status.

## Test run

| Detail | Value |
|---|---|
| Date / tester | 2026-10-08 / user confirmation |
| Build or commit tested | Not supplied with confirmation |
| Windows version | Not supplied with confirmation |
| Current display scale | Not supplied with confirmation |
| Previously reviewed scales, if known | Not supplied with confirmation |
| Imported theme used | Not supplied with confirmation |

Run the current build. Use disposable Worklog entries for editing and deletion. Note the original Settings values so you can restore them after checks that use Apply or OK.

## 1. Navigation and focus

- [x] Open Settings and visit General, Logging, Appearance, Hotkeys, and About. Each tab shows the correct content and selected underline.
- [x] Use Tab and Shift+Tab through every page. Focus is visible, follows a sensible order, and can leave each control.
- [x] Operate buttons, checkboxes, dropdowns, accordions, and sliders with the keyboard. Pointer hover and pressed feedback remain visible.
- [x] Scroll Settings at its normal and minimum sizes. Navigation, feedback, and OK/Apply/Cancel remain reachable.
- [x] Open Worklog from the tray and visit Entries, Timeline, and Summary. Switch tabs using the keyboard as well as the mouse.
- [x] Open the actual tray menu and operate its commands with mouse and keyboard. Its colors are controlled by Windows; it need not follow the app palette.

## 2. Fields, rules, and validation

- [x] Edit text and numeric fields using typing, selection, copy, and paste. Labels remain identifiable after values are entered.
- [x] Enter an invalid value in a validated field. The error is visible, committing is blocked where required, and correcting the value clears the error.
- [x] Open dropdowns and date-picker popups. They are readable, positioned sensibly, and usable with the keyboard.
- [x] If testing rule editors, enable Developer mode through the About interaction. Add, reorder, and remove disposable rules.
- [x] Leave a rule entirely blank. It is marked as removed on Apply and does not block committing. A partly filled invalid rule does block committing.
- [x] Check disabled controls. They look disabled and cannot activate their action.

## 3. Settings save and discard

For these checks, edit a reversible Appearance value and at least one nonappearance setting. Avoid starting recording or changing system registration unless you intend to test those effects.

- [x] Change values on several tabs, switch tabs, then Cancel. Switching tabs does not save; Cancel restores the last committed values and appearance.
- [x] Repeat, closing Settings through its title-bar close button. Closing discards the draft just like Cancel.
- [x] Change an Appearance value. The widget or relevant desktop colors preview immediately. Ordinary nonappearance edits remain inactive until committed.
- [x] Change values and click Apply. The values are saved, Settings stays open, and the appearance does not flash or become dimmed.
- [x] After Apply, make further edits and Cancel. The values from Apply remain; only the later edits are discarded.
- [x] Repeat Apply followed by further edits and title-bar close. It restores the same successful Apply point.
- [x] Change values and click OK. Settings closes; reopening shows the saved values.
- [x] Restart the app after a successful commit. The saved values and appearance remain.

## 4. Themes and appearance

For each theme, inspect Settings, both widget modes, Worklog, the color picker, and notifications where available. Check normal, hover, pressed, disabled, and keyboard-focused states. Note any unreadable text, missing focus ring, or inconsistent action styling.

| Theme | Checked | Problems / checks not performed |
|---|---|---|
| Dark | [x] | |
| Light | [x] | |
| Monokai | [x] | |
| Solarized Dark | [x] | |
| Nord | [x] | |
| Dracula | [x] | |
| High Contrast | [x] | |
| Imported theme | [x] | |

- [x] Open the color picker, change a color, and use OK. The preview updates without losing the parent Settings draft.
- [x] Open the color picker again, change a color, and Cancel. The picker restores its prior value and returns focus sensibly.
- [x] Preview a different preset or import a theme, then Cancel Settings. The last committed appearance returns.
- [x] Apply a theme, make further color edits, then Cancel. The applied theme remains.
- [x] Primary OK/Apply/Save actions, neutral Cancel actions, and destructive Delete actions are distinguishable. Keyboard focus remains visible on each.

Custom/Imported is implemented for the current custom draft. Include an import named Dark with custom colors, Apply/OK, reopening after its source file is unavailable, and Cancel/title-bar close after further edits. Native verification for OI-29 was confirmed complete by the user on 2026-10-08; automated identity and snapshot tests are recorded in the theme change's verification notes.

## 5. Worklog forms

Use a past day inside the retention window and disposable entries. Example: yesterday, start 10:00, duration `15m`, window `UI walkthrough`, project `UI test`.

- [x] Add an entry through its fields and Save. It appears in Entries with the expected values.
- [x] Start adding another entry and Cancel. No entry is created.
- [x] Edit the disposable entry's window, project, and duration, then Save. The new values appear.
- [x] Edit it again and Cancel. Its saved values remain unchanged.
- [x] Try invalid duration input. A useful validation message appears and the form remains available for correction.
- [x] Open Delete confirmation and Cancel. The entry remains.
- [x] Open Delete confirmation again and confirm deletion. Only the chosen disposable entry is removed.
- [x] Complete the save/cancel/delete actions with the keyboard as well as the mouse. Focus returns to a sensible control after the form or confirmation closes.
- [x] Check date-picker and project popups in the forms. They fit the window and do not obscure the action row unnecessarily.

## 6. Notifications

Use existing app events or a controlled test run. Developer notification-trigger buttons are a separate open issue, OI-43; do not assume they are available.

- [x] Information notification: inspect the rounded card, title, muted body, and close control. It dismisses automatically after about five seconds.
- [x] Dismiss an information notification using the close control. Check hover and keyboard focus where the window can receive focus.
- [x] Acknowledgement reminder: it remains open beyond the ordinary timeout, has a reachable OK button, and closes when acknowledged by mouse or keyboard.
- [x] Warning and error notifications: the severity is named explicitly, colors are readable, and automatic dismissal occurs after about fifteen seconds.
- [x] Long message: body text scrolls while the dismissal action stays reachable.

## 7. Failure and recovery

These checks require a controlled failure scenario. Use an isolated test setup or an assisted reproduction rather than altering your real settings or worklogs. If no safe scenario is available, record these as not tested; ordinary save success does not cover them.

- [x] A Settings save failure shows a useful error, keeps Settings open, and preserves the editable draft.
- [x] After removing the failure, retry succeeds. Apply stays open and OK closes as appropriate.
- [x] Cancel after a failed save restores the last successful commit, including appearance preview.
- [x] A controlled load or recovery failure offers its intended retry action. After the cause is removed, retry recovers without silently activating an invalid draft.

## Results

| Section / check | Passed, failed, or not tested | Observation / issue reference |
|---|---|---|
| All sections | Passed by user confirmation, 2026-10-08 | User reported "everything fine"; no failures reported. |

- [x] Restore temporary Settings changes and remove remaining disposable Worklog entries.
- [x] Record any failures or unavailable scenarios above.
- [x] Record whether the walkthrough is complete or which checks remain open. Untested failure/recovery cases may remain a separate verification item.

This checklist covers the UI interaction review, including the completed native theme and OI-29 checks. Automated palette and imported-selection work is implemented in [theme verification](../../../openspec/changes/archive/2026-10-08-optimize-theme-readability/verification.md). The separate F-03 data-management walkthrough, tracking-rule walkthroughs, OI-44, and widget blur OI-28 retain their own completion criteria.

Related planning: [Settings redesign](OI-21-SettingsRedesign.md), [open issues](OpenIssues.md), and [OpenSpec verification notes](../../../openspec/changes/archive/2026-10-08-adopt-dense-frost-design-system/verification.md).

## F-03 walkthrough completion, 2026-10-08

The user subsequently confirmed that the separate Worklog data-management walkthrough had already been performed and was fine. Task 10.5 in worklog-data-management is complete, including the carried-over Summary check in Light and High Contrast. This supersedes earlier notes describing the F-03 walkthrough as open. Tracking-rule walkthroughs retain their separate status.
