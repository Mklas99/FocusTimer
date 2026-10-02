# Proposal

## Why

Apply currently disables the complete Settings TabControl through `CanEdit`, repainting the previewed controls in their disabled states while saving. This makes an otherwise unchanged preview flash during a commit and gives no clear indication that saving is in progress (OI-21 and OI-24).

The color review also found that widget Hover, Pressed, and Disabled fields do not control those icon states, and Timer Background has no rendered consumer. The user approved replacing that editor with a dedicated Play/Pause color and retaining Success and Danger for future use.

## What Changes

- Keep the displayed Settings palette, control opacity, values, layout, and widget appearance stable during Apply/OK.
- Block draft mutations and duplicate commits without disabling the entire visible Settings content.
- Show a reserved footer status for saving; retain the existing success, failure, recovery, and close behavior.
- Verify pointer, keyboard, focus, delayed commands, and widget compact-mode actions during a deliberately delayed commit.
- Use Button Normal for ordinary widget icons and a dedicated Play/Pause color for the Start/Pause icon in both modes; use shared Hover, Pressed, and Disabled colors for interaction states while preserving existing background highlights.
- Replace the Timer Background editor with Play/Pause color; retain the legacy timer-background theme field for compatibility.
- Keep Success and Danger editors and saved values, with help text explaining that they are reserved for future use.
- Verify the new color and state colors preview immediately, persist through Apply/OK and reopening, and restore on Cancel.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `settings`: Stable visual presentation, explicit save feedback, input locking throughout Apply/OK, and color editors that control widget icon states with compatible theme persistence.

## Impact

Changes affect FocusTimer.App, the Core Theme model, relevant tests, and Settings documentation. The existing commit coordinator and persistence/recovery protocol remain in place. The JSON theme/settings formats receive one optional additive play/pause color field; older files use Button Normal as the fallback. No new packages or Windows platform service changes are needed. This proposal is separate from the delivered compact-mode draft routing fix and OI-29's imported-theme dropdown work.
