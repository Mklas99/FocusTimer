# Design

## Context

See proposal.md for the user-visible problem. `SettingsWindow.axaml` binds the entire TabControl's `IsEnabled` to `CanEdit`; `CanEdit` becomes false during `IsCommitting`. Fluent templates and scoped disabled-text styles therefore repaint every editor during Apply/OK. The footer already has separate commit, close, load-error, and recovery controls. `ApplyAsync` clones one candidate and awaits `SettingsCommitCoordinator`; that transaction must remain intact.

This change builds on the existing complete appearance preview and the compact-mode draft-routing fix. `make-settings-edits-consistent` owns transaction/recovery behavior; this proposal adds presentation and input handling without weakening that change's close or mutation protections.

Widget icons currently bind their Foreground directly to ButtonNormalBrush. Hover and pressed backgrounds use title-bar highlight brushes; ButtonHoverBrush, ButtonPressedBrush, and ButtonDisabledBrush do not style the icons. TimerBackgroundBrush has no rendered consumer. Success and Danger similarly have no current displayed status consumer, but their editors must remain for the user's planned future work.

## Goals / Non-Goals

**Goals:** Keep the current preview visible, protect one committing candidate from further user input, provide save feedback, restore editor focus after Apply, and connect widget color editors to consistent icon states in both modes.

**Non-Goals:** Faster disk writes, changing the transaction protocol, broad control restyling, OI-29 imported-theme selection, previewing nonappearance settings, adding a timer backdrop, or implementing future Success/Danger statuses. No Platform.Windows changes or Linux-stub work is required.

## Decisions

### Separate visual availability from permission to edit

Keep `CanEdit` as the authoritative mutation guard. Add a visual-availability property based on loaded/disposed state, excluding the transient commit flag, and bind the draft panel's `IsEnabled` to that property. Preserve intentional field-level disabled states, such as background tint under Solid. Do not simply change `CanEdit` to true during a commit: delayed imports and widget actions use it to reject edits.

### Lock interaction at the draft container and park focus

Use one commit-aware input behavior in FocusTimer.App, attached to the editable Settings container and its Window. On commit start, remember the focused editor, close existing dropdown/context-menu editor popups, end any active text composition, and move focus to a noneditable save-status element outside the panel. Add a transparent pointer shield over the panel and consume editing keyboard/text/wheel input while committing. Keep the rendered controls enabled to preserve their normal colors.

This behavior must register and release handlers with window lifetime, restore focus only to an attached and still-enabled editor when Apply completes, and leave OK's successful close alone. Gate paste/context-menu and other routed editing commands as well as raw input. The behavior must cover popup roots: an overlay alone cannot stop an already-focused TextBox or a dropdown outside the panel. Deferred picker/import results and the widget's compact action remain protected by the view-model guards. Add guards for any editable command or delayed binding path that can bypass the container lock.

Alternatives rejected: removing the lock permits edits during a cloned commit; overriding every Fluent disabled style is fragile across themes and controls; a pointer-only overlay leaves keyboard and popup input active.

### Reserve save feedback in the footer

Add a fixed-height status row next to the existing footer actions. Show "Saving..." for `IsCommitting`, with accessible status semantics and no new content shifting. Fast saves use the same row without introducing an artificial minimum duration. Keep Apply/OK/Cancel command protection and existing recovery messages. Validation errors do not enter this state.

### Keep commit and preview semantics unchanged

The candidate, persistence, registration reconciliation, runtime activation, compensation, and restore-point updates stay in `SettingsCommitCoordinator` and the existing editor flow. The status and input lock are tied to `IsCommitting`, including all exceptions and finally paths. During recovery-required states, existing recovery handling continues to own what can be committed or closed.

### Give Play/Pause a compatible normal icon color

Replace only the Timer Background editor with "Play/Pause color". Add an observable nullable `PlayPauseColor` string to Core Theme, serialized as `playPauseColor` and preserved by Clone. Null/absent means inherit ButtonNormal; do not copy ButtonNormal into the model on load, so older themes continue to follow their normal icon color until explicitly customized. Resolve the inherited color for the editor/swatch and ThemeManager's PlayPauseBrush. Keep the old TimerBackground field and clone/serialization behavior unchanged; do not reinterpret existing timer-background values as icon colors.

Apply color parsing validation to a non-null PlayPauseColor; exempt only its null inheritance sentinel from the current all-string-color check. Ensure import, export, preset selection, reset, and saved Settings retain the optional field and trigger live resource updates. Built-in themes can inherit ButtonNormal without separate palette decisions. The dedicated color applies to both Play and Pause glyphs, independently of the running state.

### Style widget icon interaction states through shared colors

Move fixed icon foreground values out of local XAML attributes into shared icon styles so ancestor Button states can override them. Ordinary icons use ButtonNormalBrush; a Play/Pause icon class uses PlayPauseBrush in its normal state. Hover, Pressed, and Disabled override both classes using ButtonHoverBrush, ButtonPressedBrush, and ButtonDisabledBrush, in that precedence order with Disabled highest and Pressed above Hover. Release/leave restores the appropriate normal color. Keep TitleBarButtonHoverBrush/PressedBrush as the existing background highlights and preserve focus indicators and controls-layer opacity.

Use the same styles in FullModeView and CompactModeView. Verify realized controls and computed icon foreground for normal, hover, pressed, and disabled states; resource-only tests cannot prove these settings are connected. Check dynamic resource replacement while a state remains active, especially pointer-over preview, and ensure fixed local values do not mask style setters.

### Retain future status editors without implying current effects

Keep Success and Danger controls editable, preview their swatches, and preserve their values through Apply/OK, Cancel, import/export, and reopen. Add concise help text that these two colors are reserved for future status displays and currently do not change notifications. Warning continues to affect existing summary warnings. Do not add unrelated statuses or remove these fields.

## Risks / Trade-offs

- Focus or popup input could bypass the shield. Mitigate with UI tests for a focused TextBox, keyboard paste, open dropdown/context menu, sliders, and widget actions during a delayed commit; do not accept a pointer-only implementation.
- A visually enabled editor can appear editable. Mitigate with the visible/announced saving status and focus parked outside the editor; restore focus after ordinary completion.
- Leaked handlers could lock later Settings windows. Attach/detach the behavior with the window lifecycle and test close/reopen.
- Screenshots alone miss mutation races; view-model tests alone miss disabled rendering. Use both delayed-commit interaction tests and visual checks across Light, Monokai, and Solarized Dark.
- Optional color fallback can be lost by cloning or validation. Test old files, explicit values, inherited ButtonNormal edits, and import/export round trips; legacy TimerBackground must remain unchanged.
- Local icon Foreground values and style precedence can mask state colors. Test the computed foreground on actual buttons in both modes, including a hovered/pressed Play/Pause icon and changes while hovering.

## Migration Plan

No rewrite of existing files or new dependencies. Add the optional playPauseColor field to the existing JSON formats; absence inherits ButtonNormal and legacy TimerBackground remains intact. Implement App presentation/styles and Core theme support after review, retain existing transaction tests, and record Windows UI evidence before marking the relevant OI-21/OI-24 gaps delivered. Reverting the view/input behavior restores the prior presentation; older app versions ignore the new field and use ButtonNormal for Play/Pause.
