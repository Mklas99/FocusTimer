# Notifications Specification

## Purpose
Defines FocusTimer's in-app notification mechanism used by break reminders and idle detection.

## Requirements

### Requirement: In-App Toast-Style Notifications
The system SHALL show in-app toast-style notifications (not native OS (Windows) toasts) for break reminders and idle pause/resume messages.

#### Scenario: Break reminder or idle event occurs
- **WHEN** a break-reminder or idle pause/resume event fires
- **THEN** an in-app toast-style notification is displayed to the user
