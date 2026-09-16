# Auto-Start Specification

## Purpose
Defines FocusTimer's start-on-login behavior.

## Requirements

### Requirement: Start-on-Login Toggle
The system SHALL provide a Settings toggle that controls whether FocusTimer starts automatically when the user logs in.

#### Scenario: User enables auto-start
- **WHEN** the user enables the auto-start toggle in Settings
- **THEN** FocusTimer is registered to start automatically on the next login

#### Scenario: User disables auto-start
- **WHEN** the user disables the auto-start toggle in Settings
- **THEN** FocusTimer's automatic-start registration is removed

## Platform Implementations

### Windows
Enabling the toggle writes an `HKCU\...\Run` registry entry for FocusTimer; disabling it removes that entry.

### Linux
TBD — not yet implemented (tracked as `OI-06`; see the Cross-Platform spec's stub note).
