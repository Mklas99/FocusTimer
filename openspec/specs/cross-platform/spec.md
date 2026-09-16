# Cross-Platform Specification

## Purpose
Defines FocusTimer's cross-platform intent and current per-platform implementation status.

## Requirements

### Requirement: Feature Parity Across Platforms
The system SHALL aim for the same feature set (active-window detection, notifications, global hotkeys, auto-start, and idle detection etc.) on every platform it supports. Windows is the primary target and is fully implemented first; other platforms are brought up to the same functionality incrementally, not scoped down permanently.

#### Scenario: Application runs on a platform with a completed implementation
- **WHEN** FocusTimer is launched on a platform whose platform-specific implementation is complete
- **THEN** active-window tracking, notifications, hotkeys, auto-start, and idle detection all function

## Platform Implementations

### Windows
Fully implemented: active-window detection, notifications, global hotkeys, auto-start, and idle detection all work via native Win32 integrations (see the respective capability specs).

### Linux
TBD. The app builds and runs on Linux and the timer widget itself functions, but active-window detection, notifications, global hotkeys, auto-start, and idle detection are currently no-op stubs in `FocusTimer.Core/Stubs`, pending platform-specific implementations (tracked as `OI-06`). This is a sequencing gap, not a scope decision — Linux is expected to reach the same functionality as Windows.
