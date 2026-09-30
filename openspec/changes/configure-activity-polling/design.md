# Design

## Context

See `proposal.md` for motivation and the two delta specs for behavior contracts.

`TimerService` owns a one-second `System.Timers.Timer` and requests `SessionTracker.OnTimerTickAsync` on every tick. That method waits on a semaphore, looks up the foreground window, then splits at midnight and compares process name/title. Slow asynchronous implementations can therefore accumulate waiting ticks. `StartAsync` performs its initial lookup outside that gate, so startup also needs coordination.

`WindowsActiveWindowService` currently performs synchronous Win32 lookups and calls `Process.GetProcessById` for each observation. Its internal delegates make failure cases testable. The service is registered as a singleton behind `IActiveWindowService`, which does not expose platform internals.

Settings load and reload through `TimerWidgetViewModel.InitializeSettingsAsync`; successful Settings Apply raises `SettingsApplied`, which `AppController` handles. The developer expander already exists in the About tab. JSON settings use the default serializer, whose failure currently resets the whole settings object. Existing scheduling tests inject a clock for civil time but do not advance monotonic timestamps. Tests use xUnit; the planning context was corrected to match.

## Goals / Non-Goals

**Goals:**

- Keep civil-time segment boundaries and monotonic scheduling independent.
- Bound work across periodic ticks, initial capture, pause/restart, and live interval changes.
- Make cache validity depend on a process lifetime, not merely a recycled process ID.
- Establish reproducible measurements before claiming savings.

**Non-Goals:**

- No change to timer elapsed-time arithmetic, break reminders, idle detection, segmentation rules, worklog storage, or settings-read caching.
- No Windows event hooks, extra recurring timers, new public platform contract, or real Linux implementation.
- No attempt to infer foreground visits between samples.

## Decisions

### 1. Persist a validated whole-second interval

Add `Settings.ActivityPollingIntervalSeconds`, default 10. The 1–60 range is the developer control limit; measure 1 second for comparison with the previous implementation, 10 seconds for the new default, and 60 seconds at the upper bound. Invalid numeric values normalize to 10 in the model. Handle fractional/nonnumeric JSON values locally with a property-specific tolerant integer converter so one invalid field does not discard other settings. Write a normal JSON integer. Global serializer permissiveness would affect unrelated settings, so avoid it.

Bind a numeric control through a view-model value that validates before assigning to `Settings`; preserve invalid-input feedback instead of silently rounding fractional input. Use the existing developer unlock and Apply/OK save flow. Successful save activates the normalized value. Editing a draft must not change tracking. Retain the last applied value on save failure and Cancel; Cancel after an earlier Apply preserves that earlier value.

`TimerWidgetViewModel.InitializeSettingsAsync` applies the interval to the tracker before enabling/starting capture. Reuse its reload path rather than introduce a second settings observer. Setting the same effective interval is a no-op, avoiding rescheduling during unrelated theme/settings updates. Developer visibility controls presentation only.

### 2. Schedule inside SessionTracker using the existing tick

Keep `TimerService`'s one-second tick and display behavior. `SessionTracker` holds the effective interval and the monotonic origin/deadline, using its injected `TimeProvider.GetTimestamp`/`GetElapsedTime`. Wall-clock time remains responsible for entry timestamps and local-midnight boundaries.

Each maintenance tick performs, under the short state lock:

1. Check tracking/session validity and split at any crossed local midnights using the existing civil-time helper.
2. Check the monotonic foreground deadline.
3. If capture is not due or already in progress, return without queuing a periodic request.
4. Otherwise reserve the capture slot for the current session generation and schedule revision, then release the lock before the OS call.

After the lookup, reacquire the state lock, reject stale session results, split any midnight crossed during the lookup, and compare the observed window. A normal capture schedules the next attempt one interval after completion, including lookup failures. There is no historical catch-up. The one-second maintenance opportunity means cadence is best effort and can be delayed by timer scheduling; it never causes extra polls to recover missed samples.

Applying a different interval anchors the next deadline at apply time plus the new interval. A schedule revision prevents an already-running lookup from overwriting that new deadline on completion; its result can still update the same valid session. Closing segments because an interval changed would introduce false activity transitions, so do not do that.

An independent periodic timer was considered, but it adds another lifecycle and wakeup source without being needed for whole-second intervals.

### 3. Bound startup and stale results as well as ticks

All foreground calls, including startup, share one capture reservation. Initial capture has priority over periodic sampling and occurs immediately when the slot is available. If an old session's lookup is still running, retain only the latest pending initial-capture request, tagged with session generation; a newer start replaces it. Complete superseded requests without OS work. Completion of the old call discards its stale result and dispatches the latest still-valid initial request.

Do not hold `_stateLock` over an await or Win32 call. Stop/disable invalidates the generation and clears pending capture state; it retains existing closure/discard behavior. A lookup failure uses the existing Unknown fallback at initial capture. Pause/restart cannot reopen old segments. Ensure tasks started by `TimerService` are observed and failures handled; `ConfigureAwait(false)` alone does not observe a fire-and-forget task.

Maintenance must continue during slow lookups. The existing waiting semaphore cannot wrap the entire tick anymore; use a non-waiting reservation for capture and keep maintenance outside it. On stop, split any still-crossed midnight boundaries before final closure so delayed callbacks do not create a cross-day segment.

### 4. Cache one Windows process with an owned lifetime handle

Keep `IActiveWindowService.GetForegroundWindowAsync` unchanged. Introduce an internal, injectable process accessor in Platform.Windows with operations to open an owned process reference, obtain its existing process-name representation, check whether that same lifetime is alive, and dispose it. Tests supply fake lifetimes rather than real OS processes.

The production reference pins a process handle that supports a non-blocking liveness check; opening/identifying it and resolving its name happens on cache miss. A cached entry holds PID, name, and that owned reference. For every capture:

- Read foreground handle, current title, and PID as today.
- Reuse the name only if PID matches and the owned lifetime is still alive.
- Otherwise release the cached reference and resolve the current process again.
- Keep at most one cached entry. Clear it when foreground is absent, PID is zero, lookup fails, or a different process is observed.
- If a lifetime handle cannot be obtained but the existing name lookup works, return that name without caching it. Preserve existing Unknown/Process_ID error mappings and retry on later captures.

An owned lifetime prevents a new process with the same PID from receiving the previous name. A PID-only dictionary or a time-based expiry could report stale attribution. Reading process start time on every sample was also considered, but may retain the expensive per-sample process query.

Serialize cache access within the platform service, including disposal, because it may have callers outside the tracker. Implement deterministic disposal on the concrete singleton and verify Host DI disposes it. Verify handles are released on invalidation and shutdown. Keep Linux stubs unchanged and compiling; native handle behavior remains Windows-only under OI-06.

### 5. Make correctness and measurements the acceptance gates

Extend existing xUnit suites with a clock that independently controls civil time and monotonic time. Test exact due boundaries, wall-clock jumps, initial capture, interval changes, slow lookups, latest-start replacement, failure cadence, disable, pause, midnight/DST, and same-process title changes. Compare normalized segments at an explicitly configured one-second interval against the baseline sequence, ignoring generated IDs and execution-dependent timestamps where necessary.

Record the baseline revision before implementation. Use an isolated Release test driver with synthetic settings/worklogs and scripted observations, plus a Windows capture workload, without modifying the user's running session. Do not store personal window titles in measurement outputs. Run the same workload three times per revision after warmup and record raw values, workload duration, .NET/build/OS versions, and machine details.

Workloads cover paused/stopped, running with logging disabled, one stable foreground window, changing titles within one process, and switching processes. Compare one-second capture with the baseline before measuring the new 10-second default and the 60-second upper bound. Record CPU seconds, allocations, foreground/name lookup counts, lookup durations, and handle count before/after repeated switches and disposal. Avoid permanent per-tick production logs; use test counters or profiling tools.

At the default interval, steady-state observations should require one initial name resolution per continuously alive foreground process rather than one per sample. All behavior tests must pass and repeated process switches must not cause monotonically growing retained handles. Compare CPU/allocations with run-to-run variation reported; investigate a consistent regression before accepting the cache. If lifetime checks negate the benefit, remove that cache portion and record the result while retaining the configurable scheduler. OI-17 stays open for the other resource paths.

## Risks / Trade-offs

- Longer sampling intervals miss brief visits and delay attribution changes. Mitigation: explain the 10-second default, the sampling tradeoff, and the option to select 1 second in the control; document observation-time attribution.
- A process can exit between Win32 calls. Mitigation: validate owned lifetime and retain current defensive fallbacks; do not promise an atomic OS snapshot.
- A slow/hung platform lookup can delay initial capture for a new session. Mitigation: permit only one call and one latest pending startup; maintenance and stop/disable remain responsive. Native lookup cancellation is outside the existing interface.
- One retained process handle raises steady-state handle count slightly. Mitigation: bound cache to one, verify disposal, and measure the net cost.
- Invalid JSON interval can discard unrelated settings under the current generic error path. Mitigation: property-local tolerant parsing and targeted persistence tests.

## Migration Plan

1. Capture baseline evidence, then implement and validate this change on Windows.
2. Existing JSON settings omit the field and adopt the new 10-second default; no worklog migration is required. Persist the value on normal settings save.
3. Update OI-14 with the delivered polling subset and remaining segmentation scope; attach capture measurements to OI-17 without marking the whole performance issue done. Align root guidance if affected and sync the two delta specs only after implementation.
4. Selecting 1 second restores the previous sampling cadence. The new default is 10 seconds. Reverting the implementation restores uncached one-second behavior; older binaries ignore the additive JSON field and worklogs remain compatible.
