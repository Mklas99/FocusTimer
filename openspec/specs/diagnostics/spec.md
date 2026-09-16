# Diagnostics Specification

## Purpose
Defines FocusTimer's application logging behavior for troubleshooting.

## Requirements

### Requirement: Serilog File and Console Logging
The system SHALL log via Serilog to rolling daily files (30-day retention) under the app's log directory and to the console, and SHALL honor a `FOCUSTIMER_LOG_DIR` environment variable override for the log directory.

#### Scenario: Application runs without env override
- **WHEN** `FOCUSTIMER_LOG_DIR` is not set
- **THEN** logs are written to the app's default log directory, rolling daily with 30-day retention

#### Scenario: Application runs with env override
- **WHEN** `FOCUSTIMER_LOG_DIR` is set
- **THEN** logs are written to the specified directory instead of the default

Note: an "Open logs folder" button in Settings is not yet implemented (tracked as `OI-05`); the log directory must currently be located manually.
