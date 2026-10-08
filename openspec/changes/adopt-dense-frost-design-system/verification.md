# Verification notes

Recorded while applying the change. Everything here was measured on one Windows 11 development machine (Avalonia 11.2.0, .NET 10 SDK building `net8.0`); nothing is inferred from a browser mockup.

## Initial dense-frost material evidence (tasks 1.1-1.4)

| Question | Result |
|---|---|
| Backend tried | Avalonia 11.2.0 `TransparencyLevelHint = [AcrylicBlur, None]`; no `Platform.Windows` adapter |
| Requested / actual level | Requested `AcrylicBlur/None`, actual `AcrylicBlur` (the API call succeeded) |
| Windows transparency effects | **Off** (`HKCU\...\Themes\Personalize\EnableTransparency = 0`) |
| Material applied | Solid fallback, as designed: the policy treats the preference as an override because Windows draws acrylic as a flat color while it is off |
| Frost visually verified | **No.** The change could not be observed on this machine, and a system setting is not changed from here |
| Solid fallback | Verified natively: over a stripe pattern the shell shows standard deviation 0.00 and no pixel step ([evidence](evidence/material/material-qualification.txt), [capture](evidence/material/material-solid.png)) |
| Control: transparent, unblurred shell | Shows sharp stripes (largest adjacent-pixel step 9), so the measurement can tell blur from plain transparency |

`DesktopMaterialNativeTests` is the acceptance check. With transparency effects on it requires the frost shell to let the backdrop bleed through (standard deviation above 0.3) while its largest pixel step stays at most a third of the unblurred control's. Until it completes a valid unobscured capture with effects on, tasks 1.1, 1.2, and 1.4 stay open and the `Platform.Windows` adapter decision (1.2) is undecided. Later notes record transparency effects enabled but an obscured test window. To close them, confirm effects are on, clear covering windows from the desktop, then run:

```powershell
$env:FOCUSTIMER_NATIVE_APPEARANCE_TESTS = "1"
$env:FOCUSTIMER_EVIDENCE_DIR = "$PWD\openspec\changes\adopt-dense-frost-design-system\evidence\material"
dotnet test tests/FocusTimer.App.Tests --filter "FullyQualifiedName~DesktopMaterialNative"
```

If it reports `NOT VERIFIED` again, or fails on the blur assertions, Avalonia's backend cannot produce visible frost here and the isolated Windows adapter of task 1.2 applies.

Fallback transitions (unit-tested in `DesktopMaterialTests`): a reported blur level gives frost; `None`, `Transparent`, `Mica`, High Contrast theme, system high-contrast preference, and transparency-effects-off each give solid; toggling any of them back restores frost. A window's layout is identical in both states because only its background brush changes.

## Layout at supported sizes (tasks 4.x, 5.1)

`DesktopShellEvidenceTests` renders every Settings page (640x540, 500x400, plus 500 and 760 wide full-height) and every Worklog page (900x620, 640x560) with the real Windows renderer for all seven built-in themes, and asserts: OK/Apply/Cancel stay fully inside the window, the five tab labels fit at 500 (no horizontal tab scrolling), and no page content passes the scroll content's right edge. Captures are in [evidence/before-after](evidence/before-after) (left: commit `e139e61`, right: this change, Dark theme) and [evidence/themes](evidence/themes).

Binding inventory: comparing `{Binding ...}` expressions, commands, `Click` handlers, and color-swatch tags in the old and new `SettingsWindow.axaml` finds no removed entries; the only additions are the two palette-group header strings.

## Behavior and regression suites

| Suite | Result |
|---|---|
| `FocusTimer.App.Tests` | 391 passed, 3 native tests skipped by default |
| `FocusTimer.App.HeadlessTests` | all passed, including new tab, control-state, rule-label, palette-disclosure, and widget-leak tests |
| Native `AppearanceNativeTests` (delayed save, IME/paste/input shielding, failure and recovery, focus restoration, 3 presets) | passed. It was **already failing on the base commit** (it addressed a tab index that no longer held the controls it needs); it now targets Appearance and expands the palette groups |
| Native `DesktopShellEvidence`, `DesktopMaterialNative` | passed (frost branch reports not verified, see above) |
| `FocusTimer.Core.Tests`, `Persistence.Tests`, `Platform.Windows.Tests` | passed |
| `FocusTimer.Host.Tests` | **not run**: running `FocusTimer.Host` processes lock the Host output files, and they were left alone. `FocusTimer.Host` itself compiles with no warnings into a separate output folder |
| `dotnet format` (whitespace, style) on the changed files | clean |
| `openspec validate adopt-dense-frost-design-system --strict`, and `worklog-data-management` | valid |

## Selected tab label and theme compatibility (tasks 3.x)

Built-in `TabSelectedText` values target text on the old filled tab (near-black on Monokai orange, white on Light blue), so on the plain tab surface six of the seven built-ins were unreadable. After the decision to use the accent, the selected label and icon use `AccentPrimary`, mixed toward white or black only when it is below 4.5:1 on the tab background (`ThemeContrast.EnsureContrast`). Tabs are drawn without a fill on the window background, so the ratio is taken against the Settings background (checked for every built-in by `SettingsThemeColorTests`). `TabSelectedText` still colors the selected Worklog row. No palette value, serialized field, or theme-file schema changed; `SettingsThemeColorTests` checks the derived ratio for every built-in, and a headless test confirms a `TabSelectedBackground` different from the accent is rendered as the underline, updates live, and round-trips through JSON.

## Review round 3 (tasks 7.x)

Changes made after the first on-screen review, each checked on screen or in a test:

- Option indent, side insets, and flush theme row: headless layout tests (`Options_AreIndentedUnderTheirHeader...`, `EveryPage_HasTheSameInsetOnBothSides`) and renders. The indent rule had lost to the checkbox margin because later rules of equal kind win; it now follows the checkbox styles.
- Accordion: Fluent applies a special header template to any toggle named `ExpanderHeader` (black fill and a second chevron). The new template names its toggle `AccordionHeader`; a headless test asserts one toggle and one rotating chevron.
- Calendar popup: captured on screen with `DesktopPopupEvidenceTests` (popups are separate native windows). Fluent fills the calendar grid layer with `BorderBrush`, so that is now the surface color; fonts, header and day buttons use desktop sizes. Activator selectors (pseudo-classes, `:not`) outrank plain ones regardless of order, which is why the calendar button rules carry `:not(.cal)`. Days after today (and before the retention window) stay hidden: Fluent sets their opacity inside the control template, which application styles cannot override.
- Success, Warning, and Danger color editors are removed; the theme values remain for per-theme tuning in code.
- Blank rule rows: warning color and removal on apply; partly filled rows: danger color and a blocked commit (`RuleListViewModelTests`, `ExclusionRuleEditorTests`, headless row-class test).
- The screen captures include whatever else is on screen behind a window, so the popup captures are not stored in the repository.

## Review round 4 (tasks 8.x)

Changes made after the second on-screen review, each checked on screen (native Skia renders and a captured calendar popup) or in a headless test (`DesktopReviewRound4Tests`):

- Worklog: one icon-only Refresh button at the right end of the day row replaces the three per-tab buttons and stays at the same place on every tab; the day arrows are icons; the Timeline zoom hint is a hover icon; the Summary tab no longer repeats the day; the Entries table header is semibold on its own band.
- Calendar: the date picker's button uses the app's calendar icon (a style replaces the button template, since Fluent draws the glyph from several shapes), the month arrows are scaled down, and the ring around today is rounded.
- Settings: the sliders sit closer together, "Overall fade" is now "Overall opacity" because a fuller bar means a more opaque widget (the stored value was already opacity; only the wording was inverted), and the helper text above the diagnostics became hover icons next to the Theme and Colors headers.
- Rule actions (Up, Down, Remove) are borderless with smaller label-colored text and a 28 px click target; the state set is repeated because the shared button states outrank plain selectors.
- Color picker: Hex, R, G, B, H, S, and V use the same label class and spacing; the R, G, and B fields match their labels' width.
- Widget (spacing only): the project field has an 8 px gap to its label and centered text; compact buttons touch, the icons are 20 percent smaller than full mode, and the click target is the whole row height (24 by 37 px at scale 1 instead of 24 by 24; the window is 37 px high instead of about 45).
- Backlog: OI-35 records the option to place the widget buttons beside or below the clock.
- Evidence: `evidence/widget/` holds the full and compact widget renders with their measured sizes (`DesktopWidgetEvidenceTests`). The before/after and theme images are regenerated. The frost qualification test (`DesktopMaterialNativeTests`) could not give a result in this round: Windows transparency effects are now on, but another window covered the test window, so the sampled pixels were not the window. Rerun it with the desktop clear.

## Worklog boundaries (task 5.3)

Only chrome changed in Worklog: toolbar, tab headers, control geometry, and fonts. The Entries table, Timeline panel, Summary rows, columns, grouping, zoom, F5 refresh, editing, and data-management behavior are unchanged, and their existing suites pass.

## Not verified

- Dense frost itself (above).
- Windows display scales 100, 125, 150, and 200 percent: the display scale is a system setting and was not changed. Layout assertions use logical pixels at the machine's current scale.
- Pointer and keyboard walkthroughs of the Worklog entry form and the color picker on a real display; the headless `WorklogEntryFormTests` and the new headless keyboard test pass.
- An imported theme through the full Apply/OK/Cancel/failure/recovery cycle natively; imported-theme round trips are covered by unit tests.
- The F-03 manual walkthrough, OI-25, and OI-28 keep their separate status; this change does not close them.

## Review round 5 (compact widget)

- Compact buttons have no fill in any state and their click areas reach 4 px over the clock; the clock width (124 px at scale 1) now fits 28 pt digits, so nothing is hidden under the buttons.
- Side space is equal: measured 12 px left and 12 px right of the visible content at scale 1 (11 and 13 at 1.5). Stacked buttons (scale 1.25 and above) are centered vertically against the clock (9 px above, 8 px below at 1.5). Renders are in `evidence/widget/`.
- Your manual size changes were kept (control height 25 px, clock sizes 38/28, hit-target floor 20, header "Widget colors", icon margins); tests and docs now match them.
## Widget correction, 2026-10-06

The earlier compact-row captures below are superseded by the requested vertical button column at every scale. Native checks passed at 0.5x, 0.75x, 1x, 1.5x, 2x, and 3x, with balanced side gaps and one outline. The shell corners shrink below 1x and stay at 12 px above it. Evidence is in `artifacts/ui-fixes/widget-correction/`; 54 related automated tests passed.

The follow-up icon-gap correction removes compact-button padding and aligns the icon bounds at their shared edge while retaining separate 20 px minimum click targets. Native checks passed at the same scales, plus twelve headless checks; captures are in `artifacts/ui-fixes/widget-icon-gap/`. The reported mismatched corners came from an older IDE launch output: its App DLL initially differed from the current test build. All five FocusTimer launch assemblies now match the successful separate Host build by SHA-256. Normal rebuild attempts encountered running-app locks.

The spacing-scale follow-up scales full button margins, compact button side padding, and outer insets. Compact minimum-width click targets can extend over the clock while keeping the icon gap proportional and both visible side gaps equal. Native checks passed in Dark and High Contrast at 0.5, 0.6, 0.7, 0.8, 0.9, 1, 1.1, 1.2, 1.3, 1.5, 2, 2.5, and 3; 54 related automated tests and a separate Windows Host build passed. The native capture gallery with red scale labels is `artifacts/ui-fixes/widget-spacing-scale/comparison.html`. Updating the normal launch output for this follow-up remains pending while the running app locks its files.

The proportions correction supersedes the fixed widget click-target floors described above. Full controls and compact rows now scale with the clock; fractional layout avoids accumulating rounded margins. Windows' caption-size validation is bypassed for this borderless, non-resizable window, so its height follows its content. Native checks passed in Dark and High Contrast at all 13 scales above, including live size and mode changes; 41 behavior/theme tests and 25 headless layout/navigation tests passed. Both normal and win-x64 IDE Host outputs rebuilt with no warnings or errors. Evidence: `artifacts/ui-fixes/widget-proportions/comparison.html` and its `widget-sizes.txt` files. Native sizing rationale: [WM_WINDOWPOSCHANGING](https://learn.microsoft.com/en-us/windows/win32/winmsg/wm-windowposchanging).

The corner-radius follow-up supersedes the earlier 12 px cap. Both modes now scale continuously: compact uses the 8 px radius token at 1x, full uses 12 px. Native checks passed at all 13 scales in Dark and High Contrast, including mode changes and live scale preview; 41 widget behavior/theme tests passed. Both IDE Host builds passed without warnings or errors. Evidence: `artifacts/ui-fixes/widget-corners/comparison.html`.

The native-frame correction disables extended Windows decoration on TimerWidgetWindow. That decoration had an independent corner radius and was absent from the earlier app-content captures. The widget keeps its single scaled shell outline. Native checks now verify both the requested decoration setting and the actual native extended-decoration state at all 13 scales, alongside size and mode changes. The separate Host build and the IDE win-x64 launch build passed without warnings or errors after the debugger stopped. App-content captures remain in `artifacts/ui-fixes/widget-native-frame/`; these captures do not include Windows compositor decoration.

## OI-42 action hierarchy

Implemented shared desktop roles for primary commit, neutral Cancel, and destructive Worklog Delete buttons. The supplied delete-dialog reference informed the muted danger fill. Labels, order, commands, targets, and the existing confirmation remain unchanged; semibold labels and outlines accompany color differences. ThemeManager derives action colors without changing stored palette values.

Validation on 2026-10-08:

- Windows Host `win-x64` build passed with zero warnings and errors.
- Fourteen focused headless control-state and Worklog form tests passed. Action focus remains visible while hovered or pressed; disabled actions return to neutral styling without resizing.
- Two palette tests passed, covering all seven built-in themes and a low-contrast import. Enabled action text reaches 4.5:1 and action focus reaches 3:1 against the tested surfaces; imported palette values remain unchanged.
- Native Windows render/layout test passed in Dark, Light, and High Contrast at default and minimum window sizes. Reviewed Settings footer, Worklog Save/Cancel and Delete confirmation, and color-picker captures in `artifacts/oi-42/`.
- Changed C# files passed `dotnet format --verify-no-changes`; the OpenSpec change passed strict validation; `git diff --check` passed.

The full native pointer/keyboard walkthrough across themes remains open. Native capture evidence verifies rendering and layout, while interaction-state and form behavior evidence comes from headless tests.

## Notification dialog styling

On 2026-10-08, adapted notification chrome to the supplied rounded-dialog reference. Cards use a 420 px logical width, 20 px corner radius and padding, a semibold heading, muted contrast-adjusted body text, a borderless 24 px close target, and the shared primary acknowledgement button. Informational outlines are subtle; High Contrast retains a 3:1 outline. Warning/error headings and thin outlines retain their readable severity colors and explicit labels. All chrome now lives in the shared styles.

Fifteen focused headless notification and control tests passed, including severity/timeouts, dismissal, acknowledgement, scrollable long messages, and live theme changes across all seven built-in themes. The native Windows render/layout test passed for Dark, Light, and High Contrast; reviewed information, acknowledgement, and severity captures in `artifacts/notification-dialog-style/`. The Windows Host win-x64 build passed with zero warnings or errors. Timing, activation, placement, and acknowledgement behavior were preserved.
