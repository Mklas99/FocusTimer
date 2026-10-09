# Design

## Context

See proposal.md for motivation. TimerWidgetWindow is borderless, sizes to content, and has one WidgetShell below sharp clock/control layers. WidgetBackdropLevels requests Transparent for every mode. ThemeManager selects the tint or opaque fallback from the theme and ActualWidgetTransparency. Settings exposes Off/Solid; WidgetBlurModes normalizes Soft/Strong/Blur to Off. Theme JSON already stores material and tint. Settings/Worklog frost uses a separate DesktopWindowMaterial policy and is qualified on the development machine.

Prior widget Acrylic and DWM experiments appeared solid or unblurred. Current documentation confirms that transparency requests have platform fallbacks; it does not prove compatibility with this widget's renderer. App targets net8.0 and must not depend on Platform.Windows. The latter targets net8.0-windows and currently has no Windows composition packages.

## Goals / Non-Goals

Qualify real desktop blur behind the existing widget, preserve its single outline and sharp foreground, and retain the shared settings transaction. Deliver Windows first, with an explicit unsupported Linux path.

Do not capture the desktop for rendering, blur the widget's own visual tree, substitute wallpaper, introduce a second visible window, or change Settings/Worklog material. One fixed frost effect is sufficient; an adjustable blur radius is deferred. Preserve factory material defaults and user opacity choices.

## Decisions

### 1. Qualify the backend before exposing Frosted

Use an isolated native widget fixture at full opacity over controlled dark/light stripes and a moving underlying window. Compare Off and Frosted at identical tint settings (0%, 25%, 50%); compare Solid separately. Test active and inactive topmost states. First qualify Avalonia 11.2's Blur/AcrylicBlur with a transparent window and no opaque fill. Inspect the pinned backend source when interpreting results; current docs describe newer releases too.

Choose the simplest backend passing the same acceptance checks. If Avalonia fails, prototype documented Windows host-backdrop composition in Platform.Windows with DWMWA_USE_HOSTBACKDROPBRUSH on Windows 11 build 22000 or later. Investigate the exact compositor target/z-order integration with Avalonia's renderer before adding packages. A native effect must sit below Avalonia content without replacing Avalonia's composition target or blurring foreground. Test rounded clipping and handle recreation in this prototype.

Task 1.3 is a hard feasibility decision. Record one passing backend, exact dependencies and tested OS/renderer versions. If neither works with the single-window shell, stop this change before production UI work and record the failure. A dependency or renderer rewrite requires a revised design. Undocumented SetWindowCompositionAttribute and repeating the failed DWM acrylic experiment without changed evidence are excluded. DWM system acrylic and Mica are not proof of desktop sampling; Mica is not the required effect.

### 2. Keep native lifetime outside the domain

App owns the requested/effective material policy and window lifecycle. With Avalonia-only blur, retain the existing App window integration and avoid adding an unused adapter contract.

If host composition is selected, add a small Core contract for a factory creating a disposable per-window backdrop session, accepting a native handle and geometry/appearance values. It reports attachment/capability failures and effective state; no Avalonia types enter Core. Host registers a Platform.Windows factory and a Core unsupported Linux stub through DI. Pass the factory into App's window construction; do not use a service locator or reverse the dependency direction.

The session owns all COM/compositor resources, handles attach only after the HWND exists, and updates on resize, DPI, theme, activation and preference changes on the owning UI thread. Stop resources on hide where necessary, resume on show, release on close, and detach before a handle is replaced. Clean up partially initialized sessions on every failure. No polling or capture loop is added. Gate OS APIs by supported Windows builds.

### 3. Separate effect, tint, and foreground

Add Frosted to Appearance alongside Off and Solid. Keep the existing tint slider usable for Off/Frosted. At 0% tint Frosted still blurs; at 100% tint the selected theme fill intentionally hides the backdrop. The native backdrop supplies no second app tint; WidgetShell supplies the one tint and outline. A native backend's unavoidable base material must still pass the low-tint visibility checks.

Material rendering must fit the same bounds and scaled rounded corners in both modes and both placements. Synchronize native geometry in physical pixels from logical bounds and display scale. Do not clip foreground hit targets. Overall opacity must also fade a native visual; if the selected backend cannot do this, it fails qualification rather than silently changing the existing overall-fade contract.

Extend effective material state to distinguish Clear, Frosted, Solid, and SolidFallback with a reason. A custom native backend cannot use Avalonia ActualTransparencyLevel as its only truth. Native API success describes attachment, not visual qualification. High Contrast, OS transparency disabled, unavailable composition/remote-session support, or attachment failure selects an opaque theme shell and tears down the effect. React to observable preference changes and recheck on activation/show; retain the requested Frosted setting so it resumes when supported. Diagnostics show requested/effective material and fallback reason without blocking the timer.

### 4. Preserve appearance transactions and serialized compatibility

Add a new Frosted token. Existing Soft/Strong/Blur values remain mapped to Off, and missing fields remain Off. Do not reinterpret old files or change factory defaults. Clone, import/export and settings persistence preserve Frosted and tint. Older builds reject unknown theme imports; record this forward-compatibility limit. Rollback with Frosted files must normalize the token to Off using a compatibility step before launching an older build, preserving other theme fields.

Preview through ThemeManager and TimerWidgetViewModel's existing appearance snapshot. Apply/OK persist and activate only through SettingsCommitCoordinator; failed activation uses its compensation path. Cancel/title-bar close restores the last commit. Fallback never rewrites saved material or tint. Imported-theme identity and High Contrast behavior stay intact.

### 5. Make acceptance observable

Extend the isolated native test pattern from DesktopMaterialNativeTests to the real TimerWidgetWindow. Capture evidence only in tests. A meaningful stripe control must show sharp steps; the Frosted sample must retain background variation while reducing stripe-edge steps to at most one third of Off at the same tint. Solid must hide that variation. Use stripe widths appropriate to DPI and repeat with multiple widths to avoid aliasing. Record regions, thresholds, build, GPU, renderer, Windows version, transparency preference and DPI with captures. Obscured windows or disabled effects are unverified, never a passing frost result.

Exercise full/compact, beside/below, 0.5x/1x/3x widget scale, 100%/150%/200% display scale, tint extremes, light/dark desktop content, theme imports and High Contrast. Check crisp foreground, aligned outline/corners, no bleed beyond shell, drag, project input, keyboard, preview/save/discard, inactive topmost behavior, hide/show and display movement. Use native evidence for compositor claims and headless tests for geometry/transactions only.

Measure Off versus Frosted using repeated 60-second idle samples after warmup plus 30 seconds dragging over changing content. Record median process CPU, private bytes, GPU usage if available, and frame responsiveness. Proposed release limits are <=1 percentage point additional idle CPU, <=25 MiB additional private bytes, no new periodic app redraw while idle, and no sustained drag stalls above 100 ms. After 100 show/hide and material-toggle cycles, native handle/session counts must return to baseline with no growing allocations. Treat regressions as a failed gate; revise the plan explicitly before accepting different budgets.

## Risks / Trade-offs

- Avalonia/native target composition may conflict. Resolve this in the real-widget prototype before shipping a new choice.
- Windows may suppress effects without changing every capability signal. Use documented preference/session checks and record tested conditions; do not claim every compositor is visually guaranteed.
- Arbitrary desktop pixels and user-selected zero tint cannot guarantee text contrast. Preserve user foreground controls and opaque High Contrast/fallback behavior; review readability over light/dark content.
- Package/runtime costs may outweigh the feature. Prefer the existing backend and record publish-size/runtime effects before selecting native dependencies.
- The full headless suite previously stalled. Run affected projects and focused geometry/transaction tests; disclose broader-run failures separately from native acceptance.

## Migration Plan

1. Implement and qualify the backend behind a test-only path.
2. Add the Frosted token, policy, preview controls and diagnostics only after the feasibility gate passes.
3. Complete automated and native acceptance, update live docs/specs, and close OI-25/OI-28 only when evidence supports both.
4. Revert native rendering safely by selecting Off/Solid. Before rolling back to an older executable, migrate any Frosted token to Off without changing tint or other palette values.

## Sources

- [Avalonia Windows transparency guidance](https://github.com/avaloniaui/avalonia-docs/blob/main/docs/platform-specific-guides/windows.md), consulted through Context7; pinned-version behavior still requires qualification.
- [Microsoft DWMWINDOWATTRIBUTE](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute), host-backdrop support and Windows build requirements.
- [Microsoft composition brushes](https://learn.microsoft.com/en-us/windows/uwp/composition/composition-brushes), backdrop/effect composition concepts; this is not proof of Avalonia interoperation.