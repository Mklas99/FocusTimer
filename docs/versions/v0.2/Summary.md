# FocusTimer — v0.2.0 Development Summary

A comprehensive recap of the v0.2.0 development cycle, expanding FocusTimer from its initial baseline into a feature-rich, customizable time tracking application with dedicated worklog management, smart tracking, and a modern dense-frost design system.

- Companion files: [Features.md](Features.md) (what's implemented) and [OpenIssues.md](OpenIssues.md) (known gaps and remaining backlog).
- Planning of this cycle: [Planning.md](Planning.md). Verification and design evidence for finished items: [evidence/](evidence/).
- Previous version snapshot: [v0.1/](../v0.1/).
- Spec history: [openspec/changes/archive/](../../../openspec/changes/archive/).

## Development steps and milestones

1. **Worklog Data Foundation (F-01)** — Introduced durable entry and session identities, UTC modification timestamps, schema version 1 RFC 4180 CSV serialization, and revision-checked atomic mutations.
2. **UI Design System Foundation (M01)** — Established the three-tier token architecture (`Tokens.axaml`), authoritative desktop spacing, radii, and accessible hit targets.
3. **Reliability and Quality Tooling (M02)** — Isolated persistence tests from user AppData, switched CI to `windows-latest`, and established automated OpenCover coverage report ingestion into SonarQube Cloud.
4. **Installer and Packaging Optimization (OI-16, OI-40)** — Produced a multi-flavor distribution suite: self-contained MSI (36 MB), .NET 8 bootstrapper setup (13 MB), and standalone portable EXE (46 MB). Packaged `CHANGELOG.md` with installed assets and embedded it into application assemblies, branded the setup with the application icon, and suppressed the chained MSI internal UI to streamline installation into a single progress window.
5. **Activity Polling Optimization (OI-14, OI-17)** — Added configurable foreground polling (1–60s) in Developer Options with bounded Windows process-handle caching to minimize lookup overhead.
6. **Worklog Summary Breakdown (F-02)** — Introduced a host-independent summary service with flexible range, application/project/window grouping, and read warnings.
7. **Widget and Settings Design Adaptation (M03)** — Separated widget shell tint from window opacity, removed the widget minimize button for balanced vertical spacing, and implemented full draft commit/recovery transactions.
8. **Worklog Data Management Window (F-03)** — Created a dedicated tray-accessible window with Entries (raw records, inline edit/delete, search highlighting), Timeline (zoomable time axis with column grouping), and Summary tabs driven by a unified day selector. Added manual time entry logging with day bounds validation.
9. **Smarter Automatic Activity Tracking (F-04)** — Built a shared window rule matcher and three rule engines: application exclusions during capture, title-change segmentation suppression, and dynamic read-time project mapping without stored log rewrites.
10. **UI Appearance and Usability Revamp (F-05)** — Implemented the dense-frost design system with Segoe UI typography, compact peer cards, icon-and-underline navigation, unified desktop action buttons, and verified contrast harmony across all 7 built-in themes plus custom imported theme snapshot preservation.
11. **Minor Customization Options & Frosted Blur (F-06)** — Added widget button placement choices (beside or below clock), notification preferences (severity visibility, 1–120s timeouts), 7-click Developer mode unlock, and native Windows frosted blur acrylic backdrop with solid fallback.
12. **Repository Layout & Single-Source Versioning (M04)** — Consolidated scripts and documentation, established the root `VERSION` file as the authoritative version across build properties, installer, and release workflows, and normalized line endings.

## Outcome

By v0.2.0, FocusTimer delivers a complete, polished desktop time management experience: a compact or full widget with native Windows acrylic blur, rich worklog inspection, editing, and timeline visualization, intelligent tracking rules, and accessible, theme-harmonized dialogs. All CI quality gates, coverage requirements, and release packaging pipelines are automated. Remaining gaps and future candidates are tracked in [OpenIssues.md](OpenIssues.md).
