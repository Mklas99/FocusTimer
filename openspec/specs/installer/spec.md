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
The system SHALL publish a downloadable GitHub Release containing self-contained and framework-dependent MSI variants, a framework-dependent setup EXE, and a portable (no-install) self-contained EXE whenever a tag matching `v*.*.*` is pushed. The setup EXE SHALL check for the x64 .NET 8 runtime and download and install it when missing. The direct framework-dependent MSI SHALL stop with a clear message when the runtime is missing.

#### Scenario: A version tag is pushed
- **WHEN** a tag matching `v*.*.*` (e.g. `v0.1.0`) is pushed to the repository
- **THEN** `.github/workflows/release.yml` builds both MSIs and the setup via `scripts/build-installer.ps1` and creates a GitHub Release for that tag with both MSIs, the setup, and the portable self-contained EXE attached

#### Scenario: The runtime is missing

- **WHEN** the user runs the framework-dependent setup without an x64 .NET 8 runtime installed
- **THEN** the setup downloads the verified Microsoft runtime installer, installs the runtime, and then installs FocusTimer

#### Scenario: The runtime is already installed

- **WHEN** the user runs the framework-dependent setup with an x64 .NET 8 runtime installed
- **THEN** the setup skips the runtime download and installs FocusTimer

#### Scenario: The direct MSI is used without the runtime

- **WHEN** the user runs the direct framework-dependent MSI without an x64 .NET 8 runtime installed
- **THEN** installation stops and directs the user to the setup EXE

#### Scenario: A user switches installer variants

- **WHEN** the user installs the other MSI variant at the same version
- **THEN** the newly selected variant replaces the installed variant
