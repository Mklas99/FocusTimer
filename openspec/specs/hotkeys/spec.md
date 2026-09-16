# Global Hotkeys Specification

## Purpose
Defines system-wide keyboard shortcuts for controlling FocusTimer without focusing the app.

## Requirements

### Requirement: System-Wide Show/Hide and Toggle Hotkeys
The system SHALL register global hotkeys (default Ctrl+Alt+T to show/hide the widget, Ctrl+Alt+P to toggle the timer) via `RegisterHotKey` and a subclassed `WndProc`, and these SHALL work even when FocusTimer is unfocused.

#### Scenario: User presses show/hide hotkey while another app is focused
- **WHEN** the user presses the configured show/hide hotkey while a different application has focus
- **THEN** the FocusTimer widget shows or hides

#### Scenario: User presses toggle hotkey while another app is focused
- **WHEN** the user presses the configured toggle hotkey while a different application has focus
- **THEN** the timer starts or pauses accordingly

### Requirement: Hotkey Display (Editing Partial)
The system SHALL display the current hotkey bindings in Settings → Hotkeys. Editing the bindings from that UI is not yet supported; the fields are read-only pending a capture UI (tracked as `OI-01`).

#### Scenario: User opens the Hotkeys settings tab
- **WHEN** the user opens Settings → Hotkeys
- **THEN** the current hotkey bindings are shown as read-only fields
