# FocusTimer screenshot UX review

Reviewed all 10 extracted screenshots individually. Theme colors are excluded. This review covers visible layout, typography, controls, information hierarchy, and the requested glass treatment. Hover states, keyboard behavior, hit areas, validation, and responsiveness cannot be confirmed from screenshots; recommendations about those behaviors are proposals.

The first fixes should be clipped timeline text, unlabeled project-rule fields, and the clipped opacity label. Then standardize the shared controls and spacing before adding glass effects.

Priority: **P1** = obstructs reading or understanding; **P2** = improves hierarchy or usability; **P3** = visual polish.

**Proposed visual baseline**

Use a restrained frosted outer surface, backdrop blur where supported, a fine edge highlight, and a soft shadow. Keep text and icons opaque. Put dense tables and forms on sufficiently solid inner surfaces so background detail does not compete with content. Avoid applying independent glass effects to every row or button.

Starting dimensions below are logical desktop pixels/DIPs, not measurements inferred from screenshot pixels. Adjust them after checking actual display scaling.

| Design element | Proposed baseline |
|---|---|
| Spacing | A 4/8-DIP spacing scale; 20–24-DIP outer padding; 24 DIP between sections. |
| Typography | 13–14-DIP body text, 12-DIP helper text, 18–22-DIP section titles. Use regular/medium weights and tabular numerals for time. |
| Controls | Consistent 32–36-DIP input/button height; 32–40-DIP icon-button hit areas; labels aligned to a shared grid. |
| Shape | Approximately 12–16-DIP outer corners, 8-DIP control corners, and fine 1-DIP borders. |
| Glass | Blur the backdrop, keep foreground content crisp, and provide a solid fallback. Avoid using whole-window fading as the main glass effect. |

**Shared UI in the settings and worklog screens**

These findings apply wherever the element repeats. Screen-specific tables below cover the remaining controls.

| UI element | Visible issue | Proposed fix | Priority |
|---|---|---|---|
| Window/content surface | Broad flat surfaces have little visible depth or grouping. | Use one restrained frosted shell with a fine border and soft shadow; use section surfaces only where they clarify groups. | P3 |
| Settings/worklog tabs | Large, light-weight labels and rectangular selected blocks dominate the content. | Reduce navigation typography and padding; use a compact rounded tab strip with a subtle inset selection shape. Keep selected state identifiable through shape/weight. | P2 |
| Settings tab order | About sits between Appearance and Hotkeys. | Keep configuration tabs together and move About to the end. | P2 |
| Content margins and widths | Large unused areas coexist with crowded controls and long lines. | Use consistent padding and constrain form line lengths; let tables use the available width deliberately. | P2 |
| Section headings and spacing | Strong gaps between headings and controls weaken grouping. | Keep headings close to their first row and use consistent section spacing. | P2 |
| Buttons and input fields | Similar actions use different widths, corner treatments, and visual weight. | Define one control family; distinguish primary, secondary, and destructive actions through structure, outline, and weight. | P2 |
| Settings footer: OK, Apply, Cancel | Three equally styled actions require explanation; the footer is especially remote on Appearance. | Keep a persistent footer. Use Save and close, Apply, and Cancel; make the main action prominent and show whether changes are pending. Apply should be unavailable when there are no changes. | P2 |

**1. settings-about.png**

[View screenshot](screenshots/settings-about.png)

| UI element | Visible issue | Proposed fix | Priority |
|---|---|---|---|
| Application heading | Generic heading does little to establish the app's identity. | Use a compact identity block with the app name, existing app icon if available, and version. | P3 |
| Version and Author rows | Useful metadata is spread across a large form-like area. | Place it in a compact, consistently aligned metadata block. | P2 |
| Repository label and URL | The label runs directly into the URL; the raw address dominates the row. | Use a descriptive Repository link with an external-link icon and proper label spacing; expose the full URL on demand. | P1 |
| Open button | Generic wording duplicates the adjacent link's role. | Combine link and action as Open repository. | P2 |
| Info text | One long line is difficult to scan. | Put a short description below the identity block and constrain its width so it wraps naturally. | P2 |
| Changelog expander | A standalone outlined button does not read as part of a content section. | Use a full-width disclosure row in a compact details group, with a consistent chevron. | P3 |
| Developer Options expander | Gives advanced controls similar prominence to the changelog and uses a different width. | Use the same disclosure pattern; keep advanced options lower in the page and collapsed by default. | P2 |

**2. settings-appearance.png**

[View screenshot](screenshots/settings-appearance.png)

| UI element | Visible issue | Proposed fix | Priority |
|---|---|---|---|
| Theme preset dropdown | A very wide field takes most of the first row. | Size it to readable preset names; keep the preset tools in a compact adjacent group. | P2 |
| Import, Export, Reset | All three have equal prominence, although Reset can discard customization. | Keep Import/Export secondary; separate Reset and name its scope, such as Reset appearance. | P2 |
| Color Palette expander | The expanded technical editor dominates before the user reaches material or widget controls. | Move basic widget/material controls first; collapse detailed palette editing by default. This is a layout recommendation, not a palette review. | P2 |
| Timer Widget palette fields and swatches | Narrow value fields and cramped labels make the two-column form uneven. | Use a consistent label/value/swatch grid with enough width for the accepted value format. | P2 |
| Dialogs & Notifications palette fields and swatches | Input widths and column rhythm differ from the neighboring group. | Reuse the same grid and field sizes as the widget palette. Stack the groups at narrower widths. | P2 |
| Success/Danger helper note and fields | The note says these values do not currently affect notifications, but they remain prominent editable controls. | Put inactive options in an advanced group and explain their current scope beside the fields. | P2 |
| Background mode dropdown showing Off | Off is unclear; diagnostics simultaneously describe a transparent backdrop. | Use explicit material names such as Solid and Transparent; offer Frosted only when available. Explain the selected material in plain language. | P1 |
| Background tint opacity label | The label is visibly clipped where the slider begins. | Give the label a wider column or place it above the slider. Show the full label. | P1 |
| Background tint, Clock, Buttons, Overall Fade sliders | Long tracks dominate the page, while independent opacity settings complicate legibility. | Use shorter aligned tracks with adjacent editable percentages. Keep text/icons fully opaque by default; move whole-widget fade to advanced settings. | P2 |
| Opacity Diagnostics panel | Raw implementation values occupy a large permanent panel; it explicitly says blur is unavailable. | Replace it with a short material status. Move raw values to Developer Options. Add backdrop blur for the intended glass effect where supported, with a solid fallback. | P2 |
| Scale slider and value | A long slider offers little precision beyond the displayed multiplier. | Pair a shorter slider with an editable multiplier and a Reset size action. | P2 |
| Use compact mode checkbox | A binary checkbox gives no visual explanation of the two layouts. | Use Compact/Expanded choices with small previews. | P2 |
| Appearance preview | No in-panel preview is visible, although the helper text says changes preview immediately. | Add a small widget preview close to material and layout controls so comparison stays in context. | P2 |
| Bottom helper paragraph | Save/cancel semantics, color formats, and file sharing are mixed together in small text. | Put each instruction beside its control; show only preview/save status near the footer. | P2 |
| Long page layout | The page mixes presets, a technical editor, material settings, diagnostics, and sizing in one tall flow. | Group it as Widget layout, Material, and Theme details; keep navigation and save actions accessible while content scrolls. | P2 |

**3. settings-general.png**

[View screenshot](screenshots/settings-general.png)

| UI element | Visible issue | Proposed fix | Priority |
|---|---|---|---|
| Startup group | Two short settings occupy a widely spaced area. | Use compact setting rows within one group. | P2 |
| Start with Windows checkbox | The visual treatment is a bare checkbox rather than a consistent setting row. | Use the shared setting-row pattern with a whole-label hit area; a switch is suitable if used consistently for on/off preferences. | P3 |
| Start minimized to tray checkbox | The label does not explain where the user can recover the widget. | Use Start in system tray with a brief explanation that the tray icon opens the widget. | P2 |
| Keep widget always on top checkbox | A single option sits under a large isolated heading. | Use one compact row under Window behavior. | P2 |
| Enable break reminders checkbox | Parent and dependent options have nearly equal visual hierarchy. | Use a primary on/off row and group its dependent controls beneath it. | P2 |
| Require acknowledgement checkbox | The long sentence makes a simple choice cumbersome to scan. | Use Keep reminder open until dismissed; put any additional explanation below it. | P2 |
| Reminder interval number field and arrows | Large side-by-side arrow cells compete with the value; units are buried in the long label. | Use a compact numeric input with minutes beside the value and an Every label. Align it with the dependent reminder settings. | P2 |

**4. settings-hotkeys.png**

[View screenshot](screenshots/settings-hotkeys.png)

| UI element | Visible issue | Proposed fix | Priority |
|---|---|---|---|
| Global Hotkeys heading and note | Two lines repeat the same concept; the note is longer than necessary. | Use Keyboard shortcuts and the helper Works even when FocusTimer is in the background. | P2 |
| Show/Hide Widget field | An example string looks like an ordinary text input; assignment and recording states are unclear. | Use a shortcut recorder with keycap-style tokens, Press shortcut, and a clear action. Show Not assigned explicitly when empty. | P2 |
| Toggle Timer field | Toggle Timer is less specific than the action the user performs. | Name it Start/pause timer and use the same shortcut recorder. | P2 |
| Shortcut registration/status area | The capture contains no visible per-shortcut status; interaction behavior is unverified. | Reserve an inline message beside each recorder for conflicts, invalid combinations, and successful registration. | P2 |
| Apply/OK helper text | A successful Apply is referenced without a nearby visible status pattern. | Show pending/saved/error feedback near the fields and shared footer, using the same save language as other settings. | P2 |

**5. settings-logging.png**

[View screenshot](screenshots/settings-logging.png)

| UI element | Visible issue | Proposed fix | Priority |
|---|---|---|---|
| Worklog CSV Location heading | File-format terminology leads the section even though the visible value is a folder. | Use Work logging, with a separate Storage folder field label. | P2 |
| Enable work logging checkbox | The enablement control is separated from the storage row by a large gap. | Put it in the group's first setting row, followed by storage controls with consistent spacing. | P2 |
| Storage path field | A long raw path dominates the row; no dedicated field label is visible. | Add Storage folder above it; preserve full editing and copying, and shorten only the read-only display when necessary. | P2 |
| Browse button | Generic wording does not state what is being selected. | Use Choose folder and align its height with the path field. | P3 |
| Retention number field and arrows | Oversized arrow cells and a long label obscure the simple retention choice. | Use Keep logs for [90] days and briefly explain when older logs are removed. | P2 |
| Project rules explanation | A dense paragraph mixes rule order, matching, history, storage behavior, and wildcard syntax. | Lead with First matching rule assigns the project. Put scope/history behavior in concise helper text and wildcard examples in expandable help. | P2 |
| Rule field containing * | No visible label explains which property this pattern matches. | Add an Application pattern header or explicit field label, after confirming the field's intended meaning. | P1 |
| Rule field containing FocusTimer | No visible label distinguishes it from the other pattern field. | Add a Window title pattern header or explicit label, after confirming the mapping. | P1 |
| Rule field containing Testing | The project destination is unlabeled. | Add a Project header/label; offer existing project suggestions while allowing entry. | P1 |
| Up and Down buttons | Reordering controls crowd each rule; boundary states are not visually distinct in the single-row capture. | Use compact reorder controls, disable impossible moves, and show rule order. Dragging can supplement keyboard-accessible controls. | P2 |
| Remove button | The destructive action has the same treatment as ordering controls. | Separate it from reorder controls and support undo for accidental removal. | P2 |
| Add project rule button | A large gap disconnects it from the rule list. | Place Add rule directly below the list using a secondary button with a small plus icon. | P2 |

**6. widget-compact.png**

[View screenshot](screenshots/widget-compact.png)

| UI element | Visible issue | Proposed fix | Priority |
|---|---|---|---|
| Widget shell | Rounded shape is useful, but the visible finish is flat rather than frosted. | Keep the compact rounded shell; add backdrop blur, a fine edge, and restrained depth. Assess the shadow with an uncropped window capture. | P3 |
| Timer digits and separators | Heavy slashed zeros and prominent colons make the timer feel technical. | Use cleaner medium-weight tabular numerals and quieter separators while keeping the timer dominant. | P3 |
| Play icon | The solid triangle has a different visual weight from the menu strokes. | Use a consistent vector icon family and a subtle button state; give the button a measured logical hit area and Start/pause tooltip. | P2 |
| Menu icon | Thick strokes add unnecessary visual weight beside the timer. | Use a smaller, evenly stroked icon with a consistent button container and Menu tooltip. | P3 |
| Two-button vertical rail | Actions appear as loose symbols rather than an aligned control group. | Align them in equal-sized button slots with consistent spacing and edge insets. | P2 |
| Timer state and dragging affordance | No textual state or drag hint is visible; existing behavior is unverified. | Keep compact mode quiet, but expose Running/Paused in a tooltip and a subtle drag affordance on hover. Keep drag regions separate from controls. | P2 |

**7. widget-expanded.png**

[View screenshot](screenshots/widget-expanded.png)

| UI element | Visible issue | Proposed fix | Priority |
|---|---|---|---|
| Widget shell | The wide, flat capsule lacks a visible material hierarchy. | Reuse the compact widget's frosted shell, edge treatment, and depth. | P3 |
| Timer digits | Same heavy slashed-zero style; digits consume most of the row. | Use cleaner tabular numerals and tune digit size together with control spacing. | P3 |
| Play icon | Its filled geometry differs from adjacent outlined/stroked controls. | Use one icon family and identical button hit areas. | P3 |
| Reset icon | Reset sits next to Start with little distinction between actions. | Add slight separation, a Reset timer tooltip, and protection against accidental loss of an active session. | P2 |
| Down-chevron control | The glyph alone does not establish whether it collapses, minimizes, hides, or opens something. | Use an icon and tooltip that match the actual action; add an accessible name. Its function needs confirmation from behavior. | P1 |
| Menu icon | Heavier strokes than neighboring icons disrupt consistency. | Match icon size and stroke weight to the other actions. | P3 |
| Action row and timer/action gap | Different icon shapes sit loosely across a broad row. | Use equal button slots with a consistent timer-to-controls gap and balanced outer padding. | P2 |

**Shared date toolbar in all three worklog screenshots**

| UI element | Visible issue | Proposed fix | Priority |
|---|---|---|---|
| Previous/next day buttons | Narrow standalone buttons are visually detached from the date field. | Group them with the date selector and use consistent chevron icons and logical hit areas. | P2 |
| Date field | Numeric date order can be ambiguous; the trailing symbol does not clearly read as a calendar. | Use a locale-aware date label such as Mon, 5 Oct 2026 and a proper calendar icon. Preserve direct date entry. | P1 |
| Today button and Today heading | The same word appears twice side by side; Summary repeats it again below. | Keep Today as the navigation action and use one clear selected-date/page heading. | P2 |
| Date/navigation toolbar layout | Date controls, page heading, tabs, and view tools form several disconnected rows. | Establish a consistent compact header, tab strip, and view-specific toolbar across Entries, Timeline, and Summary. | P2 |

**8. worklog-entries.png**

[View screenshot](screenshots/worklog-entries.png)

| UI element | Visible issue | Proposed fix | Priority |
|---|---|---|---|
| Add entry button | It has the same visual weight as maintenance and destructive actions. | Make Add entry the primary toolbar action; keep the others secondary. | P2 |
| Edit button | Selection dependency is not evident in this capture. | Make the selected row obvious and disable Edit when no valid selection exists. Verify actual behavior separately. | P2 |
| Delete button | Deletion is mixed into the main action group with equal emphasis. | Separate it from Add/Edit and support undo or an appropriate confirmation for deletion. | P2 |
| Refresh button | A full-width text button adds weight to an already busy action row. | Use a compact refresh action with a tooltip and visible progress when refreshing. | P3 |
| Search field | The shortcut is embedded in placeholder text, which disappears during entry. | Use a search icon, concise Search entries text, a separate Ctrl+F hint, and a clear action. | P2 |
| Table headers | Header text blends into the body and has no clear containing row. | Add a distinct header row with a subtle separator and consistent alignment. Show sort indicators where sorting is supported. | P2 |
| Start and End columns | Time values are useful but need a consistent typographic system. | Keep tabular numerals, predictable column widths, and consistent alignment. | P3 |
| Duration column | Values such as 0h 30m and 1h 00m contain redundant zero units. | Display 30m and 1h; use the same formatting in Summary. | P2 |
| Application and Window columns | Long text and uneven column widths weaken row scanning. | Give both columns deliberate sizing/resizing rules; truncate with full-text access when needed. | P2 |
| Project column | It is far from the other row data because Window absorbs a large amount of width. | Cap the title column at a useful width and bring Project nearer; allow users to resize columns. | P2 |
| Table rows and bold Microsoft Teams cell | Only one application cell is bold, with no visible explanation; rows have little separation. | Use a consistent full-row hover/selection pattern and light separators. If bold marks manual entries, replace it with an explicit Manual label. | P2 |

**9. worklog-summary.png**

[View screenshot](screenshots/worklog-summary.png)

| UI element | Visible issue | Proposed fix | Priority |
|---|---|---|---|
| Today/4h 20m total block | The date label is repeated and the value lacks a specific metric label. | Use one compact total block labeled Tracked time for the selected date. | P2 |
| Refresh button | It sits at the far edge, disconnected from the summary controls. | Put it in the shared view toolbar beside Group by. | P2 |
| Group by dropdown | The label and selected value repeat By; the control sits separately from other tools. | Use Group by with Application as the value and align it in the toolbar. | P3 |
| Application labels | Names sit far from their duration and percentage values. | Constrain chart width and use a consistent row with name, duration, and percentage directly above its bar. | P2 |
| Horizontal bars | The simple comparison works, but the very wide layout makes the values feel detached. | Keep proportional bars; constrain their container and optionally use subtle full-length tracks for a common baseline. | P2 |
| Duration values | Redundant zero units repeat the table's formatting problem. | Use 3h, 50m, and 30m. Align values consistently. | P2 |
| Percentage values | Percentages form a distant second numeric column. | Keep them beside duration values within the same chart row. The displayed percentages and total agree with the shown entries. | P2 |

**10. worklog-timeline.png**

[View screenshot](screenshots/worklog-timeline.png)

| UI element | Visible issue | Proposed fix | Priority |
|---|---|---|---|
| Group by dropdown | The long default label occupies excessive toolbar space. | Use a shorter All entries value and a compact, consistent field width. | P3 |
| Zoom minus, percentage, plus | Separate buttons and a loose percentage create a scattered control. | Combine them into one compact zoom group with predictable step sizes and an editable value if useful. | P2 |
| Reset button | Reset does not specify that it affects zoom rather than worklog data. | Rename it Reset zoom or Fit day, according to its actual behavior. | P2 |
| Ctrl + mouse wheel helper | A long instruction dominates the toolbar. | Keep a compact shortcut hint or tooltip beside the zoom group. | P3 |
| Hour labels and gridlines | The labels sit below their corresponding lines, and the grid spans an exceptionally wide lane. | Align labels with the hour boundary; use consistent tabular numerals and restrained gridlines. | P2 |
| Entry cards | Nearly full-window-width blocks make a few left-aligned lines feel disconnected from their containers. | Give the lane a useful width; use restrained inner surfaces and consistent padding. Keep card height proportional to duration. | P2 |
| Short-entry content | 10:15–10:45 and 12:00–12:30 clip application text; 14:00–14:20 shows only the time range. | Adapt card content to available height: full details on tall cards, a concise single line on short cards, and full details through a tooltip or detail panel. Never leave partially clipped text. | P1 |
| Medium-entry descriptions | The 11:00–11:45 description is cut off at the card's bottom edge. | Use height-aware content rules and ellipsis before the text reaches the edge. | P1 |
| Filled versus outlined entry | The Microsoft Teams card has a different treatment, but no visible legend explains it. | Name the represented state through a badge/legend. If it denotes selection, use a consistent selected-card treatment instead. Confirm its actual meaning. | P2 |
| Timeline scroll area | A thin floating scrollbar and a partly cut-off bottom hour provide a weak viewport boundary. | Use a clearly bounded scroll area with consistent scrollbar placement; keep the date and zoom toolbar fixed while scrolling. | P2 |

**Recommended order of work**

1. Fix text clipping and label ambiguity, especially timeline cards and project rules.
2. Standardize navigation, control dimensions, typography, spacing, and save actions.
3. Tighten the widget controls and worklog table/chart layouts.
4. Add the frosted material, subtle edge treatment, and shadow; keep content legible and provide a solid fallback.
5. Verify at actual display scales and narrow window widths, then check hover, focus, keyboard navigation, shortcut conflicts, and save/cancel behavior.

Extraction was checked against the ZIP manifest: 10 image files extracted, 10 individually viewed. No application code or theme values were changed.
