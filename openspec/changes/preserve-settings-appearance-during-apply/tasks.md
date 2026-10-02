# Tasks

## 1. Stable commit presentation and feedback

- [x] 1.1 Add a loaded/disposed visual-availability property separate from `CanEdit` in SettingsWindowViewModel; verify delayed-commit tests keep `CanEdit` false and visual availability true, while failed load remains visually disabled.
- [x] 1.2 Replace the TabControl's commit-driven disabled binding and reserve a fixed-height footer status for "Saving..." with accessible status semantics; verify the same values, palette, content opacity, and layout remain visible during a delayed Apply in Light, Monokai, and Solarized Dark.
- [x] 1.3 Cover saving-status transitions for validation failure, success, ordinary failure, recovery-required failure, and OK success with tests; verify status always clears after completion and validation never shows a saving state.
- [x] 1.4 Update the Settings guide and OI-21/OI-24 notes to distinguish visual availability, input lock, and transaction state; verify documentation matches the implemented status and unchanged commit/restore rules.

## 2. Commit-aware input and focus protection

- [x] 2.1 Add the App input-lock behavior for the Settings draft container, including a transparent pointer shield, parked focus, and blocked keyboard/text/wheel editing; verify focused TextBox typing/paste, sliders, and dropdowns cannot mutate the draft or candidate during a delayed commit.
- [x] 2.2 Handle already-open editor popups/context menus, text composition, routed edit commands, and deferred picker/import results; verify they cannot bypass the lock or replay mutations after completion. Retain `CanEdit` guards and cover any direct binding mutation path exposed by these tests.
- [x] 2.3 Attach and detach input handlers with Settings window lifetime and restore valid prior focus after Apply/ordinary failure; verify close/reopen has no retained lock or handler and successful OK closes without focus restoration to a detached editor.
- [x] 2.4 Test duplicate Apply/OK, Cancel/title-bar close, and the external compact-mode button during a delayed commit; verify one transaction completes, close is blocked, and no separately persisted or queued draft mutation occurs.
- [x] 2.5 Document the input behavior and focused-editor recovery in DEVELOPMENT.md; verify its lifetime and popup coverage agree with the implemented tests.

## 3. Widget color editors and theme compatibility

- [x] 3.1 Add observable optional PlayPauseColor/playPauseColor to Core Theme and preserve it in cloning and JSON round trips; verify old themes inherit ButtonNormal and existing TimerBackground values remain unchanged.
- [x] 3.2 Resolve PlayPauseBrush and the editor's inherited display value; update explicit color validation without rejecting null inheritance. Verify explicit invalid values block saves, ButtonNormal edits update inherited Play/Pause, and presets/reset/import/export keep the fallback or explicit color correctly.
- [x] 3.3 Replace the Timer Background editor with Play/Pause color; keep Success and Danger editors with reserved-for-future-use help text. Verify the fields, swatches, and compatibility behavior match the updated specification.
- [x] 3.4 Move fixed widget icon Foreground attributes into shared normal/Play-Pause styles and wire ancestor hover/pressed/disabled states to the matching brushes in both modes. Verify Disabled > Pressed > Hover > normal precedence, preserve existing background highlights/focus indicators, and apply opacity only once.
- [x] 3.5 Add realized-control tests for ordinary and Play/Pause icons in both modes, including resource changes while hovered, press/release, and disabled state. Verify the computed foreground changes, rather than only asserting ThemeManager resource values.
- [x] 3.6 Extend appearance lifecycle tests for Play/Pause and state colors across Apply, OK, ordinary failure, Cancel after an earlier Apply, reopening, and saved-data reload. Verify Success/Danger values also survive save/restore and import/export without adding status consumers.
- [x] 3.7 Update README/Settings guidance and current OI-21/OI-24 notes for the delivered icon states, dedicated Play/Pause color, and reserved Success/Danger fields; reconcile relevant settings/widget/theme specs and active change artifacts without closing OI-29 or unrelated visual work.

## 4. Windows integration verification

- [x] 4.1 Run the affected Core and App tests and Host Debug build; verify theme compatibility, preview, commit compensation, recovery, and compact-mode routing regressions pass without changes to the commit protocol or platform services.
- [x] 4.2 Record Windows visual/input evidence for a fast Apply, deliberately delayed Apply, OK, ordinary failure, and recovery-required failure across Light, Monokai, and Solarized Dark; verify there is no whole-panel disabled flash, button movement, hidden save progress, or mutation leak. Also verify normal/hover/pressed/disabled icons in full and compact modes with a distinct Play/Pause color. No FocusTimer.Platform.Windows edit or new Linux stub is expected.
- [x] 4.3 Verify proposal/spec/design/tasks and the current backlog agree with delivered behavior before archiving; keep OI-29 and other OI-21/OI-24 work open.
