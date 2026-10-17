# Design

## Context

Settings uses independent snapshots and a coordinated commit. Appearance previews already reach the widget. Notifications currently hardcode 5/15-second dismissal; the Host injects one App-owned notification service. The version-label handler currently unlocks once after seven clicks. See proposal.md for scope.

## Goals / Non-Goals

Reuse the draft and activation paths without separate saves, native services, or new dependencies. Notification suppression must not suppress diagnostics or timer/tracking transitions. OI-34's broader error policy and OI-43's diagnostic actions are separate work.

## Decisions

1. Store `WidgetButtonsBelowClock` as a false-by-default boolean, exposed as a labelled Appearance checkbox. Bind grid row/column and compact button orientation to the appearance snapshot. Keep the full controls in a horizontal row; compact controls retain their existing vertical stack beside the clock and become horizontal below it. Keep project input below both.
2. Store information/warning enabled flags and three severity durations, defaulting to 5/15/15 seconds. Accept whole seconds from 1 to 120. An App-owned notification policy takes a committed snapshot during startup/activation. Existing windows retain their initial timeout. Break reminders bypass general suppression and use the information timeout unless acknowledgement is required. Errors cannot be disabled.
3. Toggle the draft Developer flag on each group of seven clicks. Guard the gesture during loading, commit, recovery, and closure with `CanEdit`; reset partial click counts when the draft is replaced. Hiding Developer controls preserves their configured values.
4. Defaults preserve old JSON behavior; cloning carries every preference. Validate duration inputs in the settings editor and bound policy values defensively. Add persistence round-trip/default coverage and UI layout/commit regressions.

## Risks / Trade-offs

- Smaller below-clock compact layout changes geometry. Verify both modes at 0.5, 1, and 3 scale with headless layout assertions; native compositor verification remains separate.
- Notification suppression could hide failures. Errors always display and diagnostic logging remains unchanged.
- Duration changes while a toast is open could make dismissal surprising. Only newly opened notifications use new values.

## Migration Plan

New JSON fields default to existing behavior. No schema migration or worklog edits. Reverting the build ignores the extra fields. Update affected specs and current backlog descriptions after verification.
