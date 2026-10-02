# Design

## Context

`SessionTracker.CaptureAsync` turns each foreground sample (`ActiveWindowInfo`: process name, window title) into
segments: `HasWindowChanged` closes the open segment and `CreateNewEntry` opens a replacement. Settings reach the
tracker through `TimerWidgetViewModel` on load and on Apply (`SetPollingInterval` is the existing example).
`IProjectResolver` (F-02) already isolates read-side project decisions. This change is capture-side.

## Goals / Non-Goals

**Goals:**
- Excluded activity is never written, buffered, or attributed to a neighbouring application.
- One matcher that `OI-14` and `OI-08` can reuse without a second pattern syntax.
- Rule edits follow existing Settings draft/commit semantics.

**Non-Goals:**
- Segmentation rules (`OI-14`) and project rules (`OI-08`); both are later changes.
- Deleting or redacting already-recorded entries.
- Excluding time from the running timer or break reminders.
- A new platform contract; Windows and Linux stubs both supply `ActiveWindowInfo`.

## Decisions

### 1. A rule matches application and/or title

`WindowMatchRule { AppPattern?, TitlePattern? }`, at least one non-empty. Both set means both must match. Case-insensitive
glob (`*`, `?`) compiled once per rule set; no regex, so user input cannot cause pathological backtracking. A bare
application pattern matches the process name with or without a trailing `.exe`. A single shape serves "app only"
and "app plus title" users, and `OI-14`/`OI-08` extend it rather than replace it.

### 2. Exclusion is a tracker state, not a filter on stored rows

The matcher runs in the capture path after the lookup, under the existing state lock and generation checks:

```
sample --> excluded?
   yes: close open segment at now (EndReason.ApplicationChange), set _excluded, open nothing
   no : if _excluded -> start a new segment at now, clear _excluded
        else          -> existing change detection
```

No segment is open while excluded, so nothing needs removing and midnight splitting has nothing to split. The next
visible window starts its own segment at its observation time, so excluded time is never attributed to it.
Alternative rejected: a gap-marker entry, which adds a new entry kind and pollutes summaries.

The lookup cadence is unchanged. Time between the last non-excluded sample and an excluded one stays attributed to
the earlier window, as with any application change (existing "no reconstruction between samples" rule). The
exclusion therefore starts at the first sample that observes it.

### 3. Lookup failure never records an excluded window

A failed lookup keeps the current behavior (the open segment continues, or `Unknown` at initial capture). If the
tracker is in the excluded state, a failure keeps it excluded.

### 4. Rules apply from the next sample after Apply

Rules are stored in `Settings` and pushed to the tracker by the same path as the polling interval. Applying a new
list takes effect on the next sample, does not close segments by itself, and re-evaluates the current window then.
Setting an identical list is a no-op. Draft edits do not reach the tracker; Cancel restores the last applied list.

### 5. Persistence is tolerant

The list uses a property-level tolerant converter, as the polling interval does: a malformed or empty rule is dropped
with a log warning and does not reset other settings.

### 6. Startup with an excluded window in front

`StartAsync` runs the matcher on the initial capture too. If the window is excluded, tracking starts in the excluded
state and no segment opens until a non-excluded window appears.

## Risks / Trade-offs

- Title patterns depend on titles at sample time; a title that changes between samples can leak up to one interval.
  Documented, not mitigated: shorter polling is the lever.
- Dropping a malformed rule silently weakens a privacy rule, so the Developer Options editor must validate on entry
  and the loader must log each dropped rule.
- Excluded time appears as a gap in the Timeline and as a difference between the timer and the worklog total.

## Open Questions

- Rule list size limit and import/export are deferred.
- Whether the editor offers "pick from running applications" is a UI choice for implementation.
