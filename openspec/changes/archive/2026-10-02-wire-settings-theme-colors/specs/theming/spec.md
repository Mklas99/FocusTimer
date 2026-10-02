# Spec Delta

## ADDED Requirements

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
