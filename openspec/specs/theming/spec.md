# Theming Specification

## Purpose
Defines built-in and custom theme support for the widget and settings UI.

## Requirements

### Requirement: Built-In Themes
The system SHALL provide 7 built-in themes (Dark, Light, Monokai, Solarized Dark, Nord, Dracula, High Contrast), selectable in Settings and applied live.

#### Scenario: User selects a built-in theme
- **WHEN** the user selects a theme from Settings → Appearance
- **THEN** the widget and settings UI re-theme immediately without a restart

### Requirement: Custom Theme Import/Export
The system SHALL let the user import and export custom themes as `.fttheme` JSON files via a file picker, and SHALL validate the file's contents on import.

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
