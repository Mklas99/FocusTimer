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

### Requirement: Rounded notification dialog presentation
Notifications SHALL use the shared dialog language with a rounded near-solid card, a semibold title, muted body text, a compact borderless close control, and the primary action style for acknowledgement. Body text SHALL reach 4.5:1 contrast with the card in built-in and imported themes. High Contrast SHALL retain a card outline with at least 3:1 contrast. Warning and error severity labels and readable heading/outline colors SHALL remain explicit.

#### Scenario: Notification appears
- **WHEN** a notification is displayed
- **THEN** it renders the shared rounded card and readable muted body with theme-aware colors
- **AND** the close control provides at least a 24 by 24 logical-pixel target and visible keyboard focus

#### Scenario: Reminder requires acknowledgement
- **WHEN** a reminder requires acknowledgement
- **THEN** its OK button uses the primary action style and the reminder remains open until dismissed
- **AND** existing activation and dismissal behavior is preserved

#### Scenario: Notification contains a long message
- **WHEN** the body exceeds the available card height
- **THEN** the message scrolls vertically while the dismissal action remains reachable
