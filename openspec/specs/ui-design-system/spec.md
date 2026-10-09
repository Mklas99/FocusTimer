# Ui Design System Specification

## Purpose
Defines the core design-system architecture, resource tiers, standard scales, shared component styles, and accessibility requirements across FocusTimer application surfaces.

## Requirements

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

User-selected timer WidgetScale is an exception to the fixed target-size rule: widget button areas scale with the clock to preserve proportions below 1x. Desktop editors retain their minimum target sizes.

#### Scenario: User navigates controls via keyboard
- **WHEN** the user presses the Tab key to navigate between widget controls
- **THEN** interactive icon buttons receive focus sequentially and display a visible focus indicator with at least 3:1 contrast against the surface

#### Scenario: Pointer interaction on compact icon button
- **WHEN** the user clicks or taps an icon button with a visual icon smaller than 24px
- **THEN** the pointer hit-test area encompasses at least 24x24 pixels to ensure effortless interaction

### Requirement: Content Surface Solidity
Content-dense and configuration surfaces SHALL use solid or near-solid content backgrounds for legibility. Settings and Worklog MAY use one dense-frost outer shell, with sharp foreground content. Hierarchy SHALL use typography, alignment, and restrained dividers. Group cards SHALL remain near-solid and SHALL NOT form nested translucent layers.

#### Scenario: User navigates Settings
- **WHEN** the user views or scrolls Settings tabs
- **THEN** text and controls rest on solid or near-solid surfaces without transparency-induced contrast degradation

#### Scenario: User reads Worklog
- **WHEN** the user scans entries, timeline labels, or summary values
- **THEN** the data remains sharp on a solid or near-solid surface even when the outer window uses frost

#### Scenario: Cards contain controls
- **WHEN** a page uses group cards
- **THEN** cards contain compact rows without additional translucent cards nested inside them

### Requirement: Accessibility and Reduced-Transparency Fallback
The UI design system SHALL adapt to system accessibility modes, including High Contrast themes, reduced transparency, and reduced motion.

#### Scenario: System requests reduced transparency
- **WHEN** the user or operating system enables reduced transparency
- **THEN** material and translucent surfaces switch to their designated solid or near-solid fallback colors without altering layout or control positions

#### Scenario: High Contrast theme active
- **WHEN** the High Contrast theme is selected
- **THEN** surfaces, text, borders, and interactive states render with maximum contrast conforming to high-contrast requirements

### Requirement: Dense-frost desktop shells
Settings and Worklog SHALL use a restrained dense-frost shell on supported Windows systems where native blur is visually verified, with a near-solid tint and subtle edge/depth. Foreground content SHALL remain sharp. Unavailable effects, reduced transparency, or High Contrast SHALL select a solid fallback without changing layout. A successful API response alone SHALL NOT establish blur support.

#### Scenario: Native frost is available
- **WHEN** Settings or Worklog renders over a detailed desktop background with native blur enabled
- **THEN** the background behind the shell is visibly softened while labels, fields, and data remain sharp and readable

#### Scenario: Native frost is unavailable
- **WHEN** the native effect cannot be applied or does not visibly blur the background
- **THEN** the window uses the designated solid fallback rather than exposing an unblurred transparent shell

#### Scenario: Accessibility fallback is selected
- **WHEN** reduced transparency or High Contrast is active
- **THEN** the window uses a solid accessible surface and retains the same control positions and dimensions

### Requirement: Compact rows and logical group cards
Settings and app-owned editors SHALL use aligned compact label/control rows by default. A page SHALL use cards only where at least two meaningful peer groups benefit from grouping; a lone section SHALL NOT receive a decorative card. Cards SHALL contain compact rows and adapt to the available width without clipping labels or controls.

#### Scenario: General settings use rows
- **WHEN** General settings are displayed
- **THEN** Startup, Window, and Break reminders use compact sections and aligned controls without a card around each setting

#### Scenario: Appearance contains peer groups
- **WHEN** Appearance presents widget layout and widget opacity groups
- **THEN** the groups appear as separate near-solid cards with compact rows inside, arranged beside each other only when they fit

#### Scenario: Available width narrows
- **WHEN** cards or label/control rows cannot fit their preferred arrangement
- **THEN** they stack or reflow while preserving readable labels, usable controls, and the standard spacing rhythm

### Requirement: Native clarity typography
Settings, Worklog, and their app-owned editors SHALL use Segoe UI on Windows with a system sans-serif fallback elsewhere. Page titles, section titles, body labels, and helper text SHALL follow a shared restrained hierarchy. Durations and numeric values SHALL align consistently. This refresh SHALL NOT change timer-widget typography.

#### Scenario: User moves between desktop views
- **WHEN** the user opens Settings, Worklog, or an app-owned editor
- **THEN** the same font family and hierarchy distinguish page titles, sections, fields, and supporting text

#### Scenario: Numeric content changes
- **WHEN** a duration, percentage, or numeric field changes value
- **THEN** its alignment remains stable within its allocated column

### Requirement: Icon-and-underline navigation
Settings and Worklog tabs SHALL show an outlined icon beside each text label, a restrained shared baseline, and an underline for the selected tab. They SHALL retain native tab selection, keyboard navigation, accessible names, and visible keyboard focus. Selection SHALL remain identifiable without color alone, and labels SHALL remain visible at supported window sizes.

#### Scenario: User selects a tab
- **WHEN** the user selects another tab by pointer or keyboard
- **THEN** its icon and label use the selected state and the underline moves to that tab without moving the tab row

#### Scenario: User navigates with assistive technology
- **WHEN** a tab receives keyboard or accessibility focus
- **THEN** its text name and selected state are exposed and a visible focus indicator remains distinct from its selection underline

### Requirement: Soft-rounded desktop controls
Settings, Worklog, and their app-owned editors SHALL share moderately rounded buttons, text and numeric fields, dropdowns, checkboxes, sliders, and disclosure controls. Geometry and normal, hover, pressed, disabled, invalid, read-only, and keyboard-focus states SHALL be consistent across themes. Existing widget-specific controls SHALL retain their geometry and interaction styling.

#### Scenario: User interacts with desktop controls
- **WHEN** a desktop control is hovered, pressed, disabled, invalid, read-only, or keyboard focused
- **THEN** its state remains distinguishable using shared control styles without changing its layout bounds

#### Scenario: User opens an app-owned dialog
- **WHEN** a color picker or Worklog entry editor opens
- **THEN** its app-owned controls use the same rounded geometry and typography as the parent view

#### Scenario: User switches themes
- **WHEN** a different built-in or imported theme is applied
- **THEN** desktop control geometry remains unchanged and timer-widget controls retain their existing styling boundaries

### Requirement: Desktop action hierarchy
Desktop action buttons SHALL use shared primary, secondary, and destructive roles. Primary commit actions SHALL have a restrained accent fill and semibold label; secondary cancellation actions SHALL have a neutral bordered surface; destructive worklog actions SHALL have a muted danger fill, visible outline, and semibold label. Labels and geometry SHALL identify actions without relying on color alone. Derived colors SHALL preserve stored theme values, keep enabled text at 4.5:1 contrast, and keep keyboard focus indicators at 3:1 contrast on each supported action surface.

#### Scenario: User commits or discards edits
- **WHEN** Settings, the color picker, or a Worklog entry form renders its action row
- **THEN** OK, Apply, and Save use the primary role and Cancel uses the secondary role
- **AND** labels, order, commands, click targets, and save/cancel behavior remain unchanged

#### Scenario: User deletes a worklog entry
- **WHEN** the Entries toolbar or delete confirmation renders Delete
- **THEN** Delete uses the destructive role without bypassing the existing confirmation

#### Scenario: User focuses or disables an action
- **WHEN** an action receives keyboard focus while normal, hovered, or pressed
- **THEN** a visible focus ring appears without moving or resizing the button
- **AND** disabled actions use the shared neutral disabled state in every pointer state
