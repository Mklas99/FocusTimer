## MODIFIED Requirements

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
