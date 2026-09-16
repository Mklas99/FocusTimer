# Idle Detection Specification

## Purpose
Defines how FocusTimer detects OS-level user inactivity and auto-pauses/resumes the timer accordingly.

## Requirements

### Requirement: Auto-Pause on Inactivity
The system SHALL monitor Windows idle time via `GetLastInputInfo` and SHALL auto-pause the timer and notify the user once idle time reaches the 5-minute threshold while the timer is Running.

#### Scenario: User goes idle while timer runs
- **WHEN** OS idle time reaches 5 minutes while the timer is Running
- **THEN** the timer auto-pauses and an idle notification is shown

### Requirement: Resume Notification
The system SHALL show a "press play to resume" notification when the user returns from an idle-triggered pause.

#### Scenario: User returns from idle
- **WHEN** user input resumes after an idle-triggered auto-pause
- **THEN** a notification prompts the user to press play to resume
