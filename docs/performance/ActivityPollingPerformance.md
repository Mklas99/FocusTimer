# Activity polling performance

## Method and environment

Baseline revision: `aeca777ddc348d8d083b6c10c6ea990cf82b5558`. After measurements use the uncommitted configure-activity-polling implementation. Release; SDK 10.0.204; runtime 8.0.31; Windows NT 10.0.26200.0; machine MKLASOMNI; 8 logical CPUs. Measured 2026-09-27.

Each row processes 10,000 simulated one-second ticks after 100 warmup ticks. Three runs per workload use scripted foreground observations, real process-name queries, and isolated synthetic settings/worklogs. No personal titles or real AppData are read. The workload is accelerated; duration is execution time, not 10,000 seconds of real app use. CPU resolution can report zero for short runs. Allocations are managed bytes; handle counts bracket each workload after service disposal.

## baseline (interval 1 seconds)

| Scenario | Run | Duration ms | CPU seconds | Allocated bytes | Foreground queries | Name queries | Name lookup ms | Handles before/after |
|---|---|---|---|---|---|---|---|---|
| stopped | 1 | 7.8219 | 0 | 320120 | 0 | 0 | 0 | 349/349 |
| stopped | 2 | 1.7536 | 0.015625 | 320120 | 0 | 0 | 0 | 349/349 |
| stopped | 3 | 2.6487 | 0 | 320120 | 0 | 0 | 0 | 349/349 |
| disabled | 1 | 2.886 | 0 | 320120 | 0 | 0 | 0 | 349/349 |
| disabled | 2 | 1.7928 | 0.015625 | 320120 | 0 | 0 | 0 | 349/349 |
| disabled | 3 | 1.7434 | 0 | 320120 | 0 | 0 | 0 | 349/349 |
| stable | 1 | 1377.9039 | 1.140625 | 9528264 | 10001 | 10001 | 1285.9002 | 349/355 |
| stable | 2 | 765.0657 | 0.828125 | 8802048 | 10001 | 10001 | 735.9342 | 355/355 |
| stable | 3 | 558.0369 | 0.53125 | 8802048 | 10001 | 10001 | 546.7074 | 355/355 |
| titles | 1 | 560.4695 | 0.484375 | 12767912 | 10001 | 10001 | 531.2104 | 355/355 |
| titles | 2 | 503.2147 | 0.421875 | 12714920 | 10001 | 10001 | 480.8544 | 355/355 |
| titles | 3 | 729.8556 | 0.515625 | 12714920 | 10001 | 10001 | 692.5867 | 355/355 |
| switches | 1 | 30481.5963 | 27.421875 | 151442344 | 10001 | 10001 | 30364.7406 | 355/353 |
| switches | 2 | 33709.4506 | 27.734375 | 151475600 | 10001 | 10001 | 33583.1266 | 356/353 |
| switches | 3 | 28242.8637 | 25.15625 | 151518096 | 10001 | 10001 | 28135.8579 | 356/353 |

## after-1 (interval 1 seconds)

| Scenario | Run | Duration ms | CPU seconds | Allocated bytes | Foreground queries | Name queries | Name lookup ms | Handles before/after |
|---|---|---|---|---|---|---|---|---|
| stopped | 1 | 39.6305 | 0 | 320120 | 0 | 0 | 0 | 347/347 |
| stopped | 2 | 0.6538 | 0 | 320120 | 0 | 0 | 0 | 347/347 |
| stopped | 3 | 0.6285 | 0 | 320120 | 0 | 0 | 0 | 347/347 |
| disabled | 1 | 1.3929 | 0 | 320120 | 0 | 0 | 0 | 347/347 |
| disabled | 2 | 0.6509 | 0 | 320120 | 0 | 0 | 0 | 347/347 |
| disabled | 3 | 0.8286 | 0 | 320120 | 0 | 0 | 0 | 347/347 |
| stable | 1 | 5.8954 | 0 | 1361816 | 10001 | 1 | 0.1123 | 347/347 |
| stable | 2 | 5.5616 | 0 | 1361816 | 10001 | 1 | 0.1122 | 347/347 |
| stable | 3 | 5.4181 | 0.015625 | 1361816 | 10001 | 1 | 0.2226 | 347/347 |
| titles | 1 | 13.0414 | 0.015625 | 5274272 | 10001 | 1 | 0.0924 | 347/347 |
| titles | 2 | 13.1674 | 0.03125 | 5291744 | 10001 | 1 | 0.0927 | 347/347 |
| titles | 3 | 11.47 | 0 | 5514296 | 10001 | 1 | 0.103 | 347/347 |
| switches | 1 | 21129.2826 | 20.0625 | 152665776 | 10001 | 10001 | 20968.4991 | 347/351 |
| switches | 2 | 24029.7411 | 21.859375 | 150122088 | 10001 | 10001 | 23848.0205 | 351/352 |
| switches | 3 | 26122.6686 | 24.078125 | 149400728 | 10001 | 10001 | 25927.266 | 352/352 |

## after-10 (interval 10 seconds)

| Scenario | Run | Duration ms | CPU seconds | Allocated bytes | Foreground queries | Name queries | Name lookup ms | Handles before/after |
|---|---|---|---|---|---|---|---|---|
| stopped | 1 | 3.214 | 0 | 320120 | 0 | 0 | 0 | 347/347 |
| stopped | 2 | 0.6619 | 0 | 320120 | 0 | 0 | 0 | 347/347 |
| stopped | 3 | 0.6266 | 0 | 320120 | 0 | 0 | 0 | 347/347 |
| disabled | 1 | 0.6462 | 0 | 320120 | 0 | 0 | 0 | 347/347 |
| disabled | 2 | 0.7363 | 0 | 320120 | 0 | 0 | 0 | 347/347 |
| disabled | 3 | 0.6375 | 0 | 320120 | 0 | 0 | 0 | 347/347 |
| stable | 1 | 2.3136 | 0 | 425816 | 1001 | 1 | 0.1228 | 347/347 |
| stable | 2 | 1.9278 | 0 | 425816 | 1001 | 1 | 0.1113 | 347/347 |
| stable | 3 | 2.0237 | 0 | 425816 | 1001 | 1 | 0.1665 | 347/347 |
| titles | 1 | 2.3975 | 0 | 810272 | 1001 | 1 | 0.0991 | 347/347 |
| titles | 2 | 3.6394 | 0.03125 | 810272 | 1001 | 1 | 0.1018 | 347/347 |
| titles | 3 | 4.6596 | 0 | 834296 | 1001 | 1 | 0.104 | 347/347 |
| switches | 1 | 2242.5846 | 2.390625 | 15969408 | 1001 | 1001 | 2223.3782 | 347/353 |
| switches | 2 | 2634.1448 | 2.5 | 15245736 | 1001 | 1001 | 2613.1866 | 353/353 |
| switches | 3 | 2329.8663 | 2.1875 | 15242872 | 1001 | 1001 | 2310.9082 | 353/353 |

## after-60 (interval 60 seconds)

| Scenario | Run | Duration ms | CPU seconds | Allocated bytes | Foreground queries | Name queries | Name lookup ms | Handles before/after |
|---|---|---|---|---|---|---|---|---|
| stopped | 1 | 3.2007 | 0 | 320120 | 0 | 0 | 0 | 345/345 |
| stopped | 2 | 0.5944 | 0 | 320120 | 0 | 0 | 0 | 345/345 |
| stopped | 3 | 0.6175 | 0 | 320120 | 0 | 0 | 0 | 345/345 |
| disabled | 1 | 0.6091 | 0 | 320120 | 0 | 0 | 0 | 345/345 |
| disabled | 2 | 0.7776 | 0 | 320120 | 0 | 0 | 0 | 345/345 |
| disabled | 3 | 0.5954 | 0 | 320120 | 0 | 0 | 0 | 345/345 |
| stable | 1 | 1.4087 | 0 | 339080 | 167 | 1 | 0.1185 | 345/345 |
| stable | 2 | 1.738 | 0 | 339080 | 167 | 1 | 0.099 | 345/345 |
| stable | 3 | 1.3761 | 0 | 339080 | 167 | 1 | 0.1033 | 345/345 |
| titles | 1 | 1.8372 | 0 | 403408 | 167 | 1 | 0.0966 | 345/345 |
| titles | 2 | 1.39 | 0 | 403408 | 167 | 1 | 0.0973 | 345/345 |
| titles | 3 | 1.9102 | 0 | 403408 | 167 | 1 | 0.0988 | 345/345 |
| switches | 1 | 382.891 | 0.46875 | 2801104 | 167 | 167 | 377.2585 | 345/345 |
| switches | 2 | 384.6957 | 0.390625 | 2800992 | 167 | 167 | 381.0749 | 345/345 |
| switches | 3 | 368.9776 | 0.40625 | 3522744 | 167 | 167 | 365.3996 | 345/351 |

## Decision and limits

Keep the bounded process-lifetime cache. At one second, stable-window name queries fell from 10,001 to 1 while foreground/title reads remained 10,001. Stable workload allocations fell from 8.8–9.5 MB to 1.36 MB; execution time varied from 558–1,378 ms before to 5.4–5.9 ms after. At the 10-second default and 60-second maximum, foreground queries were 1,001 and 167 respectively. Stopped and disabled workloads made no foreground queries.

Switching processes invalidates the cache. The synthetic switch workload includes a system process with expensive process-name queries; compare raw runs without extrapolating to typical app switching. It made 10,001 name queries both before and after at one second. Longer intervals also miss intermediate scripted transitions by design. Stopped/disabled CPU readings stayed near the measurement resolution; the first after-1 stopped run took 39.6 ms, while the next two took 0.65/0.63 ms, so this was not a consistent regression.

Stable-workload handles stayed flat after the change. Switching-workload retained counts increased by 5–6 in some runs and then levelled off; aggregate process counts include test-runtime activity, so they cannot alone prove absence of a resource leak. Lifetime/disposal tests separately verify PID reuse and exactly-once cache release. These results establish component work reduction, not whole-app CPU, memory, or battery savings. Settings reads, worklog writes, and UI idle costs remain unmeasured.

## Reproduce

Set `FOCUSTIMER_POLLING_REPORT` to an absolute output path and `FOCUSTIMER_POLLING_INTERVAL` to 1, 10, or 60, then run `dotnet test tests/FocusTimer.Platform.Windows.Tests -c Release --filter FullyQualifiedName~ActivityPollingMeasurements`. The driver is inert unless the report variable is set. Raw JSON reports reside under ignored `artifacts/polling-*.json`; the tables above preserve every run.
