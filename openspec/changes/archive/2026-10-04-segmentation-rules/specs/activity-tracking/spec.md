# Spec Delta

## MODIFIED Requirements

### Requirement: Per-Application Time Segmentation
While the timer is Running and work logging is enabled, the system SHALL sample the foreground application/window at the configured activity polling interval, defaulting to ten seconds. When a sample detects a changed application or window title, the system SHALL close the current segment with an application-change reason and start a replacement segment with a distinct entry ID and the same running-session ID at the observation time. A window title change SHALL NOT close the segment when a segmentation rule applies (see the rule requirement below). Intervening time SHALL remain attributed to the last observed window; the system SHALL NOT reconstruct visits that occur entirely between samples.

#### Scenario: User switches active application
- **WHEN** a scheduled foreground sample detects a changed application while the timer is Running
- **THEN** the current segment closes with an application-change reason and a new segment starts for the observed application at the observation time with a distinct entry ID and the same session ID

#### Scenario: Window title changes within one application
- **WHEN** a scheduled sample detects a different window title within the same application
- **THEN** a new segment starts with that title even if the application name has not changed, unless a segmentation rule applies to both windows

#### Scenario: Existing configuration adopts the new default
- **WHEN** the saved configuration has no activity polling interval
- **THEN** scheduled foreground samples use the default ten-second interval

#### Scenario: Short visit falls between samples
- **WHEN** an application becomes foreground and loses foreground status entirely between two samples
- **THEN** no segment is invented for that unobserved visit and time remains attributed to the last observed window


## ADDED Requirements

### Requirement: Segmentation rules ignore title changes
The system SHALL NOT start a new segment when only the window title changes, provided the previous and the newly observed
window have the same process name and both match a segmentation rule. The segment SHALL keep the title observed when it
started. A change of process name, or a title change in a window that matches no segmentation rule, SHALL start a new
segment as usual. With no segmentation rules, segmentation SHALL behave as it did before the rules existed.

#### Scenario: Browser tab changes
- **WHEN** a rule matches the browser application and a sample observes a different title in the same browser
- **THEN** the open segment continues and keeps its original title

#### Scenario: Application changes
- **WHEN** a sample observes a different process name, even if both windows match segmentation rules
- **THEN** the open segment closes and a new one starts

#### Scenario: Title change in an unmatched application
- **WHEN** a title changes in an application no segmentation rule matches
- **THEN** a new segment starts with the new title

#### Scenario: Title-only rule
- **WHEN** a rule has only a title pattern and the title changes from one matching title to another within the same process
- **THEN** the open segment continues

#### Scenario: Excluded window
- **WHEN** a window matches both an exclusion rule and a segmentation rule
- **THEN** it is excluded and no segment is formed

### Requirement: Segmentation rule changes apply promptly and keep segments
A changed segmentation list SHALL take effect at the next foreground sample, using the same prompt-sample behavior as
exclusion changes. Applying it SHALL NOT close or merge existing segments. Applying an identical list SHALL have no effect.

#### Scenario: Rule added mid-session
- **WHEN** the user applies a rule matching the current application and the title then changes
- **THEN** no new segment starts for that title change, and segments written before the rule are unchanged
