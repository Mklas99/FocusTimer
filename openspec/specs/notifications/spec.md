# Notifications Specification

## Purpose
Defines FocusTimer's in-app notifications used by break reminders, idle detection, and worklog persistence failures.

## Requirements

### Requirement: In-App Toast-Style Notifications
The system SHALL show in-app toast-style notifications (not native OS (Windows) toasts) for break reminders and idle pause/resume messages.

#### Scenario: Break reminder or idle event occurs
- **WHEN** a break-reminder or idle pause/resume event fires
- **THEN** an in-app toast-style notification is displayed to the user

### Requirement: Severity and dismissal
Notifications SHALL carry explicit information, warning, or error severity. Information notifications SHALL dismiss after five seconds; warnings and errors SHALL dismiss after fifteen seconds. Reminders requiring acknowledgement SHALL remain open until dismissed. Warning and error headings and outlines SHALL use the live theme's readable Warning and Danger colors respectively, with an explicit severity label. Worklog persistence failures SHALL use error severity.

#### Scenario: Worklog save fails
- **WHEN** queued worklog entries cannot be saved
- **THEN** the notification identifies an error, uses the Danger theme color, and remains visible for fifteen seconds unless dismissed

#### Scenario: Warning notification appears
- **WHEN** a caller requests warning severity
- **THEN** the notification identifies a warning, uses the Warning theme color, and remains visible for fifteen seconds unless dismissed
