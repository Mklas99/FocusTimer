# Design

## Context

See proposal.md for motivation. TimerWidgetWindow is borderless, sizes to content, and has one WidgetShell below sharp clock/control layers. WidgetBackdropLevels requests Transparent for every mode. ThemeManager selects the tint or opaque fallback from the theme and ActualWidgetTransparency. Settings exposes Off/Solid; WidgetBlurModes normalizes Soft/Strong/Blur to Off. Theme JSON already stores material and tint. Settings/Worklog frost uses a separate DesktopWindowMaterial policy and is qualified on the development machine.

Prior widget Acrylic and DWM experiments appeared solid or unblurred. Current documentation confirms that transparency requests have platform fallbacks; it does not prove compatibility with this widget's renderer. App targets net8.0 and must not depend on Platform.Windows. The latter targets net8.0-windows and currently has no Windows composition packages.

The native fixture now proves that Avalonia 11.2 AcrylicBlur samples changing windows behind the actual widget. It also proves that the stock effect extends outside the rounded shell and remains visible at overall opacity zero. See verification.md for measurements and renderer details. On 2026-10-10 the user approved revising the plan to evaluate a maintained backend extension for per-window clipping and opacity.

## Goals / Non-Goals

Qualify real desktop blur behind the existing widget, preserve its single outline and sharp foreground, and retain the shared settings transaction. Deliver Windows first, with an explicit unsupported Linux path.

Do not capture the desktop for rendering, blur the widget's own visual tree, substitute wallpaper, introduce a second visible window, or change Settings/Worklog material. One fixed frost effect is sufficient; an adjustable blur radius is deferred. Preserve factory material defaults and user opacity choices.

## Decisions

### 1. Qualify the backend before exposing Frosted

Use an isolated native widget fixture at full opacity over controlled dark/light stripes and a moving underlying window. Compare Off and Frosted at identical tint settings (0%, 25%, 50%); compare Solid separately. Test active and inactive topmost states. First qualify Avalonia 11.2's Blur/AcrylicBlur with a transparent window and no opaque fill. Inspect the pinned backend source when interpreting results; current docs describe newer releases too.

Evaluate a narrow extension of the pinned Avalonia Windows backend next. Preserve Avalonia's existing HWND target, root container, foreground surface and host-backdrop brush. Add a public, versioned per-window capability for backdrop enablement, rounded clipping and opacity; choose and record its concrete API during the prototype. It must report unsupported or unavailable native state instead of relying on reflection into private visuals. Do not attach a competing composition target, replace the renderer, or change the default behavior of windows that do not opt in.

The extension updates only the backdrop visual beneath the foreground. Convert the widget shell's logical bounds and radius to physical pixels once using the current display scale, and apply the same rounded boundary after mode, placement, resize and DPI changes. Pass overall opacity separately from shell tint; zero opacity must leave no native blur, and intermediate values must fade the effect with the widget. Marshal native updates through Avalonia's composition owner and synchronization mechanism; do not mutate COM visuals from an arbitrary UI callback. Reapply the latest state when the surface is created or recreated, reject updates for obsolete handles, and retain Avalonia's disposal ownership. Settings/Worklog must retain their existing backdrop behavior.

A maintained source patch or fork is permitted for this extension, based initially on Avalonia 11.2.0. Pin the upstream revision, patch revision, dependency versions and package provenance. Keep affected Avalonia packages aligned; do not replace a DLL in the local package cache or rely on a machine-specific binary. Record a reproducible clean restore/build/package procedure, license notices, publish-size impact and the minimal backend diff. FocusTimer must retain the patch and upgrade regression checks for any fork it ships; prefer an upstreamable extension and document the route back to unmodified upstream packages. A broad framework upgrade or renderer rewrite remains outside this revision.

Prototype the extension behind the existing test-only path before selecting it. Repeat the same edge/detail, contrast-matched opacity control, live tracking, foreground, rounded-boundary and whole-widget-fade checks, including handle recreation and disposal. An independent documented Windows host-backdrop adapter is a secondary option only after a reviewed coexistence design establishes how it can sit below Avalonia content without replacing the existing target. No such integration has yet been qualified.

Task 1.3 is a hard feasibility decision. Record one passing backend, exact dependencies, maintenance/build evidence and tested OS/renderer versions. If the extension cannot satisfy the single-window contract or cannot be maintained reproducibly, stop before production UI work and record the failure. This revision authorizes the narrow backend extension and its necessary aligned dependency builds. Other dependency strategies, a renderer rewrite or an independent target-ownership design require another revision. Undocumented SetWindowCompositionAttribute and repeating the failed DWM acrylic experiment without changed evidence are excluded. DWM system acrylic and Mica are not proof of desktop sampling; Mica is not the required effect.

The selected App backend uses `Avalonia.Win32.Win32BackdropControllerV1.TryCreate(hwnd, out controller)`, `TryUpdate`, `TryUpdateWithCompletion(radiusInPhysicalPixels, overallOpacity, enabled, Action<bool>)`, `IsApplied` and `Dispose`. App pins fork package `11.2.0.5-focustimer.5`, with other Avalonia dependencies at 11.2.0. The additive completion API resolves the reporting gap found during integration: accepted work is pending until the composition thread applies it or fails, then posts the result to the UI thread. App's callback captures handle/session/update generation and rejects obsolete results. Surface disposal reports failure; disposed controllers and invalid handles suppress callbacks. Coalesced older updates may be skipped. App paints an opaque fallback while pending or failed and retries on activation/show or observed preference changes, without polling. The backend retains all target and resource ownership. `IsApplied` and completion are status, never proof of visible blur.

The revision was authorized by the user's request to continue after the reporting gap was identified. The original package `.4` qualified the backend at 100%/150% and cross-monitor DPI transitions; `.5` adds the notification and is requalified with reproducible builds and native integration evidence in verification.md. This qualifies App integration, not release acceptance. Groups 2-4 are complete as of 2026-10-12, combining recorded native/resource measurements with the user-confirmed final interaction review. Qualification remains limited to the recorded Windows/WinUI setup; live scaling-preview smoothness is deferred under OI-45.

### 2. Keep native lifetime outside the domain

App owns the requested/effective material policy and window lifecycle. With the extended Avalonia backend, retain App's existing window integration and wrap the public per-window capability there. Avalonia owns native composition resources; Core receives no Avalonia or COM types. Check capability at runtime and use the existing unsupported/failure policy when the extension is absent. Avoid adding an unused Core native-session factory or a Platform.Windows adapter.

If a later reviewed design selects independent host composition, add a small Core contract for a factory creating a disposable per-window backdrop session, accepting a native handle and geometry/appearance values. It reports attachment/capability failures and effective state; no Avalonia types enter Core. Host registers a Platform.Windows factory and a Core unsupported Linux stub through DI. Pass the factory into App's window construction; do not use a service locator or reverse the dependency direction.

The session owns all COM/compositor resources, handles attach only after the HWND exists, and updates on resize, DPI, theme, activation and preference changes on the owning UI thread. Stop resources on hide where necessary, resume on show, release on close, and detach before a handle is replaced. Clean up partially initialized sessions on every failure. No polling or capture loop is added. Gate OS APIs by supported Windows builds.

### 3. Separate effect, tint, and foreground

Add Frosted to Appearance alongside Off and Solid. Keep the existing tint slider usable for Off/Frosted. At 0% tint Frosted still blurs; at 100% tint the selected theme fill intentionally hides the backdrop. The native backdrop supplies no second app tint; WidgetShell supplies the one tint and outline. A native backend's unavoidable base material must still pass the low-tint visibility checks.

Material rendering must fit the same bounds and scaled rounded corners in both modes and both placements. Synchronize native geometry in physical pixels from logical bounds and display scale. Do not clip foreground hit targets. Overall opacity must also fade a native visual; if the selected backend cannot do this, it fails qualification rather than silently changing the existing overall-fade contract.

Extend effective material state to distinguish Clear, Frosted, Solid, and SolidFallback with a reason. A custom native backend cannot use Avalonia ActualTransparencyLevel as its only truth. Native API success describes attachment, not visual qualification. High Contrast, OS transparency disabled, unavailable composition/remote-session support, or attachment failure selects an opaque theme shell and tears down the effect. React to observable preference changes and recheck on activation/show; retain the requested Frosted setting so it resumes when supported. Diagnostics show requested/effective material and fallback reason without blocking the timer.

### 4. Preserve appearance transactions and serialized compatibility

Add a new Frosted token. Existing Soft/Strong/Blur values remain mapped to Off, and missing fields remain Off. Do not reinterpret old files or change factory defaults. Clone, import/export and settings persistence preserve Frosted and tint. Older builds reject unknown theme imports; record this forward-compatibility limit. Rollback with Frosted files must normalize the token to Off using a compatibility step before launching an older build, preserving other theme fields.

Preview through ThemeManager and TimerWidgetViewModel's existing appearance snapshot. Apply/OK persist and activate only through SettingsCommitCoordinator. A failed write retains the appearance draft preview; a later activation failure with successful compensation restores saved and nonappearance active values, then reapplies the retained appearance draft preview. Settings stays open with an error and the commit restore point does not advance. Failed compensation follows Settings recovery-required behavior without claiming which values are active. Cancel/title-bar close restores the last successful commit. Material capability or attachment failure selects a solid fallback independently of commit failure; fallback never rewrites the draft or saved material or tint. Imported-theme identity and High Contrast behavior stay intact.

### 5. Make acceptance observable

Extend the isolated native test pattern from DesktopMaterialNativeTests to the real TimerWidgetWindow. Capture evidence only in tests. Compare Off and Frosted at identical tint (0%, 25%, 50%) and full overall opacity, with Solid as a separate control. Use foreground-free shell regions over broad black/white steps at multiple widths scaled to display DPI. Average scanlines and normalize each edge profile between its local dark and light plateaus before measuring its 10%-90% transition width. Require the median Frosted transition to be at least twice Off's width and at least two physical pixels wider, over at least four valid edges per sample. Reduced raw edge contrast alone does not pass; an opacity-only control with matched residual contrast must fail this edge-width criterion.

Reject profiles with insufficient visible detail before normalization. At every qualification tint, broad-pattern plateau separation must be at least 10 levels on an 8-bit luminance scale and at least 10% of the matched Off separation. Solid must have less than 2 levels of separation. Use broad stripes that leave measurable plateaus outside the softened edge; record widths and plateau regions so blur overlap or aliasing cannot manufacture a result.

Keep the widget stationary and move a separate underlying pattern window by a known offset, then replace its black/white content with distinct light/dark color blocks. Capture before and after settling, including a return to the original position/content. Require Frosted's normalized pattern displacement to track the known offset within two physical pixels, and its broad color-block change to retain at least 10% of the matched Off change and at least 10 luminance levels. The return capture must reproduce the initial pattern within capture noise. Exercise active and inactive topmost states. A static wallpaper sample, stale image, or flat material cannot pass these checks.

Record regions, profiles, thresholds, build, GPU, renderer, Windows version, transparency preference and DPI with captures. Obscured windows, disabled effects, insufficient control detail or unmeasurable edges are unverified, never a passing frost result. Keep foreground sharpness and geometry checks separate from background metrics.

Exercise full/compact, beside/below, 0.5x/1x/3x widget scale, 100%/150%/200% display scale, tint extremes, light/dark desktop content, theme imports and High Contrast. Check crisp foreground, aligned outline/corners, no bleed beyond shell, drag, project input, keyboard, preview/save/discard, inactive topmost behavior, hide/show and display movement. Use native evidence for compositor claims and headless tests for geometry/transactions only.

Measure Off versus Frosted using repeated 60-second idle samples after warmup plus 30 seconds dragging over changing content. Record median process CPU, private bytes, GPU usage if available, and frame responsiveness. Proposed release limits are <=1 percentage point additional idle CPU, <=25 MiB additional private bytes, no new periodic app redraw while idle, and no sustained drag stalls above 100 ms. After 100 show/hide and material-toggle cycles, native handle/session counts must return to baseline with no growing allocations. Treat regressions as a failed gate; revise the plan explicitly before accepting different budgets.

## Risks / Trade-offs

- The stock backend fails clipping/fade, and the extension may not fix both without a larger renderer change. Keep the existing target and prove the fixes on the real widget; stop if that boundary cannot hold.
- A backend fork creates maintenance and upgrade work. Require a minimal patch, immutable provenance, aligned packages and reproducible builds before selecting it.
- Windows may suppress effects without changing every capability signal. Use documented preference/session checks and record tested conditions; do not claim every compositor is visually guaranteed.
- Arbitrary desktop pixels and user-selected zero tint cannot guarantee text contrast. Preserve user foreground controls and opaque High Contrast/fallback behavior; review readability over light/dark content.
- Package/runtime costs may outweigh the feature. Prefer the existing backend and record publish-size/runtime effects before selecting native dependencies.
- The full headless suite previously stalled. Run affected projects and focused geometry/transaction tests; disclose broader-run failures separately from native acceptance.

## Migration Plan

1. Establish the reproducible backend extension build and qualify it behind a test-only path, preserving the recorded stock-backend failure as a regression control.
2. Add the Frosted token, policy, preview controls and diagnostics only after the feasibility gate passes.
3. Complete automated and native acceptance, update live docs/specs, and close OI-25/OI-28 only when evidence supports both.
4. Revert native rendering safely by selecting Off/Solid. Before rolling back to an older executable, migrate any Frosted token to Off without changing tint or other palette values.

## Sources

- [Avalonia Windows transparency guidance](https://github.com/avaloniaui/avalonia-docs/blob/main/docs/platform-specific-guides/windows.md), consulted through Context7; pinned-version behavior still requires qualification.
- [Microsoft DWMWINDOWATTRIBUTE](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute), host-backdrop support and Windows build requirements.
- [Microsoft composition brushes](https://learn.microsoft.com/en-us/windows/uwp/composition/composition-brushes), backdrop/effect composition concepts; this is not proof of Avalonia interoperation.
