## MODIFIED Requirements

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

## ADDED Requirements

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
