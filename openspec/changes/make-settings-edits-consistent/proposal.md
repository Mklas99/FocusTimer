# Proposal

## Why

OI-21 calls for one predictable Save, Apply, and Cancel contract across Settings. The window already edits a separate settings object, but theme changes preview immediately, auto-start changes before file persistence, and save failures are only logged; those paths do not share a reliable restore point or visible failure result.

## What Changes

- Treat all editable Settings values as one draft. Switching tabs, choosing or importing a theme, and resetting a theme change the draft without saving it.
- Make OK and Apply validate and commit the full draft. OK closes only after success; Apply stays open and advances the restore point only after success.
- Make Cancel and title-bar close discard edits since the last successful commit and restore any live appearance preview.
- Block editing and committing if an existing settings file cannot be loaded; show an error and offer a retry instead of saving defaults over that file. Keep the window open while a commit is running so a close request cannot race with the result.
- Keep current immediate appearance preview behavior, while making it explicitly temporary until a successful commit. Other settings continue to take effect on commit. The optional immediate-change setting in OI-21 remains separate work.
- Show validation and save failures in Settings. Keep the draft available for correction, protect the previous settings file on write failure, and provide a visible recovery path if a later rollback cannot finish. Keep the saved auto-start choice authoritative and show any mismatch with Windows registration before Apply reconciles it.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `settings`: Define a shared draft, commit, discard, preview restore, and failure contract for every editable tab.
- `auto-start`: Reconcile start-on-login registration during a Settings commit and report registration or recovery failure.

## Impact

The main work is in `FocusTimer.App` Settings view model, window, and apply path. It also affects the JSON settings write in `FocusTimer.Persistence` and the auto-start interface plus Windows implementation and Linux stub. Existing settings and theme file formats stay compatible. Settings layout and the OI-24 visual redesign are outside this change.
