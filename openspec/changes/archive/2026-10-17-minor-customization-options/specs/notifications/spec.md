# Spec Delta

## MODIFIED Requirements

### Requirement: In-App Toast-Style Notifications
The system SHALL show in-app toast-style notifications for enabled break reminders and enabled idle pause/resume messages. General information and warning visibility SHALL follow committed notification preferences; errors SHALL remain enabled. Disabling messages SHALL NOT change timer, tracking, or diagnostic behavior. Break reminders SHALL follow their dedicated enable setting.

#### Scenario: Break reminder or idle event occurs
- **WHEN** a break-reminder or idle pause/resume event fires and its notification is enabled
- **THEN** an in-app toast-style notification is displayed to the user

#### Scenario: General notification disabled
- **WHEN** an information or warning notification is requested with its severity disabled
- **THEN** no toast opens, while timer/tracking transitions and diagnostic logging continue

### Requirement: Severity and dismissal
Notifications SHALL carry explicit information, warning, or error severity and use committed dismissal durations, defaulting to 5/15/15 seconds. Acknowledgement reminders SHALL remain open until dismissed. Warning and error headings and outlines SHALL use live Warning and Danger theme colors with explicit severity labels. Worklog persistence failures SHALL use error severity and cannot be disabled. Open notifications SHALL retain their initial timeout.

#### Scenario: Worklog save fails
- **WHEN** queued worklog entries cannot be saved
- **THEN** an error notification uses Danger and the configured error duration unless dismissed

#### Scenario: Warning notification appears
- **WHEN** a caller requests enabled warning severity
- **THEN** a warning notification uses Warning and the configured warning duration unless dismissed

#### Scenario: Acknowledgement reminder
- **WHEN** a break reminder requires acknowledgement
- **THEN** it stays open until dismissed regardless of the information visibility flag or duration

#### Scenario: Policy changes with a toast open
- **WHEN** dismissal preferences change while a toast is visible
- **THEN** that toast retains its initial timeout and later toasts use the new preference
