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
The system SHALL apply every existing Settings text, input, and tab color as the preferred source for its named visible role, updating live without changing layout or widget button colors. `TabSelectedBackground` SHALL supply the selected-tab underline color, adjusted for indicator contrast when necessary. The selected tab label and icon SHALL use the theme accent, adjusted toward white or black only as far as needed to reach a 4.5:1 contrast ratio on the Settings background (tabs have no fill of their own), because `TabSelectedText` is chosen for text drawn on the selected fill. `TabSelectedText` and `TabSelectedBackground` SHALL supply matched readable text and fill colors for selections such as the selected Worklog row, without changing stored values. Normal and hover tab backgrounds SHALL retain their respective roles. No new serialized theme field SHALL be required.

#### Scenario: Text roles update live
- **WHEN** a user selects or edits a theme while Settings is open
- **THEN** ordinary text, section headings, labels, and disabled text use their corresponding theme text colors across Settings tabs

#### Scenario: Input roles update live
- **WHEN** a user changes the input background, border, or text color
- **THEN** editable and read-only Settings fields use the matching color in their applicable normal or focused state

#### Scenario: Tab roles update live
- **WHEN** a user changes the tab background, hover background, text, or selected background color, or the accent
- **THEN** normal and hovered tabs use the matching background and text roles, the selected underline uses the selected background role, and the selected label and icon use the accent adjusted to stay readable on the Settings background

#### Scenario: Selected label stays readable in every theme
- **WHEN** any built-in or imported theme is applied
- **THEN** the selected tab label and icon reach a 4.5:1 contrast ratio on the Settings background without changing any stored theme value

#### Scenario: Selected text keeps its fill role
- **WHEN** a Worklog row is selected
- **THEN** its text and fill derive from `TabSelectedText` and `TabSelectedBackground`, adjusted together for rendered contrast

#### Scenario: Independent focus and selected colors
- **WHEN** a desktop field, button, or tab receives keyboard focus
- **THEN** its focus indicator derives from `AccentPrimary`, while the selected-tab underline derives from `TabSelectedBackground` even when that differs from the accent

#### Scenario: Existing theme round trip
- **WHEN** an existing built-in or imported theme is previewed, applied, exported, and reimported
- **THEN** its existing color, opacity, and widget backdrop values are preserved without adding or migrating fields for this refresh

### Requirement: Built-in Settings color readability
The system SHALL keep Settings text, field contents, selected tabs, and keyboard-focus indicators readable in every built-in theme, including Light and High Contrast.

#### Scenario: Built-in theme review
- **WHEN** each built-in theme is applied to Settings
- **THEN** enabled normal-size text has at least 4.5:1 contrast against its rendered surface, and selected-tab and keyboard-focus indicators have at least 3:1 contrast against adjacent colors

#### Scenario: Theme switch preserves interaction states
- **WHEN** a user switches between built-in themes with a field focused or a tab selected
- **THEN** the field remains visibly focused and the selected tab remains identifiable without changing control geometry

### Requirement: Built-in palette harmony
The system SHALL provide a coordinated palette for each built-in theme across the widget, Settings, Worklog, color pickers, and notifications. Backgrounds, text, accents, borders, selection, and status colors SHALL retain that theme's identity and a clear visual hierarchy while meeting the applicable readability requirements. Status meanings SHALL remain distinguishable without relying on color alone.

#### Scenario: Complete theme review
- **WHEN** a built-in theme is reviewed across the widget and desktop views
- **THEN** related backgrounds, text levels, accents, borders, and interaction states form a coherent palette
- **AND** success, warning, and error colors fit the theme while retaining their semantic distinctions
- **AND** the review records visual intent and representative views separately from numerical contrast results

### Requirement: Later personal palette tuning
The system SHALL preserve saved color edits to built-in and imported themes through Apply/OK, reopening, restart, and export/import. Future factory palette tuning SHALL NOT overwrite those saved edits. Authored palette choices SHALL remain separate from derived readability corrections so personal color preferences can be revised without changing layout or relaxing contrast requirements.

#### Scenario: Personal color preferences survive
- **WHEN** the user edits supported theme colors to personal taste, applies them, reopens Settings, and restarts
- **THEN** the saved source colors remain active and survive export/import
- **AND** desktop readability corrections continue to apply without replacing those stored preferences

#### Scenario: Factory palette changes later
- **WHEN** a later app build supplies harmonized or taste-adjusted factory colors for a preset with saved user edits
- **THEN** the saved palette remains active until the user explicitly selects a factory preset or resets
- **AND** choosing a factory preset or reset previews its current factory colors through the existing Settings draft model

### Requirement: Desktop palette readability across states
The system SHALL render enabled normal-size desktop text with at least 4.5:1 contrast and meaningful focus, selection, and control indicators with at least 3:1 contrast against adjacent rendered colors. This SHALL apply to Settings, Worklog, color pickers, notifications, and their popups under every built-in theme and valid imported palette. Contrast SHALL account for transparency and the actual background of each state.

#### Scenario: Built-in control state review
- **WHEN** each built-in theme is used through applicable normal, hover, pressed, focused, checked, selected, read-only, and invalid states
- **THEN** enabled text and meaningful indicators meet their contrast thresholds without changing geometry or interaction behavior
- **AND** disabled controls remain identifiable and noninteractive, without requiring the enabled-text threshold

#### Scenario: Transparent selected fill
- **WHEN** text or a Worklog row is selected using an imported translucent selection color
- **THEN** selected text is readable against the actual composited fill on that control's background
- **AND** the selection remains distinguishable from the unselected state

#### Scenario: Opposing custom backgrounds
- **WHEN** an imported palette uses light and dark backgrounds that cannot share a readable foreground
- **THEN** each applicable control state uses readable derived colors appropriate to its background
- **AND** the stored palette remains unchanged

#### Scenario: Notification severity
- **WHEN** an information, warning, error, or acknowledgement notification uses a built-in or imported palette
- **THEN** its title, body, available actions, and meaningful focus indicators remain readable
- **AND** warning and error severity remains explicit in the text

#### Scenario: Live palette switch
- **WHEN** the user changes the theme while a desktop field is focused, text is selected, or a popup is open
- **THEN** rendered colors update without losing the applicable focus or selection state or changing layout

### Requirement: Stored palette preservation during readability correction
The system SHALL preserve stored imported colors, opacity values, metadata, and backdrop choices while applying readability corrections only to derived desktop colors. Corrections SHALL keep each named color role as their preferred source and SHALL leave widget-specific color and opacity controls independent. Export SHALL contain the stored palette rather than corrected desktop colors.

#### Scenario: Import preview and export
- **WHEN** a low-contrast imported theme is previewed, applied, exported, and reimported
- **THEN** its stored appearance values and metadata survive unchanged while derived desktop colors meet the readability requirements

#### Scenario: Widget color edit
- **WHEN** the user edits a widget button or timer color
- **THEN** that widget role updates without recoloring desktop actions or changing desktop layout

### Requirement: Stable custom theme selection
The system SHALL show a Custom/Imported preset entry for an imported theme, including one named like a built-in preset. Apply/OK, reopening, and restart SHALL restore that selection and the saved theme values without rereading the import file or loading factory defaults. Built-in selection and reset SHALL explicitly switch preset identity. Edited built-in themes SHALL retain their selected preset and saved edits.

#### Scenario: Imported name matches a preset
- **WHEN** a theme named Dark with custom colors is imported, committed, and the app restarted
- **THEN** Custom/Imported is selected and the imported colors remain active

#### Scenario: Source file is unavailable
- **WHEN** a committed imported theme's source file is moved or deleted before Settings reopens or the app restarts
- **THEN** the saved theme and Custom/Imported selection remain available without requiring that file

#### Scenario: Legacy import identity
- **WHEN** saved settings identify an import by its original theme name and custom source path
- **THEN** reopening and restart restore the saved imported palette and show Custom/Imported
- **AND** merely opening Settings does not persist a migration or reload a preset

#### Scenario: Built-in transition
- **WHEN** the user selects a built-in preset or resets an imported theme to default
- **THEN** the selected built-in theme previews and import identity no longer overrides that selection

#### Scenario: Cancel after Apply
- **WHEN** an imported theme is applied, the user makes further appearance or preset edits, and cancels or closes Settings
- **THEN** the last successful commit's palette, custom selection, and appearance values are restored

#### Scenario: Cancel an uncommitted import
- **WHEN** the user imports a theme and cancels without successfully applying it
- **THEN** the prior committed palette and preset selection are restored

#### Scenario: Edited built-in survives restart
- **WHEN** a built-in preset's colors or opacity are edited, committed, and the app restarted
- **THEN** the saved edits remain active and its built-in preset remains selected
