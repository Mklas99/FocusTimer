# Versioning Specification

## Purpose
Defines how FocusTimer surfaces version and release information to the user.

## Requirements

### Requirement: About Tab Version Info
The system SHALL show the assembly's informational version, author, repository link, and changelog (read from `docs/CHANGELOG.md`) in the Settings → About tab.

#### Scenario: User opens the About tab
- **WHEN** the user opens Settings → About
- **THEN** the current version, author, repository link, and changelog contents are displayed
