# Design: Coherent Modern UI Design System

## Context

FocusTimer is a desktop timer widget built with Avalonia 11, .NET 8, and ReactiveUI. Its current UI implementation exhibits fragmented styling:
- Dynamic palette colors are updated at runtime via `ThemeManager.cs` into `Application.Current.Resources`.
- Styles are split across `ThemeResources.axaml`, legacy unused `FocusTimerTheme.axaml`, view-local `Window.Styles` in `SettingsWindow.axaml`, and inline property values in views.
- Full and Compact widget modes have diverged: Full mode uses a grid layout with `SecondaryTextBrush` for Start/Pause, while Compact mode uses a stack layout with `ButtonNormalBrush` and different toggle icons (`Menu` vs `ViewHeadline`).
- Avalonia window transparency hint in `TimerWidgetWindow.axaml` is currently `Transparent, Blur, AcrylicBlur, Mica` with `TransparencyBackgroundFallback="Transparent"`, causing an accidental fallback to total transparency instead of a readable surface.
- Sizing scaling is managed in `TimerWidgetViewModel.UpdateResponsiveLayout` through undocumented magic numbers (`36`, `25`, `175`, `115`, `25`, `28`, `0.7`, `1.25`).
- Icon buttons globally disable keyboard focus (`Focusable="False"`) and lack visible focus adorners.

The repository's normative design documents (`design/UI_DESIGN_STANDARD.md` and `design/FOCUSTIMER_UI_ADDENDUM.md`) prescribe a calm, quiet, precise desktop utility aesthetic with restrained translucency, soft geometry, 8px/4px spacing rhythm, distinct hit targets, and strict single-source-of-truth governance.

See `proposal.md` for background and requirement motivation.

## Goals / Non-Goals

**Goals:**
- Establish a strict 3-tier resource architecture: Theme palette (`Theme` / `.fttheme`) → `ThemeManager` → Semantic resources → Component tokens/styles → Views.
- Establish a single authoritative styling source in `FocusTimer.App/Styles/`, deleting the orphaned `FocusTimerTheme.axaml` and eliminating local `Window.Styles` duplicates in `SettingsWindow.axaml`.
- Unify Full and Compact widget modes under shared semantic component styles and identical action roles (Start/Pause, resets, toggles).
- Define a deliberate platform material fallback hierarchy (`Mica, AcrylicBlur, Blur, Transparent`) with a solid/near-solid fallback for legibility.
- Ensure accessible interactive hit targets (minimum 24x24px), re-enable keyboard navigation for widget controls, and render high-contrast focus rings.
- Extract canonical sizing metrics from `TimerWidgetViewModel` into documented design constants.
- Maintain solid/near-solid content surfaces for Settings and dialogs without nested glass cards.
- Guarantee 100% backward compatibility for all 7 built-in themes and existing `.fttheme` import/export files.

**Non-Goals:**
- Ground-up rewrite of the application, MVVM layers, or window management.
- Breaking or modifying the `.fttheme` JSON schema.
- Implementing unrelated backlog features (e.g., Pomodoro automation, CSV schema changes, rules engine, or editable hotkeys).
- Applying glass or acrylic effects across data-dense settings views.
- Introducing third-party UI component libraries (maintaining pure Avalonia 11 + `Material.Icons.Avalonia`).

## Decisions

### Decision 1: Resource Architecture and File Organization
We structure the design-system resources under `src/FocusTimer.App/Styles/` into modular files included via `App.axaml`:
- `Tokens.axaml`: Invariant design tokens—standard spacing scale (4, 8, 12, 16, 24, 32px), corner radii scale (`radius-xs` 4px, `radius-sm` 8px, `radius-md` 12px, `radius-lg` 16px), typography scale, and canonical sizing constants.
- `ThemeResources.axaml`: Dynamic color and brush definitions for palette tokens, semantic surfaces/text/borders, and component aliases. Populated and refreshed at runtime by `ThemeManager`.
- `ControlStyles.axaml`: Authoritative component styles for buttons (`Button.icon-button`, primary action styles), timer display (`TextBlock.timer-text`), text inputs (`TextBox.project-tag`), settings headers/labels, and focus indicators.
- Delete `FocusTimerTheme.axaml` after verifying zero references.

*Rationale:* Separating invariant geometry/metrics from dynamic theme colors ensures theme switching modifies only palette and material values without touching layout geometry or typography hierarchy.

*Alternatives considered:* Keeping all styles in a single monolithic `ThemeResources.axaml`. Rejected because mixing dynamic brush keys with static control templates and metric tokens obscures style ownership and complicates incremental migration.

### Decision 2: Deliberate Platform Material Fallback
In `TimerWidgetWindow.axaml`:
```xml
TransparencyLevelHint="Mica, AcrylicBlur, Blur, Transparent"
TransparencyBackgroundFallback="{DynamicResource WindowBackgroundBrush}"
```
The shell uses a single composited surface with a solid or near-solid fallback brush. Inner layout removes negative margin hacks (such as `Margin="10,-10,10,5"`).

*Rationale:* Placing `Transparent` first in the previous hint list caused Avalonia to prefer total transparency over native backdrops, degrading legibility when composition is restricted. The new order prioritizes native backdrops and falls back gracefully to a solid brush.

*Alternatives considered:* Forcing pure Acrylic or Mica via native Win32 interop. Rejected because Avalonia's cross-platform abstraction handles platform negotiation safely, provided the hint order and fallback brush are deliberate.

### Decision 3: Accessible Hit Targets and Keyboard Focus
For `Button.icon-button`:
- Set `MinWidth="24"` and `MinHeight="24"` (conforming to WCAG 2.5.8 target size).
- Set `Focusable="True"` (removing `<Setter Property="Focusable" Value="False"/>`).
- Define explicit `:focus-visible` pseudo-class styles with a 2px high-contrast focus border.
- The visual icon inside remains compact (e.g. 16px) with centered alignment.

*Rationale:* Floating widgets must be operable via keyboard and touch/pen without microscopic hit targets or inaccessible controls.

*Alternatives considered:* Keeping `Focusable="False"` to prevent focus rectangles on click. Rejected because modern Avalonia supports `:focus-visible` to display indicators only when navigating via keyboard, preserving clean mouse clicks while providing accessibility.

### Decision 4: Responsive Scaling Anchors
In `TimerWidgetViewModel.cs`, define a static class or constants:
```csharp
public static class WidgetDesignMetrics
{
    public const double BaseMainTimerFontSize = 36.0;
    public const double BaseCompactTimerFontSize = 24.0;
    public const double BaseMainTimerWidth = 175.0;
    public const double BaseCompactTimerWidth = 115.0;
    public const double BaseProjectFontSize = 14.0;
    public const double BaseButtonSize = 28.0;
    public const double BaseIconSize = 18.0;
}
```
`UpdateResponsiveLayout()` calculates responsive values as direct proportional products of `Settings.WidgetScale` and the base metrics.

*Rationale:* Moving magic numbers to explicit design constants aligns the C# ViewModel with XAML design tokens while preserving the responsive scaling mechanism.

*Alternatives considered:* Replacing ViewModel scaling with a XAML `Viewbox`. Rejected because `Viewbox` scaling can cause subpixel text blurring in Avalonia and prevents fine-grained control over hit targets.

### Decision 5: Full and Compact Visual Role Parity
Align `FullModeView.axaml` and `CompactModeView.axaml`:
- Both use `Button.icon-button` with identical state brushes for Start/Pause (`AccentPrimaryBrush` or `ButtonNormalBrush`).
- Both use the same icon pair for mode switching (`ChevronDown` / `Menu`).
- Both use `TextBlock.timer-text` with tabular numeral fonts (`FontFamily="Consolas, Segoe UI Mono, monospace"`).
- Remove ad-hoc local margins and let container spacing dictate layout rhythm.

*Rationale:* Full and Compact modes are two density layouts of one product, not two different visual designs.

### Decision 6: Settings Window Hierarchy and Solidity
- Remove local `<Window.Styles>` in `SettingsWindow.axaml`.
- Use global `TextBlock.settings-section-header` and `TextBlock.settings-label`.
- Replace hardcoded hex colors (`#30FFFFFF`, `#12000000`) with semantic brush resources (`BorderSubtleBrush`, `SurfaceSubtleBrush`).
- Maintain solid/near-solid background without glass cards on each setting item.

*Rationale:* Dense forms require high contrast and stable scanning surfaces.

## Risks / Trade-offs

| Risk | Mitigation |
|---|---|
| Custom `.fttheme` files may contain extreme contrast or unexpected hex values | `ThemeManager` includes fallback parsing and clamps opacity to safe ranges (0.0–1.0) |
| Expanding button hit-targets (min 24x24px) might expand the widget perimeter slightly | Offset expanded hit-targets with compact internal padding and test both Full and Compact layouts across all scale factors |
| Removal of `FocusTimerTheme.axaml` could cause missing resource errors if referenced | Repository-wide grep confirmed zero usages; build and test will verify prior to completion |
| Transparency fallback rendering differences across Windows 10 vs 11 | Fallback background brush is explicitly set to `WindowBackgroundBrush`, guaranteeing opacity when platform backdrops are unavailable |

## Migration Plan

The implementation follows an incremental 10-phase sequence:
1. **Design Governance & Metric Constants**: Define standard metrics and token mappings.
2. **Source-of-Truth Cleanup**: Deprecate and remove `FocusTimerTheme.axaml`; clean `App.axaml`.
3. **Token & Resource Foundation**: Create `Tokens.axaml`, refactor `ThemeResources.axaml`, and update `ThemeManager.cs` to map palette to semantic resources.
4. **Shared Component Styles**: Author `ControlStyles.axaml` for buttons, inputs, timer text, and focus indicators.
5. **Widget Shell Migration**: Update `TimerWidgetWindow.axaml` (transparency hints, fallback, remove layout hacks).
6. **Full & Compact Mode Migration**: Standardize `FullModeView.axaml` and `CompactModeView.axaml` on shared components and action semantics.
7. **Settings UI Migration**: Clean `SettingsWindow.axaml` local styles and adopt semantic tokens.
8. **Supporting Surfaces Migration**: Update `ColorPickerWindow.axaml` and transient dialogs.
9. **Accessibility & Fallback Verification**: Validate keyboard navigation, focus indicators, High Contrast, and reduced-transparency fallback.
10. **Visual & Theme Regression Validation**: Verify all 7 built-in themes, `.fttheme` import/export, and scaling live.

## Open Questions

- *Style File Bundling vs Separation*: Whether to split XAML resources into `Tokens.axaml`, `ThemeResources.axaml`, and `ControlStyles.axaml` included via `App.axaml`, or keep them within an expanded `ThemeResources.axaml`. (Recommended: modular separation for clean separation of concerns).
- *Minimize Button Placement*: Whether the minimize button in `TimerWidgetWindow.axaml` should remain at the top-right shell edge with updated 24x24 hit target or be integrated into the control row. (Resolved during shell migration).
