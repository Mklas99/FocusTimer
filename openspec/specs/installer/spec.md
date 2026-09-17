# Installer Specification

## Purpose
Defines how FocusTimer is packaged and installed for end users.

## Requirements

### Requirement: MSI Package
The system SHALL be packaged as a Windows Installer (MSI) via the WiX `installer/FocusTimer.Installer` project, installing per-machine to `Program Files` and supporting upgrade/uninstall through the standard MSI upgrade code mechanism.

#### Scenario: A release build is packaged
- **WHEN** a release build is produced
- **THEN** the WiX installer project builds an MSI that installs FocusTimer to `Program Files`

#### Scenario: An older version is installed and a newer MSI is run
- **WHEN** the user runs a newer FocusTimer MSI while an older version is installed
- **THEN** the installer upgrades in place; running an older MSI over a newer install is blocked with an error

### Requirement: Start Menu Shortcut
The system SHALL install a Start Menu shortcut to launch FocusTimer, always included as part of the main install.

#### Scenario: User installs FocusTimer
- **WHEN** the MSI install completes
- **THEN** a "FocusTimer" Start Menu shortcut launching `FocusTimer.Host.exe` is present

### Requirement: Optional Desktop and Uninstall Shortcuts
The system SHALL offer, as selectable install features, a desktop shortcut and a Start Menu "Uninstall FocusTimer" shortcut.

#### Scenario: User keeps default feature selection
- **WHEN** the user completes install with default features selected
- **THEN** both the desktop shortcut and the Start Menu uninstall shortcut are installed

#### Scenario: User deselects a shortcut feature
- **WHEN** the user deselects the desktop shortcut or uninstall shortcut feature during install
- **THEN** that shortcut is not created

### Requirement: GitHub Release Publishing
The system SHALL publish a downloadable GitHub Release, containing the MSI and a portable (no-install) self-contained EXE, whenever a tag matching `v*.*.*` is pushed.

#### Scenario: A version tag is pushed
- **WHEN** a tag matching `v*.*.*` (e.g. `v0.1.0`) is pushed to the repository
- **THEN** `.github/workflows/release.yml` builds the installer via `scripts/build-installer.ps1` and creates a GitHub Release for that tag with the MSI and portable self-contained EXE attached
