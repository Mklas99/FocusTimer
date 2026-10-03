# Design

## Context

`SessionTracker.HasWindowChanged` returns true when the process name or window title differs. `app-exclusions` added
`WindowMatchRule`, `WindowRuleMatcher`, an ordered rule list in `Settings`, a tolerant list converter, an editor view
model row (`ExclusionRuleItemViewModel`), and a "prompt sample on rule change" flag in the tracker. This change reuses
all of it.

## Goals / Non-Goals

**Goals:** fewer, longer segments for title-heavy applications without losing application changes; one rule shape and
one editor pattern across exclusion, segmentation, and project rules.

**Non-Goals:** minimum-duration merging; changing existing entries; per-application title policies other than "ignore".

## Decisions

### 1. The rule means "do not split on title"

`HasWindowChanged(previous, current)` stays true for a process-name change. For equal process names it is false when
both windows match a segmentation rule, true otherwise. Requiring both windows to match keeps title-only rules
predictable: leaving a matching title for a non-matching one still splits.

### 2. The segment keeps its first title

The entry carries the title seen when it started. Updating it to the latest title would need a mutable open segment title
and still misdescribe most of the time. Users who want the real title mix should leave the application unlisted.

### 3. Exclusion wins

The existing capture path evaluates exclusion first; segmentation only runs for non-excluded windows. No new ordering
state is needed.

### 4. Reuse the editor

Extract the exclusion list editing (rows, add, remove, reorder, validation, draft reset) into a reusable list view
model so Developer Options can host two lists, instead of duplicating about 100 lines of view-model code. Persist as a
second `Settings` list with the same tolerant converter, and push both lists to the tracker in the same call path.

### 5. Prompt sample on rule change

Reuse the exclusion behavior: a changed list marks one sample due at the next tick so the new rule applies without waiting a
full interval.

## Risks / Trade-offs

- A kept first title can make an entry look wrong for a long browsing stretch. Documented in the editor help text.
- Two similar lists in Developer Options can confuse users; mitigated by separate headings and help text.

## Open Questions

- Is title-ignoring enough, or is minimum-duration merging needed too? Decide after trying this on Windows.
