# FocusTimer Token Mapping Reference

This document maps raw `Theme.cs` / `.fttheme` palette properties to Avalonia semantic keys and component roles managed by `ThemeManager`.

## 1. Architecture Flow

```text
Theme / .fttheme (JSON / POCO)
  ↓ (Palette values)
ThemeManager.cs
  ↓ (Dynamic Resource Dictionary update)
Semantic Resources (WindowBackgroundBrush, PrimaryTextBrush, etc.)
  ↓
Component Styles (Button.icon-button, TextBlock.timer-text, etc.)
  ↓
Views (FullModeView, CompactModeView, SettingsWindow, etc.)
```

## 2. Palette to Semantic Mapping Table

| Theme.cs Property | Type | Avalonia Semantic Key | Semantic Role |
|---|---|---|---|
| `WindowBackground` | Hex Color | `WindowBackgroundColor`, `WindowBackgroundBrush`, `WidgetShellTintBrush`, `WidgetShellFallbackBrush`, `WidgetShellActiveBrush` | Widget tint color and opaque material fallback; active brush follows the achieved backdrop |
| `WindowForeground` | Hex Color | `WindowForegroundColor`, `WindowForegroundBrush` | High-emphasis window chrome |
| `WindowBorder` | Hex Color | `WindowBorderColor`, `WindowBorderBrush` | Widget window outline |
| `PrimaryText` | Hex Color | `PrimaryTextColor`, `PrimaryTextBrush` | Main typography foreground |
| `SecondaryText` | Hex Color | `SecondaryTextColor`, `SecondaryTextBrush` | Secondary/muted labels, subtle icons |
| `DisabledText` | Hex Color | `DisabledTextColor`, `DisabledTextBrush` | Inactive/disabled content |
| `TimerText` | Hex Color | `TimerTextColor`, `TimerTextBrush` | Hero elapsed/remaining digits |
| `TimerBackground` | Hex Color | `TimerBackgroundColor`, `TimerBackgroundBrush` | Backdrop immediately behind timer digits |
| `ButtonNormal` | Hex Color | `ButtonNormalColor`, `ButtonNormalBrush` | Primary action controls (Start/Pause, icons) |
| `ButtonHover` | Hex Color | `ButtonHoverColor`, `ButtonHoverBrush` | Hover state for interactive controls |
| `ButtonPressed` | Hex Color | `ButtonPressedColor`, `ButtonPressedBrush` | Active pressed state for controls |
| `ButtonDisabled` | Hex Color | `ButtonDisabledColor`, `ButtonDisabledBrush` | Disabled button background/foreground |
| `AccentPrimary` | Hex Color | `AccentPrimaryColor`, `AccentPrimaryBrush` | Brand highlight, focus indicators, toggles |
| `AccentSecondary` | Hex Color | `AccentSecondaryColor`, `AccentSecondaryBrush` | Secondary accent |
| `DangerColor` | Hex Color | `DangerColor`, `DangerBrush` | Destructive/warning/reset state |
| `SuccessColor` | Hex Color | `SuccessColor`, `SuccessBrush` | Timer completed, success confirmation |
| `WarningColor` | Hex Color | `WarningColor`, `WarningBrush` | Break reminder, warning status |
| `InputBackground` | Hex Color | `InputBackgroundColor`, `InputBackgroundBrush` | Text boxes, numeric inputs background |
| `InputBorder` | Hex Color | `InputBorderColor`, `InputBorderBrush` | Input field borders |
| `InputText` | Hex Color | `InputTextColor`, `InputTextBrush` | Input field text foreground |
| `ProjectTagBackground` | Hex Color | `ProjectTagBackgroundColor`, `ProjectTagBackgroundBrush` | Project input tag background |
| `ProjectTagBorder` | Hex Color | `ProjectTagBorderColor`, `ProjectTagBorderBrush` | Project input tag border |
| `ProjectTagText` | Hex Color | `ProjectTagTextColor`, `ProjectTagTextBrush` | Project input tag text |
| `SettingsBackground` | Hex Color | `SettingsBackgroundColor`, `SettingsBackgroundBrush` | Solid background for Settings dialog |
| `SettingsSectionHeader`| Hex Color | `SettingsSectionHeaderColor`, `SettingsSectionHeaderBrush` | Settings section header text |
| `SettingsLabelText` | Hex Color | `SettingsLabelTextColor`, `SettingsLabelTextBrush` | Settings label text |
| `TabBackground` | Hex Color | `TabBackgroundColor`, `TabBackgroundBrush` | Tab strip background |
| `TabHoverBackground` | Hex Color | `TabHoverBackgroundColor`, `TabHoverBackgroundBrush` | Tab hover state |
| `TabText` | Hex Color | `TabTextColor`, `TabTextBrush` | Tab header text |
| `TabSelectedText` | Hex Color | `TabSelectedTextColor`, `TabSelectedTextBrush` | Active tab text |

## 3. Opacity Mapping Table

| Theme.cs Property | Clamped Range | Avalonia Resource | Target Element |
|---|---|---|---|
| `BackgroundOpacity` | 0.0 – 1.0 | `WidgetShellTintBrush.Opacity` | Widget shell tint only; blur stays independent |
| `WidgetBlurMode` | Off / Soft / Strong | Window transparency hints | Background blur; lower levels are fallbacks |
| `TimerOpacity` | 0.0 – 1.0 | `TimerTextBrush.Opacity`, clock layer opacity | Elapsed time digits layer |
| `ButtonOpacity` | 0.0 – 1.0 | `ButtonNormalBrush.Opacity`, controls layer opacity | Control buttons layer |

`Settings.WidgetOpacity` fades the window as a whole. `WidgetBaseOpacity` remains readable in older theme files but does not render a second shell layer.

`WidgetShellActiveBrush` uses the selected tint opacity whenever the platform provides AcrylicBlur, Blur, or Transparent. If a blur request falls through to Transparent, the diagnostic reports the actual level and the shell remains see through. High Contrast or no transparency uses the opaque fallback.

## 4. Built-in Theme Coverage Verification

All 7 built-in themes define every property listed above:
- **Dark** (Default)
- **Light**
- **Monokai**
- **Solarized Dark**
- **Nord**
- **Dracula**
- **High Contrast**

Theme switching modifies these resources at runtime via `ThemeManager.ApplyTheme()`. Invariant geometry (spacing, corner radii, font metrics) remains constant across all themes.
