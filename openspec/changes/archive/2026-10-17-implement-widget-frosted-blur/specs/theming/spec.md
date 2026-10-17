# Theming delta

## ADDED Requirements

### Requirement: Frosted theme compatibility
The system SHALL recognize Frosted as an additive widget backdrop value, preserving it and tint through cloning, settings save/load, and theme import/export. Missing material SHALL remain Off and legacy Soft, Strong, and Blur SHALL continue to normalize to Off. Selecting fallback SHALL NOT rewrite the stored Frosted request or tint. Factory defaults SHALL remain unchanged.

#### Scenario: Existing theme compatibility
- **WHEN** a theme without a backdrop value or with a legacy Soft, Strong, or Blur value is loaded or imported
- **THEN** Off is selected and all other valid palette, opacity and metadata values are retained

#### Scenario: Frosted snapshot round trip
- **WHEN** a built-in edit or imported theme with Frosted is cloned, applied, exported, reimported and restarted
- **THEN** Frosted and tint remain stored, custom preset identity is preserved where applicable, and platform support determines only the effective rendering

#### Scenario: High Contrast retains requested material
- **WHEN** a High Contrast theme containing Frosted is used
- **THEN** the widget renders a solid unblurred shell while the stored backdrop and tint values remain unchanged

#### Scenario: Invalid imported backdrop
- **WHEN** a theme supplies a backdrop token outside the supported current or legacy names
- **THEN** import is rejected and the current theme is unchanged