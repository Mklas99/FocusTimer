# Proposal

## Why

The widget transparency fix is delivered, but its Off/Solid choices cannot provide visibly blurred desktop content. This change addresses the remaining frost goal of OI-25 and the Windows widget backdrop work in OI-28, using visual qualification rather than treating an accepted API call as proof.

## What Changes

- Add a new Frosted widget background choice with native desktop blur, sharp foreground controls, and independent background tint opacity.
- Keep Off clear and Solid opaque; retain immediate appearance preview and Apply/OK/Cancel behavior in both modes and both control placements.
- Preserve old settings and imported themes. Use a new serialized Frosted value so legacy Soft/Strong/Blur values continue to normalize to Off.
- Provide a solid fallback for unsupported systems, disabled transparency, High Contrast, and failed native attachment; report the requested and effective material in existing diagnostics.
- Qualify the backend on a real Windows desktop before exposing Frosted in a release. Investigate host-backdrop composition only if the existing Avalonia backend cannot meet the visual criteria.
- Measure rendering/resource costs and verify native lifetime, clipping, resizing, display scaling, and window activation.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `widget-ui`: Frosted material, independent tint, native fallback, visual qualification, and existing geometry preservation.
- `theming`: Additive Frosted serialization/import/export compatibility while preserving legacy backdrop migration.

## Impact

App widget material policy, ThemeManager, TimerWidgetWindow, Settings appearance controls and diagnostics; Core backdrop names and a platform-neutral contract if a native adapter is required; Host DI; optional Platform.Windows composition implementation with an unsupported Linux stub. Persistence uses the existing theme JSON path. Existing Settings/Worklog frost is unchanged.

No desktop capture in the rendering path, wallpaper substitution, undocumented accent APIs, framework upgrade, or blur-strength slider is planned. Native screenshots are test evidence only. Main specs will change during implementation/synchronization, not during this proposal.