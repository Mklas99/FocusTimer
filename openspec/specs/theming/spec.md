# Theming Specification

## Purpose
Defines built-in and custom theme support for the widget and settings UI.

## Requirements

### Requirement: Optional Play/Pause color compatibility
Theme JSON SHALL support an optional playPauseColor for the normal Start/Pause icon. Missing or null values SHALL inherit ButtonNormal, including later edits to ButtonNormal. Explicit values SHALL be preserved by theme cloning, settings persistence, and theme import/export. Legacy TimerBackground values SHALL remain preserved without being reinterpreted as icon colors.

#### Scenario: Older theme is imported and edited
- **WHEN** the user imports a theme without playPauseColor and edits ButtonNormal
- **THEN** the normal Start/Pause icon follows ButtonNormal until the user supplies an explicit Play/Pause color

#### Scenario: Explicit Play/Pause color is saved
- **WHEN** the user applies an explicit valid Play/Pause color and exports or reloads the theme
- **THEN** that value and the existing Success, Danger, and legacy TimerBackground values remain intact

### Requirement: Built-In Themes
The system SHALL provide 7 built-in themes (Dark, Light, Monokai, Solarized Dark, Nord, Dracula, High Contrast), selectable in Settings and applied live. Each built-in theme SHALL provide a default widget backdrop choice; High Contrast SHALL use a solid widget shell.

#### Scenario: User selects a built-in theme
- **WHEN** the user selects a theme from Settings → Appearance
- **THEN** the widget and settings UI re-theme immediately without a restart, and the widget applies that theme's backdrop and background opacity values

#### Scenario: User restarts after editing a built-in theme
- **WHEN** the user applies a backdrop or background opacity edit while a built-in theme is selected and restarts the app
- **THEN** the saved edited values remain active instead of being replaced by that theme's factory defaults

#### Scenario: User selects High Contrast
- **WHEN** the user selects the High Contrast theme
- **THEN** the widget uses a solid, unblurred shell and retains readable text and controls

### Requirement: Custom Theme Import/Export
The system SHALL let the user import and export custom themes as `.fttheme` JSON files via a file picker, SHALL include the widget backdrop choice and background opacity in exported themes, and SHALL validate supplied backdrop values on import. Theme files created before this field existed SHALL remain importable using the compatible default Off choice.

#### Scenario: User imports a theme file
- **WHEN** the user selects a valid `.fttheme` file with a supported backdrop choice
- **THEN** the file is applied as the active theme with its backdrop and background opacity values

#### Scenario: User imports an invalid theme file
- **WHEN** the user selects a `.fttheme` file that fails validation
- **THEN** the import is rejected and the current theme is unchanged

#### Scenario: User imports an older theme file
- **WHEN** the user selects a valid `.fttheme` file without a backdrop choice
- **THEN** the theme imports with Off and preserves its other values

#### Scenario: User imports an invalid backdrop choice
- **WHEN** the user selects a `.fttheme` file with an unsupported backdrop choice
- **THEN** the import is rejected and the current theme is unchanged

#### Scenario: User exports a theme
- **WHEN** the user exports the active theme
- **THEN** its backdrop choice and background opacity are present in the exported file and survive a later import

### Requirement: Reset to Default
The system SHALL let the user restore the Dark theme with a single action.

#### Scenario: User resets theme
- **WHEN** the user clicks "Reset to default" in Settings → Appearance
- **THEN** the Dark theme is applied

### Requirement: Theme Invariant Boundaries
The system SHALL ensure that built-in themes and imported custom themes modify only palette colors, material opacity values, and the widget backdrop choice, preserving layout geometry, spacing scales, corner radii, typography hierarchy, and control interaction states across all themes.

#### Scenario: User switches between themes
- **WHEN** the user switches between Dark, Monokai, Nord, Light, Dracula, Solarized Dark, or High Contrast themes
- **THEN** widget dimensions, control alignment, font sizes, corner radii, padding, and interaction states remain identical while appearance values update live

#### Scenario: Custom theme imported
- **WHEN** the user imports a valid custom `.fttheme` file
- **THEN** palette, opacity, and backdrop values apply without altering component structure, layout bounds, or interaction states

### Requirement: Settings color roles
The system SHALL apply each existing Settings text, input, and tab theme color to the visible control role named by that color. Theme changes SHALL update those roles in the open Settings window without changing control layout or widget button colors.

#### Scenario: Text roles update live
- **WHEN** a user selects or edits a theme while Settings is open
- **THEN** ordinary text, section headings, labels, and disabled text use their corresponding theme text colors across Settings tabs

#### Scenario: Input roles update live
- **WHEN** a user changes the input background, border, text, or focus-border color
- **THEN** editable and read-only Settings fields use the matching color in their applicable normal or focused state

#### Scenario: Tab roles update live
- **WHEN** a user changes the tab background, hover background, text, selected background, or selected text color
- **THEN** Settings tabs use each matching color in the applicable normal, hovered, or selected state

#### Scenario: Independent focus and selected colors
- **WHEN** `InputFocusBorder` or `TabSelectedBackground` differs from `AccentPrimary`
- **THEN** the focused field border or selected tab background uses its named value rather than `AccentPrimary`

### Requirement: Built-in Settings color readability
The system SHALL keep Settings text, field contents, selected tabs, and keyboard-focus indicators readable in every built-in theme, including Light and High Contrast.

#### Scenario: Built-in theme review
- **WHEN** each built-in theme is applied to Settings
- **THEN** enabled normal-size text has at least 4.5:1 contrast against its rendered surface, and selected-tab and keyboard-focus indicators have at least 3:1 contrast against adjacent colors

#### Scenario: Theme switch preserves interaction states
- **WHEN** a user switches between built-in themes with a field focused or a tab selected
- **THEN** the field remains visibly focused and the selected tab remains identifiable without changing control geometry
