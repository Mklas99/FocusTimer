# Worklog CSV Performance Baseline

The current worklog backend remains CSV; this baseline is deliberately not a SQLite comparison. It establishes a
repeatable correctness-and-throughput exercise before a later storage decision (OI-09).

## Scenario

`CsvWorklogStoreTests.WorklogPerformanceBaseline_LargeDailyAppendAndMultiDayQuery_ReturnsEveryEntry` appends
3,500 valid current-schema entries in one batch across seven daily partitions (500 entries per day), then queries
the same seven-day half-open interval. The test requires a successful append, a successful query, and exactly
3,500 returned records. It measures append and query elapsed time to ensure both operations are exercised, but
intentionally has no machine-dependent timing threshold.

## Recorded reference run

On 2026-09-17, the Debug test run on the development machine completed the single large-day scenario in **254 ms**
(one passing test). This is a reference point only; compare future runs using the same command and environment.

`CsvWorklogStoreTests.QueryAsync_GivenExternalReplacementWithPreservedMetadata_InvalidatesItsContentValidatedCache` warms a
daily-file cache, replaces that CSV externally, and verifies a subsequent query returns the replacement record.
The cache fingerprint combines file length, UTC last-write time, and a SHA-256 content hash; a changed file is
re-read rather than served stale even if an external writer preserves the file's length and timestamp. Store writes
refresh the cache and retention deletion removes its entry.

## Re-running

```powershell
dotnet test tests/FocusTimer.Persistence.Tests --filter FullyQualifiedName~WorklogPerformanceBaseline
dotnet test tests/FocusTimer.Persistence.Tests --filter FullyQualifiedName~InvalidatesItsContentValidatedCache
```

Record the host CPU, storage type, .NET SDK version, configuration, elapsed times, and commit when comparing a
future change. Do not infer a user-facing performance promise from a developer-machine run.
