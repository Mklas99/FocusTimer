## Purpose

Defines the core design-system architecture, resource tiers, standard scales, shared component styles, and accessibility requirements across FocusTimer application surfaces.

## ADDED Requirements

### Requirement: Three-Tier Token Architecture
The system SHALL organize styling resources into three distinct tiers: theme palette values, semantic UI resources defining functional intent, and component-level tokens and styles consumed by views. Feature views SHALL consume semantic or component resources rather than raw palette colors.

#### Scenario: View consumes semantic tokens
- **WHEN** a feature view or component renders text, surfaces, borders, or actions
- **THEN** it resolves colors and brushes from semantic keys (such as `PrimaryTextBrush`, `WindowBackgroundBrush`, or `AccentPrimaryBrush`) rather than hardcoded colors or direct palette tokens

#### Scenario: Theme variant switches palette
- **WHEN** the user switches active themes
- **THEN** `ThemeManager` repopulates the semantic and component resource dictionaries from the new palette values while component geometry and hierarchy remain unchanged

### Requirement: Single Authoritative Style Source
The application SHALL maintain a single authoritative source of truth for global styles, brushes, and control templates under `FocusTimer.App/Styles/`, and SHALL NOT maintain duplicate or conflicting style definitions across legacy files or view-local overrides.

#### Scenario: Application startup loads authoritative styles
- **WHEN** the application starts up
- **THEN** it loads styles exclusively from the authoritative design-system resources without referencing deprecated or duplicate style files

#### Scenario: Settings view consumes shared styles
- **WHEN** the Settings window renders section headers, labels, and form controls
- **THEN** it uses shared design-system classes and styles rather than redundant local `Window.Styles`

### Requirement: Standard Spacing and Geometry Scales
The design system SHALL expose tokenized standard spacing intervals based on an 8px rhythm (with 4px sub-intervals) and a restrained corner radius scale, which views and components SHALL consume for recurring layouts.

#### Scenario: Layout adopts standard spacing
- **WHEN** controls, margins, and panel paddings are laid out
- **THEN** recurring spacing values resolve to standard scale tokens (4, 8, 12, 16, 24, 32px) rather than arbitrary pixel values

#### Scenario: Nested surfaces maintain concentric radii
- **WHEN** child containers or controls sit inside a rounded parent container
- **THEN** corner radii are assigned from the standard radius scale to maintain visual concentricity

### Requirement: Accessible Control Targets and Focus Rings
All interactive controls (including icon buttons and title bar controls) SHALL provide an interactive hit target of at least 24x24 pixels regardless of inner visual icon size, SHALL support keyboard focus navigation, and SHALL display a high-contrast focus indicator when focused via keyboard.

#### Scenario: User navigates controls via keyboard
- **WHEN** the user presses the Tab key to navigate between widget controls
- **THEN** interactive icon buttons receive focus sequentially and display a visible focus indicator with at least 3:1 contrast against the surface

#### Scenario: Pointer interaction on compact icon button
- **WHEN** the user clicks or taps an icon button with a visual icon smaller than 24px
- **THEN** the pointer hit-test area encompasses at least 24x24 pixels to ensure effortless interaction

### Requirement: Content Surface Solidity
Content-dense and configuration surfaces, including `SettingsWindow` and data views, SHALL use predominantly solid or near-solid backgrounds to prioritize legibility and scanning, and SHALL establish hierarchy through typography, alignment, and restrained dividers rather than nested translucent cards.

#### Scenario: User navigates Settings
- **WHEN** the user views or scrolls settings tabs
- **THEN** controls and configuration sections rest on solid or near-solid surfaces that do not exhibit transparency-induced contrast degradation

### Requirement: Accessibility and Reduced-Transparency Fallback
The UI design system SHALL adapt to system accessibility modes, including High Contrast themes, reduced transparency, and reduced motion.

#### Scenario: System requests reduced transparency
- **WHEN** the user or operating system enables reduced transparency
- **THEN** material and translucent surfaces switch to their designated solid or near-solid fallback colors without altering layout or control positions

#### Scenario: High Contrast theme active
- **WHEN** the High Contrast theme is selected
- **THEN** surfaces, text, borders, and interactive states render with maximum contrast conforming to high-contrast requirements
