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
The system SHALL provide 7 built-in themes (Dark, Light, Monokai, Solarized Dark, Nord, Dracula, High Contrast), selectable in Settings and applied live. Each SHALL provide a default widget backdrop choice; High Contrast SHALL use a solid shell.

#### Scenario: User selects a built-in theme
- **WHEN** the user selects a theme from Settings → Appearance
- **THEN** the widget and settings UI re-theme immediately without a restart

#### Scenario: User restarts after editing a built-in theme
- **WHEN** the user applies an edited backdrop choice or tint opacity with a built-in theme selected and restarts
- **THEN** the saved values remain active instead of being replaced by factory defaults

### Requirement: Custom Theme Import/Export
The system SHALL let the user import and export custom themes as `.fttheme` JSON files via a file picker, including widget backdrop choice and background opacity, and SHALL validate the file's contents on import. Older files missing the choice SHALL use Off.

#### Scenario: User imports a theme file
- **WHEN** the user selects a `.fttheme` file to import
- **THEN** the file is validated and, if valid, applied as the active theme

#### Scenario: User imports an invalid theme file
- **WHEN** the user selects a `.fttheme` file that fails validation
- **THEN** the import is rejected and the current theme is unchanged

### Requirement: Reset to Default
The system SHALL let the user restore the Dark theme with a single action.

#### Scenario: User resets theme
- **WHEN** the user clicks "Reset to default" in Settings → Appearance
- **THEN** the Dark theme is applied

### Requirement: Theme Invariant Boundaries
The system SHALL ensure that built-in themes and imported custom themes modify only palette colors, material opacity values, and the widget backdrop choice, preserving layout geometry, spacing scales, corner radii, typography hierarchy, and control interaction states across all themes.

#### Scenario: User switches between themes
- **WHEN** the user switches between Dark, Monokai, Nord, Light, Dracula, Solarized Dark, or High Contrast themes
- **THEN** widget dimensions, control alignment, font sizes, corner radii, and padding remain identical while colors and brushes update live

#### Scenario: Custom theme imported
- **WHEN** the user imports a valid custom `.fttheme` file
- **THEN** palette and opacity values map to semantic UI resources without altering component structure, layout bounds, or interaction states
