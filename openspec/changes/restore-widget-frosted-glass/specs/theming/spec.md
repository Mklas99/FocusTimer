# Spec Delta

## MODIFIED Requirements

### Requirement: Built-In Themes
The system SHALL provide 7 built-in themes (Dark, Light, Monokai, Solarized Dark, Nord, Dracula, High Contrast), selectable in Settings and applied live. Each built-in theme SHALL provide a default widget blur choice; High Contrast SHALL use an unblurred solid widget shell.

#### Scenario: User selects a built-in theme
- **WHEN** the user selects a theme from Settings → Appearance
- **THEN** the widget and settings UI re-theme immediately without a restart, and the widget applies that theme's blur and background opacity values

#### Scenario: User selects High Contrast
- **WHEN** the user selects the High Contrast theme
- **THEN** the widget uses a solid, unblurred shell and retains readable text and controls

#### Scenario: User restarts after editing a built-in theme
- **WHEN** the user applies a blur or background opacity edit while a built-in theme is selected and restarts the app
- **THEN** the saved edited values remain active instead of being replaced by that theme's factory defaults

### Requirement: Custom Theme Import/Export
The system SHALL let the user import and export custom themes as `.fttheme` JSON files via a file picker, SHALL include the widget blur choice and background opacity in exported themes, and SHALL validate supplied blur values on import. Theme files created before the blur choice existed SHALL remain importable using the compatible default blur choice.

#### Scenario: User imports a theme file
- **WHEN** the user selects a valid `.fttheme` file with a supported blur choice
- **THEN** the file is applied as the active theme with its blur and background opacity values

#### Scenario: User imports an invalid theme file
- **WHEN** the user selects a `.fttheme` file that fails validation
- **THEN** the import is rejected and the current theme is unchanged

#### Scenario: User imports an older theme file
- **WHEN** the user selects a valid `.fttheme` file without a blur choice
- **THEN** the theme imports with the compatible default blur choice and preserves its other values

#### Scenario: User imports an invalid blur choice
- **WHEN** the user selects a `.fttheme` file with an unsupported blur choice
- **THEN** the import is rejected and the current theme is unchanged

#### Scenario: User exports a theme
- **WHEN** the user exports the active theme
- **THEN** its blur choice and background opacity are present in the exported file and survive a later import

### Requirement: Theme Invariant Boundaries
The system SHALL ensure that built-in themes and imported custom themes modify only palette colors, material opacity values, and the widget blur choice, preserving layout geometry, spacing scales, corner radii, typography hierarchy, and control interaction states across all themes.

#### Scenario: User switches between themes
- **WHEN** the user switches between Dark, Monokai, Nord, Light, Dracula, Solarized Dark, or High Contrast themes
- **THEN** widget dimensions, control alignment, font sizes, corner radii, padding, and interaction states remain identical while appearance values update live

#### Scenario: Custom theme imported
- **WHEN** the user imports a valid custom `.fttheme` file
- **THEN** palette, opacity, and blur values apply without altering component structure, layout bounds, or interaction states
