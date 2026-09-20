# Proposal: Introduce UI Design System

## Why

FocusTimer currently suffers from fragmented visual style ownership (split between `ThemeResources.axaml`, legacy `FocusTimerTheme.axaml`, local `Window.Styles`, and raw inline XAML attributes), inconsistent control semantics between Full and Compact modes, hardcoded magic numbers for sizes and spacing in both XAML and ViewModels, an accidental material fallback order (`Transparent, Blur, AcrylicBlur, Mica` falling back to `Transparent`), and accessibility gaps such as globally non-focusable icon buttons and missing focus rings.

Introducing a coherent, layered UI design system based on `design/UI_DESIGN_STANDARD.md` and `design/FOCUSTIMER_UI_ADDENDUM.md` establishes a single authoritative source of truth, eliminates duplicate styling, guarantees robust material fallbacks, and aligns Full and Compact widget modes as unified presentations of one component library—all while preserving existing Avalonia architecture, MVVM contracts, `.fttheme` theme persistence, and all 7 built-in themes.

Related OpenIssues backlog items:
- **OI-13 (Widget UI: Minimise behavior and visibility)**: Addressed in part by correcting icon button hit-target sizing, visual contrast, and focus states.
- **OI-03 (Widget UI: Window position and size)**: Supported by decoupling canonical widget base dimensions and spacing from ad-hoc view margins.

## What Changes

- **Design System Architecture**: Introduce a strict 3-tier resource architecture (`Theme` / `.fttheme` palette values -> `ThemeManager` -> semantic UI tokens -> component styles -> views).
- **Style Source-of-Truth Cleanup**: Deprecate and remove obsolete orphaned styles (`FocusTimerTheme.axaml`), eliminate redundant view-local `Window.Styles` in `SettingsWindow.axaml`, and centralize authoritative styles and resources in `FocusTimer.App/Styles/`.
- **Tokenized Visual Foundations**: Establish semantic resources for standard spacing intervals (4px/8px rhythm), corner radii scale (`radius-xs` through `radius-lg`), semantic typography roles (tabular numerals for timer display), depth/border tokens, and state colors.
- **Full and Compact Mode Alignment**: Align Full and Compact widget modes as two density arrangements of a single unified component system. Equivalent actions (such as Start/Pause and widget toggles) consume identical semantic roles, state brushes, and interaction behavior.
- **Material Strategy and Fallback Ordering**: Reorder Avalonia transparency hints to follow a deliberate hierarchy (`Mica, AcrylicBlur, Blur, Transparent`), provide a guaranteed readable solid or near-solid fallback surface (`TransparencyBackgroundFallback`), eliminate conflicting negative margins, and maintain a single composited material surface without unnecessary nested glass layers.
- **Accessibility Foundations**: Enable keyboard focus on interactive icon buttons, establish distinct 2px high-contrast focus rings, separate visual icon dimensions from interactive hit-target bounds (meeting WCAG 24x24px minimums), ensure High Contrast theme support, and respect reduced-transparency/reduced-motion modes.
- **Responsive Scaling & Canonical Dimensions**: Move canonical base sizing dimensions (timer font size, button target dimensions, icon sizes, text widths) from raw magic numbers in `TimerWidgetViewModel` into design-system constants/resources, maintaining responsive multiplier scaling while anchoring base dimensions.
- **Settings and Content Solidification**: Standardize `SettingsWindow` and supporting dialogs on predominantly solid/near-solid surfaces, structuring hierarchy via typography, spacing, and subtle separators rather than decorative glass cards.
- **Full Compatibility Guaranteed**: Strictly preserve `.fttheme` JSON import/export schemas, runtime dynamic theme switching, all 7 built-in themes (Dark, Light, Monokai, Solarized Dark, Nord, Dracula, High Contrast), user opacity sliders, and core timer services.

## Capabilities

### New Capabilities
- `ui-design-system`: Defines authoritative design-system resource layers (palette, semantic, component), standard spacing/radius/typography tokens, shared component styles, accessible hit-target and focus standards, and surface hierarchy across widget and dialog surfaces.

### Modified Capabilities
- `theming`: Enforces the invariant that theme variants change palette and material values without altering component geometry, spacing, typography hierarchy, or interaction semantics, while guaranteeing backward compatibility for `.fttheme` import/export and the 7 built-in themes.
- `widget-ui`: Mandates shared component roles across Full and Compact modes, specifies deliberate OS material fallback ordering with readable non-transparent fallbacks, and grounds responsive scaling in canonical design-system base dimensions.

## Impact

- **Affected Code**:
  - `src/FocusTimer.App/Styles/`: Refactor `ThemeResources.axaml` into clear token tiers; delete unused legacy `FocusTimerTheme.axaml`; add component style resources.
  - `src/FocusTimer.App/Services/ThemeManager.cs`: Extend runtime resource dictionary management to populate semantic and component brush/metric resources from active theme palette values.
  - `src/FocusTimer.App/Views/TimerWidgetWindow.axaml`: Correct transparency hints and fallback; eliminate layout hacks and negative margins; standardize shell layout.
  - `src/FocusTimer.App/Views/FullModeView.axaml` & `CompactModeView.axaml`: Refactor to consume shared component styles, unified button semantics, and design tokens.
  - `src/FocusTimer.App/Views/SettingsWindow.axaml` & `ColorPickerWindow.axaml`: Remove duplicate local styles and inline colors; adopt semantic text, spacing, and input styles.
  - `src/FocusTimer.App/ViewModels/TimerWidgetViewModel.cs`: Replace hardcoded sizing magic numbers with design-system base constants.
  - `src/FocusTimer.Core/`: No breaking model changes; `Theme.cs` and `ThemeService.cs` maintain full backwards compatibility for all serialized JSON properties.
- **Dependencies**: No new external dependencies required; uses existing Avalonia 11 primitives and `Material.Icons.Avalonia`.
- **Breaking Changes**: None. Existing custom `.fttheme` files and user settings remain 100% compatible.
