# Proposal

## Why

The widget's background opacity slider no longer reveals the desktop because an opaque base layer remains behind the adjustable surface (OI-25). The user wants the earlier tinted, frosted appearance back, with selectable blur strength and independent foreground readability.

## What Changes

- Restore one effective widget shell whose background tint opacity slider controls only its tint, in both full and compact modes. At 0% tint, the app adds no theme color; Off plus 0% yields a clear background where transparency is supported. The platform blur itself can still look opaque.
- Add Off and Solid background choices. Off shows the desktop through the selected tint; Solid uses a fully opaque theme color regardless of the tint slider. Existing Soft, Strong, and Blur values load as Off because blur was not visibly distinct on the tested compositor.
- Keep clock, button, and project-field opacity independent of the background slider. Preserve the existing explicit Overall Fade control as the only control intended to fade the whole widget.
- Persist the blur choice with theme appearance, include it in theme import/export, and give older settings and theme files a compatible default. High Contrast uses a solid, unblurred surface.
- Keep saved blur and tint edits when the user restarts with a built-in theme selected. Preview changes live and restore the last successfully applied appearance when Settings is canceled or closed without applying.
- Record the Windows compositor limitation found during visual testing: the previous Soft fell back to clear transparency, while Strong's AcrylicBlur looked nearly solid. OI-28 tracks a true see-through custom blur without desktop capture.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `widget-ui`: Specify independent shell tint opacity, Off/Solid background choices, and their fallback and accessibility behavior.
- `theming`: Persist, apply, and exchange the blur choice with theme appearance without changing widget layout or interactions.

## Impact

- `FocusTimer.App`: widget shell, Appearance controls and view models, theme resource mapping, and runtime backdrop selection.
- `FocusTimer.Core`: theme model, built-in appearance defaults, cloning, and theme file compatibility.
- Focused tests for opacity isolation, theme round trips, blur selection and fallback; rendered checks on Windows for both widget modes and representative themes.
- No timer behavior, widget layout redesign, or general Settings control redesign.
