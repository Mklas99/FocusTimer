# Dev/Build Infrastructure Specification

## Purpose
Defines the quality gates and test infrastructure that back FocusTimer's development process (distinct from end-user-facing capabilities). Packaging/installation is its own capability — see the Installer spec.

## Requirements

### Requirement: CI and Static Analysis
The system SHALL be built and analyzed via a GitHub Actions sonar-scan workflow with SonarQube (`.sonarqube/`) and StyleCop (`stylecop.json`) rules enforced, with architecture documented in `ARCHITECTURE.md`.

#### Scenario: Code is pushed to the repository
- **WHEN** a change is pushed or a pull request is opened
- **THEN** the sonar-scan workflow runs SonarQube analysis and StyleCop rule enforcement

### Requirement: Automated Test Coverage
The system SHALL maintain automated test projects for Core, App, Persistence, Platform.Windows, and Host, covering timer logic, session tracking, the event bus, break reminders, theming, and CSV/JSON persistence.

#### Scenario: A pull request changes application logic
- **WHEN** a pull request modifies timer, session, event bus, break-reminder, theming, or persistence code
- **THEN** the corresponding test project(s) run and must pass before merge
