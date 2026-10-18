# AGENTS.md

Guidance for coding agents working in this repo.

## Documentation map
- `README.md` — project overview. `docs/ARCHITECTURE.md`, `docs/DEVELOPMENT.md`, `docs/QUALITY_IMPROVEMENTS.md` — architecture, dev guide, quality history. `docs/OpenTasks.md` — manual checks that archived changes left open. `docs/design/` — UI design standard and audits.
- `VERSION` — the only place the version is written. `Directory.Build.props`, `scripts/build-installer.ps1` and the release workflow read it.
- `docs/versions/current/` — live planning baseline for the in-progress version: `Features.md` groups larger feature and improvement candidates, while `OpenIssues.md` is the detailed backlog and source of truth for individual `OI-##` items. `docs/versions/<version>/` (e.g. `v0.1/`) — archived snapshots from past versions.
- `docs/archived/` — historical planning docs, superseded, kept for reference only.
- `openspec/` — spec-driven source of truth (`specs/`, `changes/`, `config.yaml`).

## Keep docs in sync
`README.md`, `docs/`, and `openspec/` describe the same system from different angles. They must not contradict each other. When a change touches one of them, check whether the others need updating too, and fix any contradiction you notice - even if it's unrelated to your current task.
