# Acceptance Trace

This trace confirms that every proposal outcome is represented by a specification scenario and implementation task.

| Proposal outcome | Specification scenario(s) | Implementation task(s) |
|---|---|---|
| Versioned durable records and provenance | worklog-storage: Entry round-trips through storage | 1.1–1.3, 3.2–3.3 |
| Running-session semantics | activity-tracking: User resumes after pausing; Window changes during a running interval | 2.2–2.4 |
| Local-day segmentation | activity-tracking: Timer remains running across midnight; worklog-storage: Cross-day interval is appended | 2.5, 4.1 |
| Storage-neutral worklog API | worklog-storage: Explicit Worklog Query; Read Outcomes and Diagnostics | 1.4, 4.4–4.6 |
| Safe RFC 4180 current-schema CSV | worklog-storage: Window title contains CSV control characters; Existing file uses an unsupported schema | 3.1–3.5 |
| Idempotent persistence | activity-tracking: A completed entry is submitted more than once; worklog-storage: Identical append is retried | 2.6, 4.2–4.3 |
| Stale-poll protection and in-process retry | activity-tracking: Timer stops while a foreground lookup is pending; A worklog append temporarily fails | 8.1–8.2 |
| Revision-checked same-day mutation | worklog-storage: Matching revision is updated; Stale revision is updated; Existing entry is deleted | 5.1–5.6 |
| Application integration and retention | activity-tracking: Entry is flushed on pause | 6.1–6.5 |
| No migration implementation | worklog-storage: Existing file uses an unsupported schema | 3.5, 7.2, 7.4 |
| CSV performance baseline without SQLite | No new behavioral requirement; design risk “Per-file ID checks add append I/O” | 7.1 |

The proposal's excluded migration, backup, installer, and rollback behavior remains excluded. OI-20 owns future
release-grade migration strategy; no migration implementation is present in this change.
