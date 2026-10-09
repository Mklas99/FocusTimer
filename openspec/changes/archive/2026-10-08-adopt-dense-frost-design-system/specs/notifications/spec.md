## ADDED Requirements

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
