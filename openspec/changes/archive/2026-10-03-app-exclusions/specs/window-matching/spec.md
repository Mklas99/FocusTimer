# Spec Delta

## Purpose

Defines how a rule identifies the foreground window it applies to, by application name and/or window title, so that
exclusion, segmentation, and project rules interpret patterns the same way.

## ADDED Requirements

### Requirement: Window rule shape
A window rule SHALL have an application pattern, a window-title pattern, or both. A rule with neither, or with only
blank patterns, SHALL be invalid. A rule with both patterns SHALL match a window only when both patterns match.

#### Scenario: Application-only rule
- **WHEN** a rule has only an application pattern and the window's application matches it
- **THEN** the rule matches regardless of the window title

#### Scenario: Application and title rule
- **WHEN** a rule has both patterns and only the application matches
- **THEN** the rule does not match

#### Scenario: Empty rule
- **WHEN** a rule has no non-blank pattern
- **THEN** it is invalid and never matches

### Requirement: Pattern semantics
Patterns SHALL be matched case-insensitively against the whole value, where `*` matches any run of characters and `?`
matches exactly one. All other characters SHALL match literally. An application pattern SHALL match the process name
with or without a trailing `.exe`, except that a pattern whose text before `.exe` is only wildcards (such as `*.exe`)
SHALL be matched literally.

#### Scenario: Wildcard match
- **WHEN** the title pattern is `*Inbox*` and the title is `Inbox (3) - Mail`
- **THEN** the pattern matches

#### Scenario: Case and extension
- **WHEN** the application pattern is `KeePass` and the process name is `keepass.exe`
- **THEN** the pattern matches

#### Scenario: Wildcard-only executable pattern
- **WHEN** the application pattern is `*.exe`
- **THEN** it matches only process names ending in `.exe`, not every process

#### Scenario: Literal characters
- **WHEN** a pattern contains characters such as `(`, `[`, or `.`
- **THEN** they are matched literally, not as regular-expression syntax

### Requirement: Ordered evaluation
When several rules are configured, the system SHALL evaluate them in list order and report the first matching rule.
Evaluation SHALL be deterministic for the same window and rule list.

#### Scenario: First match wins
- **WHEN** two rules match the same window
- **THEN** the earlier rule in the list is the one reported
