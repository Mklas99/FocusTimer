# AGENTS.md

Guidance for coding agents working in this repo.

## Documentation map
- `README.md` — project overview. `docs/ARCHITECTURE.md`, `docs/DEVELOPMENT.md`, `docs/QUALITY_IMPROVEMENTS.md` — architecture, dev guide, quality history. `docs/OpenTasks.md` — manual checks that archived changes left open. `docs/design/` — UI design standard and audits.
- `VERSION` — the only place the version is written. `Directory.Build.props`, `scripts/build-installer.ps1` and the release workflow read it.
- `docs/versions/current/` — live planning baseline for the in-progress version, four files only: `Features.md` groups candidate outcomes, `OpenIssues.md` is the backlog of unfinished `OI-##` items and the source of truth for them, `Planning.md` holds the next steps, `Summary.md` recaps what was built. `docs/versions/<version>/` (e.g. `v0.2/`) — snapshot of a finished version: the same files plus its past planning and an `evidence/` folder for its verification notes.
- `docs/performance/` — measurement baselines and how to reproduce them. `docs/design/` — design standard, token mapping, theme palette reference and audits. These are reference docs, not tied to a version.
- `docs/archived/` — historical planning docs, superseded, kept for reference only.
- `openspec/` — spec-driven source of truth (`specs/`, `changes/`, `config.yaml`).

## Keep docs in sync
`README.md`, `docs/`, and `openspec/` describe the same system from different angles. They must not contradict each other. When a change touches one of them, check whether the others need updating too, and fix any contradiction you notice - even if it's unrelated to your current task.
