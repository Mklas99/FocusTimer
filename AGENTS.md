# AGENTS.md

Guidance for coding agents working in this repo.

## Documentation map
- Root `*.md` (README.md, ARCHITECTURE.md, DEVELOPMENT.md, QUALITY_IMPROVEMENTS.md) — project overview, architecture, dev guide, quality history.
- `docs/versions/current/` — live baseline for the in-progress version: `Features.md` (what's implemented), `OpenIssues.md` (known gaps, `OI-##` ids). `docs/versions/<version>/` (e.g. `v0.1/`) — archived snapshots from past versions.
- `docs/archived/` — historical planning docs, superseded, kept for reference only.
- `openspec/` — spec-driven source of truth (`specs/`, `changes/`, `config.yaml`).

## Keep docs in sync
Root `*.md`, `docs/`, and `openspec/` describe the same system from different angles. They must not contradict each other. When a change touches one of them, check whether the others need updating too, and fix any contradiction you notice - even if it's unrelated to your current task.
