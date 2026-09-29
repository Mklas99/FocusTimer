# Theme color wiring audit prompt

Audit FocusTimer's theme color wiring across the Settings window, timer widget, and context menus before any visual redesign. This is a read-only analysis. Do not edit code, documentation, specs, or theme files.

## Goal

Explain why changing a theme color updates some Settings text and controls but leaves others unchanged, and why some color changes also affect widget buttons. Trace the same properties through both widget modes and every context menu. Produce a concrete mapping that gives each area consistent theme behavior without accidental coupling.

## Inspect

- `src/FocusTimer.Core/Models/Theme.cs`, including serialized properties and built-in theme values.
- `src/FocusTimer.App/Services/ThemeManager.cs` and its tests.
- `src/FocusTimer.App/Styles/ThemeResources.axaml`, `ControlStyles.axaml`, and `Tokens.axaml`.
- `src/FocusTimer.App/App.axaml` and the Settings view, view model, and code-behind.
- `src/FocusTimer.App/Views/TimerWidgetWindow.axaml`, `FullModeView.axaml`, `CompactModeView.axaml`, their code-behind and view model, and widget-related converters.
- Context-menu creation and styling, including `src/FocusTimer.App/App.axaml.cs`, `src/FocusTimer.App/Services/TrayStateController.cs`, and the `ContextMenu` style in `ControlStyles.axaml`. Distinguish Avalonia context menus from the tray's native `NativeMenu`; determine which one users actually see and what the operating system controls.
- `design/TOKEN_MAPPING.md`, relevant OpenSpec requirements, and any other code that reads or writes these theme resources.

Treat design documents as intended behavior, not proof of current behavior. Trace each color from its `Theme` property through runtime resource updates, style selectors, and the control that renders it. Account for Avalonia Fluent defaults, inherited properties, selector precedence, dynamic resources, widget opacity layers, native menu rendering, and live theme updates. Distinguish code-confirmed behavior from behavior that needs a running UI check.

## Questions to answer

1. Which theme properties visibly affect Settings backgrounds, text, tabs, accordions, inputs, checkboxes, sliders, buttons, and focus indicators? Which are unused, overridden, or only partially applied?
2. Where does the Settings window use different text roles across tabs or fall back to Fluent colors? Identify cases where editing a named text color does not affect the control a user would expect.
3. Which properties or semantic brushes couple Settings actions to widget buttons? In particular, trace `ButtonNormal`, `ActionPrimaryBrush`, and `AccentPrimary` separately.
4. In full and compact widget modes, which theme properties control the clock, shell, border, project field, icons, button states, minimize button, and focus states? Where are a resource and a separate opacity setting applied to the same visual?
5. Which context menu is actually shown from the tray or widget? Which menu colors respond to theme changes, which are set by Fluent or Windows, and which exposed theme properties have no effect there?
   Determine whether the native menu can realistically follow the app theme. If it cannot, compare keeping the accessible system menu with using a themeable app menu, including behavior and maintenance tradeoffs. Do not assume that an Avalonia `ContextMenu` style changes a native `NativeMenu`.
6. Does every tab property map to its own resource and rendered state? Check `TabSelectedBackground` against the value assigned to `TabSelectedBackgroundColor`.
7. Do built-in themes and imported `.fttheme` files retain usable text contrast across Settings, the widget, and menus? Identify specific risks without claiming a contrast result that was not measured.
8. How do live preview, Apply, OK, and Cancel affect theme changes across all three areas? Flag any behavior that would make the proposed color separation confusing.

## Deliverable

Return a concise report with:

1. A short diagnosis of the main causes, with file and line references.
2. A mapping table: theme property, resource key, Settings consumer, widget consumer, menu consumer, observed status, and recommended role. Mark native-menu colors as system-controlled where applicable.
3. Proposed semantic roles for Settings surfaces, text, fields, tabs, focus, and actions; widget shell, clock, and controls; and themeable menus. State which existing serialized properties can back each role and which need a distinct role. Keep widget control colors separate from Settings action colors. Recommend a native or themeable menu direction based on what the platform actually permits.
4. A prioritized implementation plan limited to color wiring. Do not redesign layout or change control geometry in this plan.
5. Focused verification steps covering every Settings tab, both widget modes, the actual tray or widget context menu, built-in themes, a custom/imported theme, hover/pressed/disabled/focus states, and live updates.

Do not implement the plan. Call out uncertainty where static inspection cannot establish the rendered result.
