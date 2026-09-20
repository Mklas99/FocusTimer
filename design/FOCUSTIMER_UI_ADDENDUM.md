# FocusTimer UI Addendum

**Applies with:** `Modern Glass UI Design Standard`

This file specializes the generic standard for FocusTimer. It changes product character and surface choices; it does **not** relax the generic standard’s consistency, accessibility, state-completeness, or verification requirements.

---

## 1. Product character

FocusTimer SHOULD feel:

**calm · focused · precise · quiet · modern · trustworthy · low-friction**

It MUST NOT feel like a game, attention-seeking dashboard, neon “cyber” interface, or collection of decorative glass cards.

The application is used while the user is trying to reduce distraction. The UI itself must therefore consume very little attention when no action is required.

---

## 2. Visual direction

Use a restrained contemporary desktop aesthetic:

- deep teal as the principal identity family
- soft blue as a supporting family
- muted cyan as a controlled highlight/accent family
- neutral slate/ink surface and text roles
- subtle ambient gradients only on large background regions where they do not reduce legibility
- softly rounded geometry
- restrained depth
- light translucency on shell/navigation/transient UI rather than on dense content

Exact colors MUST live in the project’s semantic token set. Feature views MUST NOT introduce their own approximate teal/blue/cyan values.

Dark mode is first-class, not an afterthought.

---

## 3. Windows desktop material strategy

FocusTimer is a long-lived desktop utility, so the main timer must remain stable and readable for extended periods.

Where the supported Windows/Avalonia version permits it:

| Surface | Preferred direction |
|---|---|
| main window backdrop | subtle Mica-like/system backdrop |
| timer content area | solid or near-solid with very high legibility |
| title/command/navigation region | lightly materialized, integrated with window backdrop |
| tray flyout/popover | Acrylic-like/frosted transient surface |
| context menu/dropdown | Acrylic-like/frosted transient surface |
| modal dialog | stable near-opaque surface over dim scrim |
| settings/logs/data views | predominantly solid/near-solid |
| compact mode | same material system at reduced density |

Always provide a solid fallback. Never make FocusTimer dependent on transparency being available.

Do not simulate multiple stacked acrylic layers when a single platform material surface can express the hierarchy.

---

## 4. Timer hierarchy

The timer is the visual hero.

The default visual priority is:

1. remaining time
2. current mode/state (focus, break, paused, completed)
3. current task/session context, when present
4. primary timer action
5. secondary timer actions
6. statistics/settings/administrative controls

The remaining-time display MUST remain extremely legible in every theme and window size.

Timer digits SHOULD use tabular numerals so the display does not shift horizontally as values change.

Do not animate the full timer surface every second. Numerical changes should feel stable.

A progress ring, arc, bar, or similar visualization MAY supplement time, but it must remain subordinate to the numeric value and must not create constant distracting motion.

---

## 5. Primary controls

At any moment the currently relevant action — typically Start, Pause, or Resume — MUST be visually obvious.

Reset, Skip, Stop, edit-session, and other secondary actions SHOULD have lower emphasis.

Equivalent timer controls MUST use the same icon, label, size, radius, state behavior, and semantic color everywhere they appear, including normal, compact, and tray surfaces.

Destructive/reset-like actions must not visually compete with the primary timer action.

---

## 6. Session states

Focus, break, paused, and completed states SHOULD be distinguishable through a combination of:

- semantic text/label
- iconography where useful
- restrained accent/tint changes
- optional subtle progress treatment

Do not encode session state through color alone.

State changes SHOULD feel calm. Avoid aggressive flashing, repeated pulses, continuous glow, or full-screen color changes.

A completed session MAY receive one restrained acknowledgement animation/transition, then settle into a stable state.

---

## 7. Motion

FocusTimer motion should be quieter than a general consumer app.

Use short state transitions for Start/Pause/Resume and panel changes. Prefer opacity, small translation, or subtle scale changes.

Avoid:

- ticking/pulsing every second
- continuously rotating decorative elements
- animated background gradients while a session runs
- strong parallax
- repeated celebration animations
- blur animation across large surfaces

Reduced-motion mode MUST replace nonessential movement with minimal fades or immediate state changes.

---

## 8. Compact mode

Compact mode is **not** a second design language.

It MUST use the same semantic tokens, timer typography family, controls, state colors, radii logic, icons, and interaction states as the normal window.

Only density, arrangement, and visibility priority may change.

The compact surface SHOULD expose only the information/actions needed to monitor and control the current session.

---

## 9. Settings, rules, logs, and data-heavy views

These surfaces prioritize readability and scanning over visual effect.

Use solid or near-solid content surfaces with clear headings, spacing, aligned fields, and restrained separators.

Do not render every setting as an independent floating glass card.

Rules/configuration interfaces SHOULD make grouping and dependency obvious through structure rather than decorative containers.

Logs/history/statistics SHOULD keep data backgrounds stable; glass belongs to shell/navigation/transient controls, not underneath dense rows or charts.

---

## 10. Tray and transient surfaces

The system-tray experience SHOULD feel like a compact extension of the main window rather than a separate mini-app.

It should preserve:

- same timer state terminology
- same primary action semantics
- same icon family
- same accent/state logic
- same radius/material philosophy

Because tray surfaces are transient, stronger Acrylic-like treatment is appropriate when available, provided readability is preserved.

---

## 11. Notifications

Notifications SHOULD be concise, calm, and actionable.

Prefer messages that communicate the state transition and one useful next action.

Avoid gamified streak pressure, excessive celebratory language, or repeated notifications that make the focus tool itself distracting.

System-native notification behavior SHOULD be preferred when available.

---

## 12. Window and platform behavior

FocusTimer SHOULD behave like a well-integrated Windows desktop application.

Preserve expected Windows behaviors for window movement, resizing, focus, keyboard navigation, system theme, high contrast, DPI scaling, title-bar controls, and taskbar/tray behavior.

Custom chrome must not reduce discoverability or accessibility of standard window actions.

Keyboard focus MUST remain clearly visible.

---

## 13. Effect/performance policy

FocusTimer should remain lightweight while running for long periods.

Avoid overlapping transparent layers and excessive shadow/blur effects.

Use one meaningful backdrop/material layer instead of reproducing “glass” independently in each child panel.

Effects MUST NOT cause timer input, resizing, opening a tray flyout, or navigating settings to feel delayed.

Where composition effects are disabled or expensive, fall back gracefully to semantic solid surfaces without changing layout.

---

## 14. Coherence rule for every FocusTimer UI change

Before accepting a UI modification, compare it with all other surfaces that expose the same concept.

Examples:

- changing Start/Pause styling → inspect main, compact, and tray controls
- changing session-state colors → inspect timer, progress, tray, notification, and history/status representation
- changing menu radius/material → inspect every menu/popover/flyout
- changing input styling → inspect settings, rules, edit dialogs, and any task/session editor
- changing spacing/density → verify normal and compact modes remain one system

A local improvement that causes cross-surface inconsistency is a regression.

---

## 15. FocusTimer acceptance additions

In addition to the generic acceptance gate, verify:

| Check | Required outcome |
|---|---|
| Timer prominence | Remaining time is unmistakably the primary information |
| Calmness | No unnecessary recurring movement or visual noise |
| State clarity | Focus/break/paused/completed are understandable without color alone |
| Cross-mode coherence | Main, compact, and tray experiences use the same component language |
| Long-lived readability | Main timer remains readable over extended use |
| Data readability | Settings/logs/rules do not sacrifice clarity for glass effects |
| Windows adaptation | Theme, high contrast, scaling, transparency fallback, and native behaviors remain correct |
| Performance | Backdrop/blur/shadow use remains lightweight |

---

## 16. FocusTimer visual summary

FocusTimer should resemble a **quiet, premium Windows utility with modern material depth**, not a glass-effect showcase.

The main content stays stable. Glass appears mainly at the shell, navigation, and transient interaction layers. Soft geometry, deep teal/blue/cyan semantics, strong timer hierarchy, restrained motion, and shared components make every surface recognizably part of the same application.
