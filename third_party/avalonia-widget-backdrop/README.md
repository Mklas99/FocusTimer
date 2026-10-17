# Avalonia widget backdrop backend

App uses this maintained fork of Avalonia.Win32 11.2.0. It exposes per-window rounded backdrop clipping, opacity and UI-thread completion/failure notifications while Avalonia retains its HWND target, foreground visual, composition commit loop and disposal ownership. Native release acceptance remains separate from integration.

Upstream is AvaloniaUI/Avalonia commit `68ab391c0a3653e0722638e29fcbd9633c7fd386` (tag `11.2.0`). `backend.patch` changes five upstream files. Its SHA-256 is `0046A4C388DC4F27E1C6EC1161E66042E4B1F3479FDF5F2EE9F9E6B5A1F66EA4`. The two source overlays and both NuGet lock files are versioned alongside it. The upstream MIT notice is in LICENSE.md and the package includes its original licence.md.

## Build and test

Prerequisites are Git, network access to GitHub/NuGet and .NET SDK 8.0.425 (selected by upstream global.json). Run from the repository root:

```powershell
./third_party/avalonia-widget-backdrop/build.ps1 -CheckoutName avalonia-widget-backdrop-clean
New-Item -ItemType Directory -Path third_party/avalonia-widget-backdrop/packages -Force | Out-Null
Copy-Item .build/avalonia-widget-backdrop-clean/packages/*.nupkg third_party/avalonia-widget-backdrop/packages/ -Force
dotnet restore tests/FocusTimer.App.Tests/FocusTimer.App.Tests.csproj -p:FocusTimerBackendPrototype=true --configfile third_party/avalonia-widget-backdrop/NuGet.Config
$env:FOCUSTIMER_NATIVE_APPEARANCE_TESTS = '1'
$env:FOCUSTIMER_EVIDENCE_DIR = Join-Path (Get-Location) 'tests/FocusTimer.App.Tests/bin/widget-frost-extension'
dotnet test tests/FocusTimer.App.Tests/FocusTimer.App.Tests.csproj --no-restore -p:FocusTimerBackendPrototype=true --filter FullyQualifiedName~AvaloniaCandidates_RetainDetailAndWidenNormalizedEdges
```

Use a fresh checkout name for each clean build. The script refuses existing directories and performs no deletion. It verifies the upstream revision, patch and overlay checksums before applying changes, then restores locked dependencies and compares the built assembly against provenance.json. Generated build files stay under ignored .build. Normal App/Host restore uses the root NuGet.Config and the pinned local package. To test stock fallback, restore/build with `FocusTimerUseStockBackend=true` and omit `FocusTimerBackendPrototype`.

Each native test needs its own `dotnet test` process because Avalonia application initialization is process-wide. Do not combine the two frost methods or other native fixtures in one filter. For a focused live-DPI probe, use `--filter FullyQualifiedName~DpiTransitions_UpdatePhysicalClipOnLiveHandle` in a separate invocation with the same prototype property and opt-in environment. That probe needs monitors with different display scales and does not repeat edge/detail or foreground qualification. The full prototype intentionally fails stock clipping/fade qualification when run without the fork.

For a publish-size comparison, publish Host with `-p:FocusTimerUseStockBackend=true` and normally with the fork, using the same Release/win-x64 settings and separate output directories. Restore normally afterward. PublishPrototype.targets remains available for explicitly qualifying fork packages against historical stock dependency declarations; normal builds do not need that import.

The fork package is `Avalonia.Win32` version `11.2.0.5-focustimer.5`; it keeps assembly identity 11.2.0.0 and uses upstream's public signing key. Other Avalonia packages stay at 11.2.0. The extra numeric revision satisfies Avalonia.Desktop's stable dependency floor without replacing a cached official package. Never modify cached DLLs or publish this as an upstream release. Any changed binary requires a new fork version and matching dependency/provenance updates. Deterministic compilation maps checkout paths to a fixed source root; compare assemblies rather than ZIP timestamps.

The standalone backend project compiles against pinned Avalonia runtime metadata. Consumer reference assemblies deliberately prohibit implementing internal backend interfaces. Building against those facades fails; rebuilding unrelated framework modules is unnecessary. Source generators and native code generators use the checked-in lock files.

## Public capability

`Win32BackdropControllerV1.TryCreate(hwnd, out controller)` accepts a live Avalonia window using the WinUI composition backend on a system supporting host backdrop clipping. It rejects other renderers and obsolete HWNDs. Call it and all mutations on the UI thread. `TryUpdate(radiusInPhysicalPixels, opacity, enabled)` queues coalesced updates through the existing composition commit loop. `IsApplied` reports completion of that update, not visible blur or full platform support. The client still needs native visual qualification and solid fallback. Dispose restores the original backend backdrop policy; window closure invalidates the controller. Native visuals and targets remain owned by Avalonia.

`TryUpdateWithCompletion(radius, opacity, enabled, Action<bool>)` adds a UI-thread notification. True reports that the composition thread applied the latest queued state; false reports an update failure or subsequent surface disposal. Coalescing may skip obsolete updates. Notifications stop after controller disposal or handle invalidation. App supplies update-generation identity in its callback and ignores superseded work. Pending is represented by accepted work awaiting notification, unavailable by failed attachment/update acceptance. This does not prove visible blur. No polling or competing target is added.

Only the widget opts in. Unattached windows retain the upstream behavior. App tracks physical radius, overall opacity, material, size and DPI changes, and retries attachment after failure on activation/show or observed preference changes. Production integration evidence uses the real single shell and controller notifications; prototype fixtures retain a test-only bypass for stock regression controls.

## Maintenance gate

The original prototype is qualified on Windows build 26300 with the WinUI composition backend and transparency enabled, at 100%/150% display scale. The notification revision has native production probes at 100%/200% and live same-HWND cross-monitor transitions. The user confirmed the remaining native interaction, physical drag and 100%/150%/200% review on 2026-10-12. Recorded idle/movement and lifetime budgets pass. Other renderer/OS combinations and compositor GPU cost remain unverified. See the change's verification.md for measurements, manual evidence and qualification limits. Live scaling-preview smoothness is deferred under OI-45.

For any upstream or fork update, pin the new commit, review the five changed paths for target ownership, commit threading, completion/failure delivery, geometry resize and disposal, regenerate lock files intentionally, and update patch checksums and package version. Build twice from fresh checkouts and compare extracted backend assemblies. Verify package/assembly identities and native dependencies before restoring tests. Rerun actual-widget normalized edge/detail, moving-window, foreground, clipping, fade, inactive, DPI and lifecycle gates, plus non-opted-in Settings/Worklog checks. Record package size and publish-size differences. Do not release while release qualification requirements remain open.
