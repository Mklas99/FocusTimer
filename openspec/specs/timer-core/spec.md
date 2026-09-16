# Timer Core Specification

## Purpose
Defines the core start/pause/reset timer behavior and manual project/task tagging that all other capabilities (activity tracking, break reminders, idle detection) build on.

## Requirements

### Requirement: Start, Pause, and Reset
The system SHALL let the user start, pause, and reset the timer from the widget, and SHALL track timer state as one of Idle, Running, or Paused.

#### Scenario: User starts the timer
- **WHEN** the user clicks play while the timer is Idle or Paused
- **THEN** the timer state becomes Running and elapsed time counts up in HH:MM:SS

#### Scenario: User pauses the timer
- **WHEN** the user clicks pause while the timer is Running
- **THEN** the timer state becomes Paused and elapsed time stops counting

#### Scenario: User resets the timer
- **WHEN** the user clicks reset
- **THEN** elapsed time returns to zero and the timer state becomes Idle

### Requirement: Project/Task Tag
The system SHALL let the user attach a free-text project/task tag to the current timer session before or while it is running, and SHALL attach that tag to entries logged during the session.

#### Scenario: User tags a running session
- **WHEN** the user types a tag into the project/task field while the timer is Running
- **THEN** subsequently logged entries for that session carry the tag
