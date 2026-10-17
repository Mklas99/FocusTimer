# Widget frost verification

Final acceptance is recorded in the 2026-10-12 section at the end of this file. Earlier sections retain the
prototype and integration history, including failures and blockers resolved by later runs.

## Initial App policy groundwork, 2026-10-10 (reporting gap resolved below)

Added the pure App `WidgetMaterialPolicy` with Clear/Frosted/Solid/SolidFallback and fallback reasons, plus an
optional binding to the fork's public `Win32BackdropControllerV1` API. No private framework visuals are accessed.
The wrapper retains Avalonia ownership, rejects obsolete handles and disposed updates, and exposes queued-update
completion separately from update acceptance. Production dependencies and the widget's Off/Solid behavior remain
unchanged. Task 2.1 remains open until the window integration is connected.

Lifecycle integration exposed a V1 reporting gap. In `backend.patch`, `ScheduleBackdrop` catches a composition
failure, sets `_applied = false` and attempts to turn blur off. `TryUpdate` already returned true to the UI thread;
`IsApplied == false` also describes pending work. There is no public failure/completion notification or distinct
failure status, and the window's ActualTransparencyLevel is not updated by that catch. An App policy based only on
accepted updates and ActualTransparencyLevel could keep a clear low-tint shell after native failure. Periodic App
polling would violate the no-polling design and still needs a timeout assumption to distinguish failure.

Resolve this before connecting the widget lifecycle. A narrow public completion/status notification, delivered on
the UI thread with handle/update identity, would allow opaque fallback after failure and retries on activation/show
without private reflection or polling. This requires updating the selected API/design and qualifying a new pinned
fork package. The existing prototype qualification is preserved; it does not cover this asynchronous failure path.

The proposed API revision would report Pending/Applied/Failed/Unavailable and notify completion on the UI thread
with the controller's handle and update generation. App would accept only the current handle's latest generation,
keep the shell opaque until Applied, fall back immediately after failure, and retry on activation/show or observed
preference changes. Notifications after disposal would be ignored. This is a proposed narrow extension to Avalonia's
existing composition ownership, not an independent composition target.

Validation of this groundwork: 44 focused policy, capability, ThemeManager, desktop-material and backdrop-level
tests passed with stock dependencies, including five new tests. Release Windows Host build passed with zero
warnings/errors. Strict OpenSpec validation and `git diff --check` passed. No new native visual or performance
acceptance is claimed, and progress remains 3/14 tasks complete.

## Contract corrections

The frost delta now follows the Settings contract. Failed writes retain the draft preview. Successful compensation after activation failure restores saved and nonappearance values, then retains the appearance draft preview. Cancel or title-bar close restores the last successful commit. Failed compensation follows recovery-required behavior. Unsupported material uses solid fallback independently of commit success.

The visual gate now requires contrast-normalized 10%-90% edge widening, minimum visible desktop detail, rejection of an opacity-only control, and tracking of moving and replaced underlying window content. Raw contrast reduction cannot qualify blur.

## Native prototype, 2026-10-10

`WidgetFrostPrototypeTests` creates the actual `TimerWidgetWindow` with its existing foreground and single shell. Only the native transparency hint and shell tint are overridden in the fixture; no production Frosted choice or serialized value is added. It captures screen pixels and averaged foreground-free scanlines, rather than headless rendering.

Environment: Windows build 26300, Avalonia 11.2.0, Intel Arc 140V GPU (16GB), driver 32.0.101.8626, display scale 100%, full-mode widget at 3x (909 x 182 logical pixels), overall opacity 1, Windows transparency preference enabled. The precise selected graphics/composition backend was not recorded, so this is partial evidence rather than completed qualification.

The broad-stripe run passed the initial edge/detail checks for AcrylicBlur at 0%, 25% and 50% shell tint. It did not qualify Blur, which resolved to Transparent.

| Pattern width | Off normalized edge width | Acrylic normalized edge width | Acrylic plateau separation at 0% / 25% / 50% tint |
| --- | --- | --- | --- |
| 96 physical pixels | 0.8 px | 56.45 / 56.60 / 56.67 px | 127 / 96 / 64 luminance levels |
| 128 physical pixels | 0.8 px | 75.70 / 76.60 / 75.80 px | 193 / 146 / 96 luminance levels |

Off plateau separation was 255 / 192 / 128 levels. The opacity-only negative control reduced separation to 39 levels but retained a 0.8-pixel edge width and failed the blur criterion as intended. Solid controls passed the less-than-2-level separation gate. There were 7 valid edges at width 96 and 6 at width 128.

An earlier run with 24/48-pixel stripes failed the detail gate. Those patterns are too narrow to establish local plateaus for this acrylic effect and are not evidence that broad desktop detail is absent. The fixture uses 96/128-pixel stripes for the current first gate.

The fixture was then extended with known 16-pixel underlying-window movement, normalized displacement measurement in active/inactive topmost states, a return-capture check, and dark/light content replacement. That run and one retry failed at the initial Off control: captured pixels showed other desktop content rather than the stripe window. This is an invalid capture environment. No live-tracking result was obtained; it is not a backend failure or a passing qualification.

Local raw reports, PNGs and scanline CSVs are under the ignored `tests/FocusTimer.App.Tests/bin/widget-frost-prototype-broad/` directory. The invalid live runs are under `widget-frost-prototype-live/` and `widget-frost-prototype-live-retry/` alongside it. Rerun the fixture separately with `FOCUSTIMER_NATIVE_APPEARANCE_TESTS=1` and `FOCUSTIMER_EVIDENCE_DIR` set to an evidence directory.

## Remaining feasibility work

Task 1.1 is now complete; tasks 1.2-1.3 remain open. The resumed run below resolves the capture problem and live-tracking uncertainty, then identifies an actual clipping/fade failure. No backend has passed the complete feasibility gate, so production UI work remains blocked.

The pinned [Avalonia 11.2.0 Windows implementation](https://github.com/AvaloniaUI/Avalonia/blob/11.2.0/src/Windows/Avalonia.Win32/WindowImpl.cs) checks composition-effect support before accepting material hints. Current documentation retrieved through Context7 is broader than this pinned implementation and does not replace native evidence.

## Validation

- OpenSpec strict validation passed after the contract corrections.
- `git diff --check` passed.
- Native broad-stripe prototype passed (one test); the initial narrow-pattern experiment failed its detail gate.
- The extended native live-tracking test compiled without new analyzer warnings, but its run and retry failed the Off-control visibility guard. Live tracking remains unverified.
- Build output reported missing local Sonar integration targets; this was informational and did not prevent compilation. No full-suite or release acceptance claim is made.

## Resumed feasibility run, 2026-10-10

Full-window captures revealed that the widget remained visible while unrelated windows covered the non-topmost pattern window underneath it. The fixture now keeps the pattern topmost beneath the widget, restores that ordering when activating the pattern, captures the entire widget as well as scanlines, and rejects inconsistent plateau separation across a pattern. Test-only reflection records the native surface without mutating Avalonia internals: `Avalonia.Win32.WinRT.Composition.WinUiCompositedWindowSurface` on `Avalonia.Win32.WindowImpl`.

The corrected live run passed (one test, 93 seconds). Off tracked the known 16-pixel underlying-window movement at both stripe widths and all three tints. AcrylicBlur also tracked 16 pixels in both active and inactive topmost samples at all widths/tints; returned profiles had zero mean absolute luminance error. Replacing the pattern with dark/light window content changed mean luminance by 255 / 192 / 128 levels at 0% / 25% / 50% tint. This proves live window sampling under the tested conditions, beyond a successful API result. Foreground is present in the full native captures; formal foreground sharpness and the full mode/placement matrix remain outstanding.

The final fixture adds an opacity-only control matched to the measured acrylic residual contrast, verifies the widget activation state, and checks clipping/fade before accepting a backend. That probe compiled successfully and failed the native feasibility gate, with an earlier probe producing the same clipping/fade result. At the 3x full-mode shell, a 2x2 patch at logical (8,8), outside its 36-pixel rounded fill, differed from Off by 24.5 luminance levels. At overall opacity zero, the foreground disappeared but blur remained: its scanline differed from the clear desktop by 87.539 levels on average. These are valid native failures, not obscured captures. Blur still resolved to Transparent. A passing live-blur result must not override either failure.

The pinned [WinUiCompositedWindow source](https://github.com/AvaloniaUI/Avalonia/blob/11.2.0/src/Windows/Avalonia.Win32/WinRT/Composition/WinUiCompositedWindow.cs) owns the HWND desktop target, root container and private blur visual. Its rounded clip is supplied at construction by the global backend option; it does not track the widget's mode-dependent radius or Avalonia visual opacity. The [surface source](https://github.com/AvaloniaUI/Avalonia/blob/11.2.0/src/Windows/Avalonia.Win32/WinRT/Composition/WinUiCompositedWindowSurface.cs) reads that option while creating the internal render target. No public per-window insertion/clip/opacity hook was identified in these pinned paths.

The independent host-backdrop alternative therefore needs a concrete coexistence design before creating another HWND composition target. Microsoft documents [composition target limits and ordering](https://learn.microsoft.com/en-us/windows/win32/api/dcomp/nf-dcomp-idcompositiondevice-createtargetforhwnd); those limits are a warning about target ownership, not proof that WinRT interoperation is impossible. Do not replace Avalonia's root or use reflection into its native visuals in production. No host-composition adapter has been implemented and task 1.3 is not complete.

New local evidence is under ignored `tests/FocusTimer.App.Tests/bin/widget-frost-stacking/` (passing live run), `widget-frost-geometry/` and `widget-frost-final-probe/` (clipping/fade failure), plus `widget-frost-visibility/` (capture consistency guard). Task 1.1 is complete; task 1.2 is partial with an identified failed backend. Groups 2-4 have not started.

## Approved backend plan revision, 2026-10-10

The user approved evaluating a maintained Avalonia backend extension exposing per-window backdrop clipping and opacity. Proposal, design, tasks and widget scenarios now reflect that route. Task 1.3 evaluates a narrow, public capability while preserving Avalonia's existing target and native resource ownership; it requires immutable dependency provenance, aligned packages, reproducible clean builds, maintenance/upgrade checks and the full native feasibility gate. An independent composition adapter or broad renderer rewrite still needs another reviewed design.

At approval, the extension had not been built or qualified. The stock backend's failed clipping/fade result remains a regression control. Implementation evidence follows below.

## Backend extension prototype, 2026-10-10

The test-only fork is retained under `third_party/avalonia-widget-backdrop/`. It pins upstream commit `68ab391c0a3653e0722638e29fcbd9633c7fd386`, five modified backend files, two source overlays, NuGet dependency locks and the upstream MIT notice. Its public `Avalonia.Win32.Win32BackdropControllerV1` capability queues physical-pixel radius, overall opacity and enabled state through Avalonia's existing composition commit loop. Avalonia retains the HWND target, foreground surface and native resource ownership. Capability creation rejects other renderers and obsolete handles. `IsApplied` reports queued update completion, not visible blur. App and Host keep stock dependencies; only the explicitly enabled test project selects the fork.

The build procedure and upgrade checks are in `third_party/avalonia-widget-backdrop/README.md` and `build.ps1`. The script checks the pinned revision and patch checksum and restores locked dependencies in a fresh ignored checkout. The backend builds against official pinned runtime metadata, because consumer reference facades prohibit implementing backend interfaces. Other Avalonia packages remain 11.2.0. The final fork package is `Avalonia.Win32.11.2.0.4-focustimer.4`, assembly identity 11.2.0.0, built with .NET SDK 8.0.425. No official package or cached DLL is replaced.

Two fresh checkouts produced identical final backend assemblies with SHA-256 `AE80AF171FA8744184DD187004E6CD891B087A9330F097D3AAD2656134A23401`. Earlier prototype versions differed because debug provenance embedded absolute checkout paths. Canonical path mapping and disabling SDK SourceLink for the patched fork resolve this; ZIP timestamps are excluded from the assembly comparison. `provenance.json` records source-input hashes and the verified assembly hash.

At 100% display scale on the same Windows/Intel/WinUI composition environment, the extension retained the stock acrylic edge/detail results at both stripe widths and all three tints. Contrast-matched opacity-only controls retained 0.8-pixel edges and failed blur qualification. Both active and inactive widget captures tracked the known 16-pixel underlying-window movement with zero return error; replacing dark/light content remained visible. Z-order setup now moves the fixture backdrop one pixel and back before the inactive baseline, because the initial reorder left a stale DWM sample until an underlying-window update. Every measured baseline must independently contain visible edges; setup motion is excluded from the measured 16-pixel displacement.

The extension fixes the stock geometry/fade failures in native pixels: the outside-rounded-corner difference and zero-opacity desktop error were both 0 luminance levels. At half opacity the blur residual was 43.876 levels versus 87.539 at full opacity. Changing radius from rounded to square and back changed the corner by 24.5 levels only in the square state. On uniform black underlying content, all 16,660 bright clock pixels and the complete foreground crop matched Off with zero mean channel error. This demonstrates sharp foreground independently of background edge softening.

The expanded intermediate-fade/foreground run passed in its own process (one test, 122 seconds), with local evidence in ignored `tests/FocusTimer.App.Tests/bin/widget-frost-extension-v1-qualified/`. Earlier extension runs are in `widget-frost-extension-v1/` and `widget-frost-extension-v1-retry/`; the first failed its inactive baseline, while the corrected run passed. Combining two native fixture classes in one process failed at the second AppBuilder initialization; the existing appearance fixture passed. Native fixtures must be invoked in separate processes.

The final package's native widget run passed (one test, 128 seconds). It verified three hide/show cycles, rejection of a closed HWND, attachment to a newly created TimerWidgetWindow, and rejection after controller disposal. The recreated window has no ViewModel, so that portion checks backend lifetime only. It does not substitute for the release workload and 100-cycle resource-budget test.

The expanded final-package run passed (one test, 153 seconds) for all 12 geometry combinations of 0.5x/1x/3x, full/compact and beside/below at 100% display scale. Each outside-corner sample matched Off with zero luminance error, with native captures and bounds/radius recorded. These samples prove dynamic boundary updates; they do not qualify every combination's edge/detail or pointer behavior. The existing native appearance fixture passed separately (one test, 9 seconds). Evidence is under ignored `widget-frost-extension-v4/` and `widget-frost-extension-v4-matrix/` directories in the test bin directory.

The existing Settings dense-frost/fallback fixture passed with the final fork (one test, 5 seconds). Its first capture saw desktop wallpaper; the next showed that Settings startup centering had placed the realized window outside the stripe window. The fixture now keeps both windows topmost and positions Settings after Show, then records its actual client origin. The valid run measured frost max adjacent-pixel step 1, opaque fallback standard deviation 0, and unblurred control step 13. Earlier invalid captures remain recorded under `widget-frost-extension-v4-settings/` and `widget-frost-extension-v4-settings-visible/`; valid evidence is in `widget-frost-extension-v4-settings-positioned/`. At that point, only the non-opted-in Settings material had been tested.

Fifty-one focused material, theme and Settings transaction tests passed with the fork. Release win-x64 self-contained single-file Host publishes succeeded with stock and fork dependencies, using an explicit test-only import without editing Host's production dependency declarations. Stock publish total was 104,046,203 bytes; fork total was 104,050,469 bytes (+4,266). The executables were 103,807,875 and 103,812,141 bytes respectively. The backend DLL is 957,952 bytes versus 952,832 for stock net8.0 (+5,120); no new native dependency is introduced. Publish evidence stays under ignored `.build/publish-backdrop-stock/` and `.build/publish-backdrop-fork/`.

The desktop native fixture was extended to create the actual Worklog window without a data ViewModel and sample its empty outer padding. Settings and Worklog shell qualification passed in separate stock/fork runs (one test per process, 9 seconds each), with identical measured frost, solid and unblurred-control values. This verifies the non-opted-in native shell policy, not Worklog data interactions. Evidence is under ignored `widget-frost-stock-desktop/` and `widget-frost-extension-v4-desktop/`.

Normal stock dependencies were restored after the prototype measurements. The Release Windows Host build passed with zero warnings/errors, and the same 51 focused tests passed in the stock configuration. Prototype compilation also checks the public ScalingChanged hook, which updates physical clip radius when a real DPI event arrives.

At the end of the 100% session, task 1.3 remained open for real DPI transitions. The user initially chose to keep 100%, then changed the main screen to 150% and requested more native testing. The resumed evidence follows. The release matrix, preference/session fallbacks, performance budgets and full lifecycle stress remain pending in groups 2-4.

## Native 150% and live DPI checks, 2026-10-10

The main display now reports scale 1.5 on the actual widget, with the same Windows build 26300, Intel Arc GPU and WinUI composition backend. The static 150% run passed (one test, 151 seconds), including all six edge/detail combinations, matched-opacity rejection, active/inactive live tracking, zero/intermediate fade, foreground, all 12 mode/placement/scale boundary combinations and basic lifetime checks.

| Stripe width (logical / physical) | Off edge width | Acrylic edge width at 0% / 25% / 50% tint | Acrylic plateau separation at those tints |
| --- | --- | --- | --- |
| 96 / 144 pixels | 0.8 px | 83.05 / 82.28 / 82.80 px | 213 / 160 / 106 luminance levels |
| 128 / 192 pixels | 0.8 px | 100.60 / 100.75 / 100.40 px | 246 / 185 / 123 luminance levels |

Off separation was 255 / 192 / 128 levels. All active/inactive known movements measured 16 physical pixels and all return errors were zero. The outside-corner and zero-opacity desktop errors were zero; half-opacity blur residual was 28.671 levels versus 57.109 at full opacity. All 37,506 bright clock pixels and the complete foreground crop matched Off with zero mean channel error. Evidence is under ignored `tests/FocusTimer.App.Tests/bin/widget-frost-extension-v4-150/`.

The monitor inventory records a main 3440x1440 display at 150%, a left 2880x1800 display at 150%, and a right 1050x1680 display at 100%. The fixture can therefore test real DPI changes without changing system preferences. It moves the same compact 1x widget and pattern to the 100% monitor and back to the 150% main display. The first transition run passed (one test, 163 seconds), received two ScalingChanged events and retained the same HWND. Physical clip radius changed from 8 to 12 pixels, outside-corner error stayed zero, and replacing dark/light underlying content changed the interior sample by 255 levels at each scale. Evidence is under ignored `widget-frost-extension-v4-dpi-transition/`.

The final transition probe also compares a fixed physical boundary pixel against Off, with the foreground outline removed only during this clip calibration. The pattern edge is aligned to that pixel: it must be visibly blurred inside an 8-pixel radius at 100% and remain clear outside a 12-pixel radius at 150%. This distinguishes correct physical clipping from a stale radius that would pass an outermost-corner-only check.

The initial physical (2,2) probe failed, as did the next (2,3) bright-side probe, which had only 5 levels of separation near the window edge. Native 24x24 captures confirmed the underlying edge was correctly aligned but the bright-side result was nearly white. The calibrated probe uses physical (2,3) on the dark side, requires the Off sample to be within 2 levels of black, and retains the minimum 10-level blur threshold. It passed in the focused live-DPI test (one test, 30 seconds): difference from Off was 245 levels at 100% and zero at 150%. Two real scaling events occurred on the same HWND, with zero outside-corner error and 255-level live content changes at both scales. Lifetime checks also passed afterward. Evidence is under ignored `widget-frost-extension-v4-dpi-probe-dark/`; failed calibration attempts remain in `widget-frost-extension-v4-dpi-calibrated/`, `widget-frost-extension-v4-dpi-probe/` and `widget-frost-extension-v4-dpi-probe-origin/`. The focused test explicitly does not repeat background edge/detail or foreground qualification, which the full 150% run already passed.

Non-opted-in Settings/Worklog native shell checks passed separately at measured scale 1.5 (one test, 7 seconds), with frost step 1, opaque fallback standard deviation 0, and unblurred step 13. The native appearance fixture also passed (one test, 4 seconds). The Worklog fixture checks its real shell without a data ViewModel, not data interactions. Evidence is under ignored `widget-frost-extension-v4-desktop-150/`.

Tasks 1.2 and 1.3 are complete. The selected prototype backend is the maintained Avalonia.Win32 fork `11.2.0.4-focustimer.4` using AcrylicBlur on the WinUI composition backend, qualified on Windows build 26300 with transparency enabled, at 100%/150% and real cross-monitor transitions. Stock Blur remains unsupported and stock Acrylic remains a recorded clipping/fade failure. This completes the prototype decision, not release acceptance or universal Windows support. Overall progress is 3/14 tasks complete; production material/Settings integration and groups 2-4 remain open. Display scale 200%, preference/session fallbacks, full interactions, workload budgets and 100-cycle stress remain unverified.

## App integration and notification revision, 2026-10-10

The user's request to continue authorized resolving the V1 reporting gap and completing integration. The selected
backend is now `Avalonia.Win32` `11.2.0.5-focustimer.5`, retaining assembly identity 11.2.0.0 and the same upstream
commit. `TryUpdateWithCompletion` posts composition completion/failure to the UI thread. App accepts only the
current session/handle/update generation, keeps an opaque shell while pending or failed, and retries on
activation/show or observed preference changes. Surface disposal reports failure. There is no App polling,
private visual access, desktop capture in rendering, or second composition target.

`WidgetWindowMaterial` connects the real widget shell, corners, bounds, overall fade, DPI and lifetime to the public
capability. High Contrast, disabled transparency, unsupported sessions/renderers, missing capability and native
failure select a readable solid fallback without changing saved Frosted/tint. Diagnostics report requested,
actual transparency, effective material and reason. Settings offers Off/Frosted/Solid, with tint enabled for
Off/Frosted. Existing draft transactions retain the appearance after failed writes or successful compensation;
Cancel/title-bar close restores the last successful commit. Failed compensation retains recovery data and blocks
new commits. Clone/save/load/export/import, imported preset identity and rollback-to-Off tests cover Frosted;
legacy normalization and factory defaults remain unchanged.

Normal App/Host dependencies select the fork from the root local NuGet feed. `FocusTimerUseStockBackend=true`
selects the stock dependency and unavailable-capability fallback. The maintained backend build instructions,
source inputs, locked dependencies and licenses are in `third_party/avalonia-widget-backdrop/`.

A fresh network checkout built through `build.ps1` produces the same assembly as the development checkout:

- Upstream: `68ab391c0a3653e0722638e29fcbd9633c7fd386`; SDK 8.0.425.
- Patch SHA-256: `0046A4C388DC4F27E1C6EC1161E66042E4B1F3479FDF5F2EE9F9E6B5A1F66EA4`.
- Assembly SHA-256: `CBB6DA0CCBB1ADF1067D16D0D866DAEEF4302F20D90E8C4B05F630A01DD4E22D`.
- Clean package SHA-256: `1D13204BDB3FA2E4A704F10E9B3EF7D266F5C670F733E4FC2046CAC25316106D`.

### Native integration evidence

Windows build 26300, WinUI composition, Intel Arc 140V/driver 32.0.101.8626, local session, Windows transparency
enabled. The current displays are main 100%, left 200%, right 100%. Earlier 150% evidence remains historical
qualification of package `.4`; package `.5` production interaction acceptance at 150% remains unverified.
Each fixture runs in a separate native test process. Captures, scanlines and reports remain in ignored local
`tests/FocusTimer.App.Tests/bin/` directories:

| Fixture / directory | Result and scope |
| --- | --- |
| `widget-frost-notifications-v5` | Full prototype passed at 100% (170 s): normalized live blur/detail, opacity-only rejection, moving/replaced underlying content, inactive topmost, exact foreground, clipping/fade and controller lifetime. UI-thread completion is asserted. |
| `widget-frost-production-v5` | Production policy and real shell passed at 100% (73 s): tint profiles, exact foreground, opaque tint, fade, 12 mode/placement/scale combinations, High Contrast and 100 material/hide-show cycles. |
| `widget-frost-production-v5-200` | Same production fixture passed at 200% (74 s). Broad-pattern frost edge widths 100.40–108.40 physical px versus Off/control 0.80 px; visible plateau detail 123–255 levels. Exact foreground match; half fade residual 17.721 versus full 35.299; zero residual 0. |
| `widget-frost-notifications-v5-dpi` | Real 100% ↔ 200% transition passed on one HWND (31 s): two scaling events, radius 8/16 physical px, corner error 0 and live color replacement difference 255. |
| `widget-frost-v5-desktop` | Settings/Worklog native shell check passed (8 s); these windows do not opt into the widget controller. The no-ViewModel Worklog fixture proves shell isolation rather than a full Entries/Timeline interaction review. |

Production 100% handle baseline was 646, cycle samples 643, and final count stayed at/below baseline. At 200%,
the baseline was 646 and samples at 25/50/75/100 cycles were 643/643/644/644, with one HWND retained and closed
HWND rejected. These automated probes do not prove physical dragging, keyboard/project interaction, every
imported palette or the complete display-scale matrix.

### Automated regression verification

Affected full suites passed: Core 342, Persistence 105, Windows 70, Host 22 and App Release 434 (973 total;
9 opt-in native fixtures skipped in the ordinary App run). Focused App tests passed 116. The selected headless
placement/transaction set passed 34 in 2m43s; six geometry tests and four imported-Frosted Apply/Cancel/title-bar
scenarios also passed in focused reruns. The combined run was slow but completed successfully; it was not a stall.
Debug solution and Release Windows Host builds passed with zero warnings/errors. Strict change validation and all
23 main specs passed. Working-tree and staged `git diff --check` passed. Patch context lines require a literal
space, so the patch artifact has a scoped Git whitespace attribute; source whitespace remains checked normally.

The main widget/theming specs, root documentation and current backlog now describe the integrated behavior and
its release limits. Groups 2–3 and task 4.3 are complete: **11/14 tasks done**. Tasks 4.1, 4.2 and 4.4 remain open
until the full native interaction and physical drag release gates pass. OI-25/OI-28 remain partial/in progress;
no archive or universal compositor support is claimed.

### Release workload measurements

`ProductionPolicy_MeasuresReleaseWorkloads` passed in a separate Release process at 200% (7m37s).
The report is in `widget-frost-release-v5-workloads-validated/workloads.txt` and `production.txt`.
After five warmup pairs, three 60-second samples per material produced:

| Measurement | Off | Frosted | Gate |
| --- | --- | --- | --- |
| Normalized process CPU samples | 0.3482 / 0.3254 / 0.3645% | 0.3808 / 0.3644 / 0.2571% | Median increase 0.0163 percentage points, below 1 |
| Private bytes samples | 136536064 / 138817536 / 143192064 | 148611072 / 151875584 / 154324992 | Median increase 13058048 bytes (12.45 MiB), below 25 MiB |
| Idle widget scene invalidations | 0 / 0 / 0 | 0 / 0 / 0 | No new periodic redraw |
| 30-second automated movement CPU | 0.9561% | 0.7937% | Recorded, no separate movement CPU threshold |
| Automated movement max UI iteration | 6.106 ms | 4.892 ms | Below 100 ms |
| Movement final private bytes | 166711296 | 167522304 | Recorded |

The movement workload sets native window positions over changing light/dark content. It measures UI job iteration
latency, not presented frames or physical pointer dragging. Those release checks remain open. Process CPU is
normalized by logical processor count. This fixture has the actual widget view/shell, without a running timer
ViewModel; it does not measure the entire recording session workload.

GPU Engine counters were available but three of 28 process counters returned invalid status/value readings. An
initial unfiltered sum was invalid and is excluded. Five filtered samples showed 0% across 25 valid counters,
with three excluded; this partial process reading cannot qualify compositor GPU cost. GPU figures remain
unverified. The raw and filtered counter files are retained alongside the workload report.

The backend DLL is 958976 bytes versus stock 952832 (+6144 bytes); the clean package is 342771 bytes.

The native appearance regression `RealizedWidgetIcons_UseLiveStateColorsInBothModes` also passed in a separate
Release process (7 s), with evidence under `widget-frost-v5-appearance`. Its programmatic control-state checks
supplement, but do not replace, the physical pointer/keyboard walkthrough.

Both Release/win-x64 single-file Host publishes passed. Stock output totals 104071463 bytes; fork output totals
104079825 bytes across the same six files (+8362 bytes). The executable accounts for the size increase; the App
and Host PDB content differs with the build inputs but its size does not. Solution restore and a zero-warning/error
Release Host build were then returned to the default fork configuration.

### Next native walkthrough

Run `.build/publish-widget-v5-fork/FocusTimer.Host.exe`, select Frosted in Appearance, and verify pointer/keyboard
controls, project text entry and physical dragging over changing content for at least 30 seconds versus Off.
Exercise full/compact, beside/below, 0.5x/1x/3x, 100%/150%/200% displays, light/dark content, imported themes and
High Contrast, plus Apply/OK/restart, Cancel/title-bar close, inactive topmost, cross-display movement and hide/show.
Record which combinations were exercised, effects preferences and any visible stalls. Package `.5` at 150% needs
fresh native qualification rather than treating historical `.4` evidence as proof of the notification revision.
Presented-frame responsiveness and the complete interaction matrix remain unverified until that evidence exists.
The idle and automated movement budgets above pass; they do not waive these remaining release requirements.

## Final native acceptance, 2026-10-12

The user reported that widget frost works well and accepted less-smooth live scaling preview for now.
That separate improvement is recorded as **OI-45, Open - deferred**; no scaling optimization is included here.

When asked whether the review covered pointer/keyboard controls, project entry, physical dragging for 30 seconds
versus Off, both widget modes and placements, imported themes/High Contrast and 100%/150%/200% displays, the user
answered **"All listed checks passed"**. This completes the manual portion of the native review for the integrated
backend, including 150% and physical dragging. No additional drag stall was reported. The scaling-preview issue
remains the accepted exception under OI-45; it does not alter the measured resource limits.

This confirmation supplements the native production profiles at 100%/200%, the prior 150% prototype profiles,
same-HWND DPI probes, live underlying-window tracking, clipping/fade and foreground checks, geometry combinations,
100-cycle handle evidence, Settings/Worklog isolation, transaction/import regressions and Release measurements
recorded above. The 150% integrated-backend review is user-reported; no new `.5` numerical edge-profile captures
were collected at that scale. Manual observations are not a presented-frame trace, and the automated movement
latency remains a UI-iteration measurement. These evidence types are kept separate rather than claimed as new
instrumented measurements. No budget was changed: idle CPU/memory/redraw and automated movement latency passed,
and the user confirmed the physical drag comparison passed. Compositor GPU cost remains unverified because the
available process counters were incomplete; no reliable GPU usage claim is made.

Qualification applies to the recorded Windows build 26300, WinUI composition backend and supported local session
with transparency enabled. Other OS/renderer combinations are not qualified; disabled effects, High Contrast,
unsupported sessions and native failures use the tested conservative fallback. This is not universal compositor
support or Linux blur qualification.

With the user's native confirmation and the recorded automated/resource evidence, tasks 4.1 and 4.2 are complete.
Main widget/theming specs and current/root/backend documentation are reconciled. OI-25/OI-28 are completed and
OI-45 stays open. Task 4.4 is complete: **14/14 tasks done**. The change is ready for review/archive and remains
active until explicitly archived. Earlier open-gate statements describe the state on 2026-10-10.
