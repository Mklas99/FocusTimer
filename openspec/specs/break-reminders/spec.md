# Break Reminders Specification

## Purpose
Defines interval-based break notifications and how the user can require explicit acknowledgement of them.

## Requirements

### Requirement: Interval-Based Reminder
The system SHALL fire a break-reminder notification after `BreakIntervalMinutes` (default 50) of cumulative running time.

#### Scenario: Break interval elapses
- **WHEN** the timer has been Running for `BreakIntervalMinutes` since the last reminder
- **THEN** a break-reminder notification is shown

### Requirement: Optional Acknowledgement Requirement
The system SHALL provide a setting that, when enabled, keeps the break reminder visible until the user acknowledges it instead of auto-dismissing and re-firing after 10 minutes.

#### Scenario: Acknowledgement required and reminder shown
- **WHEN** the acknowledgement setting is enabled and a break reminder fires
- **THEN** the reminder stays visible until the user explicitly dismisses/acknowledges it

#### Scenario: Acknowledgement not required and reminder shown
- **WHEN** the acknowledgement setting is disabled and a break reminder fires
- **THEN** the reminder auto-dismisses and re-fires after 10 minutes if not acted on
