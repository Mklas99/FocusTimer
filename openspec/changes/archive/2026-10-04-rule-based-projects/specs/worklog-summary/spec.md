# Spec Delta

## ADDED Requirements

### Requirement: Summaries use the resolved project
Summaries, project filters, the by-project grouping, the Timeline, and the Entries table SHALL use the resolved project
defined by `project-rules`, not only the stored project value, and SHALL treat project names that differ only by
letter case or surrounding spaces as one project.

#### Scenario: Grouping by project with rules
- **WHEN** a day is summarized by project and some untagged entries match a rule
- **THEN** those entries are counted under the rule's project and the total is unchanged

#### Scenario: Filtering by a rule-assigned project
- **WHEN** the user filters by a project that only exists through a rule
- **THEN** the matching entries are included

#### Scenario: Entries and Timeline agree with the summary
- **WHEN** an entry is resolved to a project by a rule
- **THEN** the Entries table and Timeline show the same project as the summary
