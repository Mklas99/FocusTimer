## ADDED Requirements

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
