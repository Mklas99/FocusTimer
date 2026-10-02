# Proposal

## Why

OI-21 and OI-24 track inconsistent Settings theme colors and poor readability. Several editable text, input, and tab colors are published as resources but do not reach the controls they name; `InputFocusBorder` and `TabSelectedBackground` are also populated from `AccentPrimary` instead of their own theme values.

## What Changes

- Map `InputFocusBorder` and `TabSelectedBackground` to their matching runtime resources.
- Make Settings text, editable and read-only inputs, and tab states use the existing theme color roles consistently across Settings tabs.
- Keep widget button colors separate from Settings control colors and preserve existing theme file fields and layout.
- Check the seven built-in themes for readable text, field contents, selected tabs, and keyboard focus, then update the OI-21/OI-24 status notes and design mapping to match the delivered behavior.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `theming`: Require Settings text, input, and tab colors to reflect their named theme properties, including live theme changes and legible built-in theme states.

## Impact

The change is centered in `FocusTimer.App` theme resource mapping and Settings styles, with focused App tests. It does not add serialized theme fields or dependencies. Widget rendering, tray menu styling, Settings layout, action-button redesign, and the broader OI-21 draft/save workflow remain separate work.
