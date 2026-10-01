# Design

## Context

See [proposal.md](proposal.md). `TimerWidgetWindow.axaml` requests `Mica, AcrylicBlur, Blur, Transparent`, places a fully opaque `WidgetBaseLayer` under `PanelMaterialLayer`, and uses `WindowBackgroundBrush` as its transparency fallback. `ThemeManager` already gives that brush background opacity while the panel applies the same value again; the window also applies Overall Fade. The existing color-wiring audit documents this repeated alpha and the hidden base layer. Both widget modes share the window shell, so the shell change can stay in `FocusTimer.App` while the blur choice is stored in `FocusTimer.Core.Models.Theme`.

## Goals / Non-Goals

**Goals:**

- Give the shell tint one opacity owner and leave foreground element opacity independent.
- Make the three blur choices take effect live without recreating the widget or changing its bounds.
- Preserve readable fallback and older settings and `.fttheme` files.

**Non-Goals:**

- A numeric blur-radius API or a custom desktop capture and blur renderer.
- Redesigning widget geometry, controls, or the Settings draft/Cancel model tracked by OI-21.
- Adding Linux-specific blur code; unsupported platforms use the same fallback contract.

## Decisions

### 1. Use platform blur levels as the three choices

Store a stable `Off`, `Soft`, or `Strong` value in `Theme` and expose it in Appearance beside Background opacity. Map Off to `Transparent`, Soft to `Blur, Transparent`, and Strong to `AcrylicBlur, Blur, Transparent`. On the tested Windows compositor, Blur is unavailable, so Soft falls through to Transparent and remains see through; the diagnostic must say blur is unavailable. AcrylicBlur is reported for Strong but appears nearly solid even at 0% tint. Remove Mica from this widget's preferred sequence because it is system-tinted rather than the desktop blur requested here. Observe `ActualTransparencyLevel` so the UI can report the achieved API effect without promising visual transparency. The shared shell applies the choice to both modes. Avalonia's backdrop levels are discrete and do not provide a continuous blur radius. OI-28 tracks a true custom radius and a verifiably see-through desktop backdrop.

### 2. Compose one shell tint beneath unchanged content

Replace the visible base/panel pair with one semantic widget-shell tint surface. Derive its RGB color from the theme's `WindowBackground`, discard any embedded color alpha for this surface, and apply `BackgroundOpacity` exactly once to the tint. Keep the window background transparent; do not bind background opacity to the window, content grid, clock, buttons, or project field. Keep Overall Fade as a separate explicit whole-window control, but remove its duplicate application from child-layer opacity calculations so it is applied only once. Clock and controls keep their own opacity settings. At 0% tint the selected blur remains; Off at 0% is clear on a capable platform. Keep the border independent of tint opacity so the shell edge can remain visible.

`WidgetBaseOpacity` is a legacy serialized field without a visible editor. Stop consuming it in the shell, but tolerate it when reading old files. Avoid changing global `WindowBackgroundBrush` consumers; introduce a widget-specific shell tint resource or binding instead. Retain one opaque theme-colored fallback brush for the window when transparency is not available, rather than reusing the opacity-controlled tint brush.

### 3. Store blur with theme appearance and validate imports

Use a string-stable serialized enum or equivalent validated value for blur. New Dark and other regular themes default to Strong so the restored frost is available; tune their initial tint opacities for visible blur without sacrificing contrast. High Contrast defaults to Off and forces a solid shell even if an imported or previously stored value asks for blur. A missing blur field in older settings or `.fttheme` files resolves to Strong; an unknown supplied value fails theme import validation. Update `Theme.Clone()` and theme export so the choice survives preset selection, settings reload, import/export, and restart. Preserve existing color and opacity values in older files. At startup, use the Dark factory preset only for a truly new/default settings instance; a persisted `Settings.Theme` is authoritative even when `ActiveThemeName` names a built-in preset. Explicit preset selection or reset still replaces the edited values with that preset.

Settings currently edits a separate `Settings` instance. Have its appearance preview publish a theme snapshot through `ThemeManager` (or a small equivalent app-level event), and have the widget window consume that snapshot for blur and tint. The window must not rely only on its persisted `TimerWidgetViewModel.Settings` for live preview. Capture the last successfully applied appearance when Settings opens, advance that restore point after successful Apply, and reapply it on Cancel or window close. This restores the new appearance preview without redesigning the remaining Settings draft model.

### 4. Keep accessibility fallback separate from the tint slider

When transparency is unavailable or disabled, render a solid or near-solid shell using the theme's opaque background color. Do not dim foreground content to simulate fallback. High Contrast always takes this path. When blur falls through to Transparent, preserve the user's tint opacity so the widget stays see through, and report that the requested blur was unavailable. The tint opacity can reach 0% on every transparency-capable path.

## Risks / Trade-offs

- Blur support and exact intensity vary by Windows version and compositor. Map the choices to stable platform levels, inspect `ActualTransparencyLevel`, and verify the result on a running Windows build. Do not label a fallback as the requested level in diagnostics.
- A reported AcrylicBlur level does not prove that desktop content is visible through it. The tested Windows compositor produced a nearly solid dark material at 0% app tint; OI-28 owns a custom backdrop investigation.
- A very low tint can reduce contrast over busy desktop content. Keep the solid fallback for accessibility modes and verify clock, buttons, project input, focus, and border over light and dark backgrounds.
- Stored `WidgetBaseOpacity` can surprise maintainers after it stops affecting the shell. Document its compatibility-only role and cover older-file loading in tests; remove it only through a separate migration decision.
- The current Settings live-preview and Cancel behavior is inconsistent (OI-21). Use the existing settings flow for this change and do not present this proposal as fixing that broader issue.
- `AppController` currently replaces loaded built-in themes with factory presets at startup, and Settings edits a different settings object from the widget. The startup and preview paths must be corrected for these appearance properties or the new controls will appear to work but revert after restart or fail to update live.

## Migration Plan

1. Add the blur value with a default for missing fields, keep old theme files readable, and verify clone and import/export round trips.
2. Replace the shell layers and duplicate alpha application; wire the blur choice and opaque fallback through Appearance and the widget window.
3. Check the rendered result on Windows in full and compact modes, with each blur choice and 0%, intermediate, and 100% tint. Check saved themes, High Contrast, and unavailable transparency.
4. Roll back to the previous renderer if platform behavior is unacceptable; the new blur field remains ignorable by older versions, while existing opacity fields retain their meanings.

## Windows visual evidence (2026-10-01)

With the Dracula theme in the running Windows build, user screenshots showed Strong reporting `AcrylicBlur` at 0% tint while the widget surface looked nearly solid dark. Soft reported `Transparent` and looked like Off, confirming the platform `Blur` level was unavailable in this session. Removing the app's opaque shell fallback made Soft see through again. An `ExperimentalAcrylicBorder` prototype did not make Strong visibly see through, so it was removed. The remaining full/compact and theme matrix in tasks 4.2–4.3 is still open.
