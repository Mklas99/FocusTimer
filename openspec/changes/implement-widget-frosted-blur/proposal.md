# Proposal

## Why

The widget transparency fix is delivered, but its Off/Solid choices cannot provide visibly blurred desktop content. This change addresses the remaining frost goal of OI-25 and the Windows widget backdrop work in OI-28, using visual qualification rather than treating an accepted API call as proof.

## What Changes

- Add a new Frosted widget background choice with native desktop blur, sharp foreground controls, and independent background tint opacity.
- Keep Off clear and Solid opaque; retain immediate appearance preview and Apply/OK/Cancel behavior in both modes and both control placements.
- Preserve old settings and imported themes. Use a new serialized Frosted value so legacy Soft/Strong/Blur values continue to normalize to Off.
- Provide a solid fallback for unsupported systems, disabled transparency, High Contrast, and failed native attachment; report the requested and effective material in existing diagnostics.
- Qualify the backend on a real Windows desktop before exposing Frosted in a release. Stock Avalonia 11.2 produces live blur but fails rounded clipping and overall fade. Evaluate a maintained backend extension exposing per-window backdrop clipping and opacity while retaining Avalonia's composition target.
- Measure rendering/resource costs and verify native lifetime, clipping, resizing, display scaling, and window activation.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `widget-ui`: Frosted material, independent tint, native fallback, visual qualification, and existing geometry preservation.
- `theming`: Additive Frosted serialization/import/export compatibility while preserving legacy backdrop migration.

## Impact

App widget material policy, ThemeManager, TimerWidgetWindow, Settings appearance controls and diagnostics; Core backdrop names; a narrow Avalonia Windows backend extension and reproducible dependency build/package integration if it passes qualification. Avalonia retains native resource ownership. Persistence uses the existing theme JSON path. Existing Settings/Worklog frost is unchanged, and unsupported platforms use the solid fallback.

The extension may require a maintained fork pinned to an immutable upstream revision with a reviewable patch and aligned Avalonia package versions. Its maintenance, license notices, upgrade checks and clean-build reproducibility are part of the feasibility gate. An independent Platform.Windows composition adapter remains a possible alternative only after a separate target-coexistence design is reviewed.

No desktop capture in the rendering path, wallpaper substitution, undocumented accent APIs, framework upgrade, or blur-strength slider is planned. Native screenshots are test evidence only. Main specs will change during implementation/synchronization, not during this proposal.
