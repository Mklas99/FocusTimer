# Tasks

## 1. Correct theme resource mapping

- [x] 1.1 Add focused `ThemeManagerTests` using distinct `InputFocusBorder`, `TabSelectedBackground`, and `AccentPrimary` values; verify the two published colors and brushes retain their own values during live theme changes.
- [x] 1.2 Update `ThemeManager` to publish those two properties under their matching resource keys; verify the focused tests and App test project pass.
- [x] 1.3 Update `design/TOKEN_MAPPING.md` for the corrected input-focus and selected-tab roles; verify the names agree with `Theme.cs` and `ThemeResources.axaml`.

## 2. Connect Settings control colors

- [x] 2.1 Add Settings-scoped shared text and field styles for ordinary, heading, label, disabled, editable, and read-only roles across General, Logging, Appearance, Hotkeys, and About; verify a running Settings window shows the matching colors and the widget project field keeps its own styling.
- [x] 2.2 Connect normal, hover, selected, text, and keyboard-focus tab states to the existing tab resources, including rendered Fluent template parts where needed; verify each state responds to a live theme edit in a running Settings window.
- [x] 2.3 Add focused UI/resource tests for representative Settings text, field, and tab state values and dynamic theme updates; verify `dotnet test tests/FocusTimer.App.Tests/FocusTimer.App.Tests.csproj` passes.

## 3. Verify built-in readability

- [x] 3.1 Measure rendered Settings text/surface pairs and selected-tab/focus indicators in all seven built-in themes; verify enabled normal text reaches 4.5:1 and selected/focus indicators reach 3:1, recording the measured pairs.
- [x] 3.2 Correct any failing built-in theme colors without changing theme-file fields or control geometry; verify the contrast measurements and a focused Light/High Contrast manual state check pass.
- [x] 3.3 Update `docs/versions/current/OpenIssues.md` and the theme color wiring audit with delivered scope, remaining OI-21/OI-24 work, and current widget/preview behavior; verify those documents agree with the implementation and one-accordion Appearance layout.
