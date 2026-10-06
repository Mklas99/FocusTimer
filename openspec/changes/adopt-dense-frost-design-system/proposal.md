# Proposal

## Why

FocusTimer's Settings and Worklog screens have oversized navigation, uneven spacing, and inconsistent control chrome despite an existing shared token system. The screenshot review and the user's selected alternatives now provide a concrete visual direction for the remaining OI-21/OI-24 polish and the Windows material work related to OI-28.

## What Changes

| Area | Selected direction | Result |
|---|---|---|
| Material | Dense frost | A restrained blurred outer shell with a near-solid tint, fine edge, and soft depth; sharp foreground content and an accessible solid fallback. |
| Layout | Compact rows, with grouped cards where useful | Aligned label/control rows by default. Cards are used only for two or more meaningful peer groups, with compact rows inside them. |
| Typography | Native clarity | Segoe UI on Windows, a clear page/section/body/helper hierarchy, and stable numeric alignment. |
| Navigation | Uploaded icon-and-underline reference | Icons beside labels, a thin shared baseline, and an underline under the active tab. Settings order is General, Logging, Appearance, Hotkeys, About. |
| Controls | Soft rounded | Shared, moderately rounded buttons, fields, dropdowns, numeric inputs, checkboxes/toggles, sliders, and disclosure rows. |

- Apply this language to Settings, the Worklog window, and their app-owned editors through shared resources. Keep dense tables and timeline content on readable near-solid surfaces.
- Fix layout defects affected by the refresh, including the clipped opacity label, missing persistent rule-field labels, and crowded controls at the existing minimum window sizes.
- Keep the existing OK, Apply, Cancel, draft, preview, commit locking, failure, and recovery behavior. Restyle those actions; do not replace the commit model.
- Verify native Windows frost before claiming it works. A browser mockup or a successful compositor API call is insufficient evidence. Preserve the solid fallback when effects are unavailable.
- Keep theme colors and theme-file values separate from this change. No palette redesign, new theme presets, or settings/theme schema migration is planned.
- Preserve the timer widget's current Full/Compact layout, typography, Off/Solid choices, foreground-opacity settings, and tray behavior. Only spacing changed, on request: a gap and vertical centering for the full-mode project field, and tighter compact-mode buttons whose click target spans the row height. Its native blur investigation remains separately tracked in OI-28.

The visual scope is the five selected improvements. Widget typography/icon redesign and worklog table, summary, or timeline redesign from ranks 6–7 are separate follow-ups. Worklog controls, navigation, fonts, spacing, and window material still adopt the shared language.

## Capabilities

### New Capabilities

None. Reuse the existing design-system, Settings, and theming boundaries.

### Modified Capabilities

- `ui-design-system`: dense-frost configuration/reporting shells with verified fallback, native typography, compact row and multi-card rules, icon-and-underline navigation, and soft-rounded control geometry.
- `settings`: the five-tab icon-and-underline shell, preserved editable control inventory, scannable Appearance layout, and persistent save/feedback footer at the supported window sizes.
- `theming`: preserve the existing tab palette roles while rendering selected state as an underline instead of a large filled tab, with the selected label and icon in the contrast-adjusted accent because built-in `TabSelectedText` values target the old fill; keep palette and serialized theme values unchanged.

## Impact

Primary ownership is `FocusTimer.App`: `Styles/Tokens.axaml`, `Styles/ControlStyles.axaml`, `Styles/ThemeResources.axaml`, `Services/ThemeManager.cs`, Settings/Worklog views, rule editors, and app-owned dialogs. The implementation must preserve the existing MVVM commands and style boundaries around widget controls.

Start material verification with the pinned Avalonia 11.2.0 APIs. If those cannot demonstrate the chosen effect, an isolated Windows adapter may require `FocusTimer.Platform.Windows`, a minimal platform-neutral contract/stub, and Host DI wiring. Do not introduce an App-to-Windows dependency, desktop capture, a new UI theme package, or a framework upgrade as part of the visual refresh.

Related backlog: [OI-21, OI-24, OI-25, OI-28](../../../docs/versions/current/OpenIssues.md), [Settings redesign decisions](../../../docs/versions/current/OI-21-SettingsRedesign.md). OI-25/widget transparency and OI-28/widget-native blur are not closed by a Settings/Worklog material implementation.

The in-progress `worklog-data-management` change already moved Summary out of Settings. This proposal uses that implemented five-page baseline and does not repeat the reporting move or alter its remaining manual verification.

## Visual references

The combined compositions show the selected styles together. They use an expanded height to expose the groups; the implemented default window will scroll content while keeping navigation and actions reachable.

![Proposed General composition: dense frost and compact rows](references/selected/combined-general.png)

![Proposed Appearance composition: compact rows inside two peer cards](references/selected/combined-appearance.png)

The uploaded navigation image is authoritative for the navigation structure, not for its theme colors.

![User-selected icon-and-underline navigation](references/navigation-user-reference.jpg)

The [reference gallery](references/README.md) includes the ten original screenshots, snapshots of the selected alternatives, and combined General/Appearance mockups. Mockups illustrate the target composition; they are not evidence of native Avalonia blur or completed application changes.
