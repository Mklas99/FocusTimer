## 1. Design Governance and Documentation

- [x] 1.1 Document canonical design metrics and token map in `FocusTimer.App/Styles/DesignMetrics.cs` and verify file compiles with StyleCop compliance
- [x] 1.2 Establish token mapping documentation linking palette properties in `Theme.cs` to semantic resources in `ThemeResources.axaml` and verify all 7 built-in themes map to every required semantic role

## 2. Style Source-of-Truth Cleanup

- [x] 2.1 Remove obsolete and unreferenced `FocusTimer.App/Styles/FocusTimerTheme.axaml` and verify solution builds without references to it
- [x] 2.2 Verify and clean `App.axaml` style imports so only authoritative design-system resources are loaded

## 3. Design-Token and Resource Foundation

- [x] 3.1 Create `FocusTimer.App/Styles/Tokens.axaml` containing spacing scale, corner radii scale, typography metrics, and base size constants, and include it in `App.axaml`
- [x] 3.2 Refactor `FocusTimer.App/Styles/ThemeResources.axaml` to define semantic surface, text, border, and state color/brush resources
- [x] 3.3 Update `ThemeManager.cs` to populate semantic resources and component brushes from active theme palette values at runtime, and verify unit tests for `ThemeManager` pass

## 4. Shared Component Styles

- [x] 4.1 Create `FocusTimer.App/Styles/ControlStyles.axaml` with standardized `Button.icon-button` styles enforcing min 24x24px hit target, `Focusable="True"`, and `:focus-visible` focus ring
- [x] 4.2 Define standardized `TextBlock.timer-text` with tabular monospace font family and semantic foreground binding
- [x] 4.3 Define standardized `TextBox.project-tag`, settings section headers, and settings labels in `ControlStyles.axaml`, and verify all control styles load in Avalonia previewer/compilation

## 5. Widget Shell Migration

- [x] 5.1 Update `TimerWidgetWindow.axaml` transparency hint order to `Mica, AcrylicBlur, Blur, Transparent` and set `TransparencyBackgroundFallback` to `WindowBackgroundBrush`
- [x] 5.2 Remove hardcoded negative layout margins (`Margin="10,-10,10,5"`) and eliminate nested duplicate translucent borders in `TimerWidgetWindow.axaml`
- [x] 5.3 Update top-right minimize button in `TimerWidgetWindow.axaml` to meet the 24x24px interactive hit target with visible focus adorner, verifying drag and minimize behaviors remain functional

## 6. Full and Compact Mode Migration

- [x] 6.1 Refactor `FullModeView.axaml` to consume shared component styles, semantic action brushes for Start/Pause, and standard spacing tokens
- [x] 6.2 Refactor `CompactModeView.axaml` to consume the identical semantic action role for Start/Pause and unified mode toggle iconography
- [x] 6.3 Update `TimerWidgetViewModel.cs` to anchor `UpdateResponsiveLayout` to `DesignMetrics` base constants instead of magic numbers, and verify switching between Full and Compact modes preserves theme colors and action states

## 7. Settings UI Migration

- [x] 7.1 Remove duplicate local `<Window.Styles>` in `SettingsWindow.axaml` and switch headers and labels to shared global style classes
- [x] 7.2 Replace hardcoded hex colors and inline borders in `SettingsWindow.axaml` with semantic brushes and tokenized spacing, verifying tabs remain readable and visually structured on solid surfaces

## 8. Supporting Surfaces Migration

- [x] 8.1 Update `ColorPickerWindow.axaml` to consume shared button, input, and label styles with tokenized spacing
- [x] 8.2 Standardize tooltips, context flyouts, and dialog scrims on shared transient semantic brushes, verifying visual alignment with the design system

## 9. Accessibility and Fallback Verification

- [x] 9.1 Verify keyboard focus navigation across widget controls and Settings tabs using Tab/Shift+Tab and confirm 2px high-contrast focus rings display on all focused controls
- [x] 9.2 Verify High Contrast theme renders crisp borders and high-contrast text on all interactive surfaces
- [x] 9.3 Verify widget rendering when OS composition is disabled or reduced transparency is requested, confirming solid fallback surfaces maintain full legibility

## 10. Visual and Theme Regression Validation

- [x] 10.1 Verify all 7 built-in themes (Dark, Light, Monokai, Solarized Dark, Nord, Dracula, High Contrast) switch cleanly at runtime without layout jumping or missing brushes
- [x] 10.2 Verify custom `.fttheme` export and import functionality remains 100% compatible and applies correctly
- [x] 10.3 Run test suite via `dotnet test` and format checks via `dotnet format --verify-no-changes` to verify no regressions
