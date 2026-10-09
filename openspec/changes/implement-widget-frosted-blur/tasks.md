# Tasks

## 1. Native backend qualification

- [ ] 1.1 Add an isolated real TimerWidgetWindow qualification fixture using the native-test process pattern; verify Off shows sharp desktop stripes, Solid hides them, evidence is unobscured, and moving underlying content is visible through Off.
- [ ] 1.2 Prototype Avalonia Blur/AcrylicBlur behind the existing single shell without exposing a saved Frosted choice; inspect the pinned backend, verify reduced stripe edges at matched tint, sharp foreground, rounded bounds, inactive topmost behavior and whole-widget fade, and record candidate results.
- [ ] 1.3 If task 1.2 fails, prototype documented host-backdrop composition in Platform.Windows on supported Windows 11 builds; verify Avalonia target/z-order coexistence, DPI/corner clipping and disposal. Record one passing backend, exact dependencies, supported conditions and publish-size impact in design/verification. If no backend passes, stop before group 2 and record the failed feasibility gate.

## 2. Material state and native lifecycle

- [ ] 2.1 Implement the selected App material policy with Clear/Frosted/Solid/SolidFallback and reason; if a native adapter is required, add the Core factory/session contract, Windows implementation, Host DI and unsupported Linux stub. Verify no App-to-Platform.Windows dependency and unit-test capability/attachment failure paths. Record responsibilities in ARCHITECTURE.md.
- [ ] 2.2 Connect material attach/update/detach to TimerWidgetWindow handle, bounds, DPI, activation, hide/show and close; verify lifetime tests and native repeated-cycle evidence show no obsolete visual or growing session/handle count.
- [ ] 2.3 Integrate High Contrast, transparency preference and supported session/composition checks with ThemeManager's single shell; test preference transitions and failure recovery, verify fallback never mutates stored values, and document requested/effective diagnostic meaning and limitations.
- [ ] 2.4 Verify native material and one shell tint follow corners, bounds, both placements/modes and overall opacity; run headless containment/geometry tests and native edge/foreground/fade captures at tint extremes, and document any supported-platform limits.

## 3. Theme and settings behavior

- [ ] 3.1 Add the Frosted token without changing legacy Soft/Strong/Blur normalization or factory defaults; verify clone, old-file defaults, JSON round-trip, imports including preset-name collisions, invalid token rejection, and export/reimport. Document older-build compatibility and test the Frosted-to-Off rollback conversion.
- [ ] 3.2 Add Frosted to Appearance and enable tint for Off/Frosted only; route preview through existing snapshots and commit activation, test Apply/OK/restart, failed activation compensation, Cancel/title-bar discard, and native switching without foreground blur. Update README.md and DEVELOPMENT.md with the supported behavior.
- [ ] 3.3 Update existing material diagnostics with requested/effective state and fallback reason; verify changes refresh after preview, rollback and preference transitions without diagnostics affecting recording or timer behavior.

## 4. Native release acceptance and integration

- [ ] 4.1 Run native visual acceptance across full/compact, beside/below, 0.5x/1x/3x widget and 100%/150%/200% display scales, light/dark content, High Contrast and imported palettes; verify screenshot metrics and pointer/keyboard, project input, drag, inactive topmost, display movement and hide/show. Record build/OS/GPU/renderer/preferences and user walkthrough results in verification.md; unavailable/obscured cases remain unverified.
- [ ] 4.2 Measure repeated Off/Frosted idle and dragging workloads plus 100 material/hide-show cycles; verify CPU, memory, responsiveness and native lifetime against design budgets, record GPU figures where available, and reject regressions rather than silently relaxing limits.
- [ ] 4.3 Run affected Core/Persistence/App/Windows/Host tests, focused headless placement/transaction tests and Windows Host build; run OpenSpec strict validation and git diff --check. Record any full-suite stall separately and confirm existing Settings/Worklog material remains unchanged.
- [ ] 4.4 Reconcile main widget/theming specs and current docs with delivered behavior and native evidence; close the remaining OI-25/OI-28 frost work only after release gates pass, verify no document claims universal compositor support, and prepare the change for review/archive.