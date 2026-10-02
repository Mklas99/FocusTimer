# Spec Delta

## MODIFIED Requirements

### Requirement: Built-In Themes
The system SHALL provide 7 built-in themes (Dark, Light, Monokai, Solarized Dark, Nord, Dracula, High Contrast), selectable in Settings and applied live. Each built-in theme SHALL provide a default widget backdrop choice; High Contrast SHALL use a solid widget shell.

#### Scenario: User selects a built-in theme
- **WHEN** the user selects a theme from Settings → Appearance
- **THEN** the widget and settings UI re-theme immediately without a restart, and the widget applies that theme's backdrop and background opacity values

#### Scenario: User selects High Contrast
- **WHEN** the user selects the High Contrast theme
- **THEN** the widget uses a solid, unblurred shell and retains readable text and controls

#### Scenario: User restarts after editing a built-in theme
- **WHEN** the user applies a backdrop or background opacity edit while a built-in theme is selected and restarts the app
- **THEN** the saved edited values remain active instead of being replaced by that theme's factory defaults

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

### Requirement: Theme Invariant Boundaries
The system SHALL ensure that built-in themes and imported custom themes modify only palette colors, material opacity values, and the widget backdrop choice, preserving layout geometry, spacing scales, corner radii, typography hierarchy, and control interaction states across all themes.

#### Scenario: User switches between themes
- **WHEN** the user switches between Dark, Monokai, Nord, Light, Dracula, Solarized Dark, or High Contrast themes
- **THEN** widget dimensions, control alignment, font sizes, corner radii, padding, and interaction states remain identical while appearance values update live

#### Scenario: Custom theme imported
- **WHEN** the user imports a valid custom `.fttheme` file
- **THEN** palette, opacity, and backdrop values apply without altering component structure, layout bounds, or interaction states
