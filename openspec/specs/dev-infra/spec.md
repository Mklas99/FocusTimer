# Dev/Build Infrastructure Specification

## Purpose
Defines the quality gates and test infrastructure that back FocusTimer's development process (distinct from end-user-facing capabilities). Packaging/installation is its own capability — see the Installer spec.

## Requirements

### Requirement: CI and Static Analysis
The system SHALL be built and analyzed via a GitHub Actions sonar-scan workflow with SonarQube (`.sonarqube/`) and StyleCop (`stylecop.json`) rules enforced, with architecture documented in `ARCHITECTURE.md`.

#### Scenario: Code is pushed to the repository
- **WHEN** a change is pushed or a pull request is opened
- **THEN** the sonar-scan workflow runs SonarQube analysis and StyleCop rule enforcement

### Requirement: Automated Test Coverage and Ingestion
The system SHALL maintain automated test projects for Core, App, Persistence, Platform.Windows, and Host, covering timer logic, session tracking, the event bus, break reminders, theming, and CSV/JSON persistence. Persistence tests SHALL execute within isolated temporary environments without modifying the user's real `%APPDATA%` settings. The CI workflow and local coverage scripts SHALL generate OpenCover coverage reports ingested by SonarQube / SonarCloud analysis.

#### Scenario: A pull request changes application logic
- **WHEN** a pull request modifies timer, session, event bus, break-reminder, theming, or persistence code
- **THEN** the corresponding test project(s) run within isolated environments and must pass before merge

#### Scenario: CI executes test suite with coverage
- **WHEN** the sonar-scan workflow executes in CI with SonarCloud credentials
- **THEN** tests execute across all test projects and publish OpenCover coverage reports that are ingested into the Sonar analysis
