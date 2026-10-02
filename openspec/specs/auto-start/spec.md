# Auto-Start Specification

## Purpose
Defines how FocusTimer commits Windows start-on-login registration and restores its prior state after commit failure or startup recovery.

## Requirements

### Requirement: Start-on-Login Toggle
The system SHALL provide a Settings toggle for start-on-login. The saved value SHALL be the draft's initial value. Editing it SHALL only change the draft; Apply or OK SHALL attempt to reconcile Windows registration with the saved candidate before reporting success. Failed reconciliation or restoration SHALL follow the Settings failure and recovery behavior.
Rollback and startup recovery SHALL restore the prior Windows Run command and value kind when a registration existed.

#### Scenario: User enables auto-start
- **WHEN** the user enables the auto-start toggle and Apply or OK succeeds
- **THEN** FocusTimer is registered to start automatically on the next login

#### Scenario: User disables auto-start
- **WHEN** the user disables the auto-start toggle and Apply or OK succeeds
- **THEN** FocusTimer's automatic-start registration is removed

#### Scenario: User cancels auto-start edit
- **WHEN** the user changes the toggle and cancels before applying it
- **THEN** the saved setting and start-on-login registration remain as they were

#### Scenario: Saved value and registration differ
- **WHEN** Settings opens and the saved start-on-login value differs from Windows registration
- **THEN** the toggle shows the saved value, a warning explains the mismatch and that Apply or OK will reconcile it, and Cancel leaves registration unchanged

#### Scenario: Auto-start registration fails
- **WHEN** changing start-on-login registration fails during Apply or OK
- **THEN** Settings reports the failure, does not report a successful commit, and attempts to restore the previous saved setting and registration

## Platform Implementations

### Windows
Enabling the toggle writes an `HKCU\...\Run` registry entry for FocusTimer; disabling it removes that entry.

### Linux
TBD — not yet implemented (tracked as `OI-06`; see the Cross-Platform spec's stub note).
