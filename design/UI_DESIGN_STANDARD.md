# Modern Glass UI Design Standard

**Purpose:** Reusable, platform-aware instructions for creating and modifying modern application interfaces with restrained translucency, soft geometry, coherent depth, and high usability.

**Scope:** All UI surfaces: windows, pages, panels, navigation, controls, dialogs, menus, popovers, tooltips, notifications, settings, data views, compact/responsive variants, and platform-specific shells.

**Normative language:** **MUST / MUST NOT** = required. **SHOULD / SHOULD NOT** = default rule; deviate only for a documented reason. **MAY** = optional.

---

## 1. Design priorities

When rules conflict, use this order:

1. Usability and task clarity.
2. Legibility and accessibility.
3. Cross-surface consistency.
4. Existing product and platform conventions.
5. Visual hierarchy.
6. Performance and responsiveness.
7. Aesthetic character and decorative effects.

A visually impressive result that weakens a higher-priority item is not acceptable.

The target language is **modern, quiet, precise, layered, softly rounded, and lightly translucent**. Glass is a material for communicating hierarchy and context, not decoration to apply everywhere.

---

## 2. Respect the existing product system

Before creating or modifying UI, inspect the repository's current:

- design-system documentation
- theme model
- semantic resources/tokens
- shared styles
- reusable controls/components
- platform-specific material support
- equivalent/sibling surfaces
- accessibility behavior
- visual test/screenshot infrastructure

The existing coherent design system MUST be extended before creating a competing parallel system.

Do not introduce a second source of truth for values or components that already exist elsewhere.

If the current system is inconsistent, first identify the authoritative source, then migrate toward it deliberately.

A UI task is not permission to redesign unrelated product areas.

---

## 3. Design-system layers

A durable UI system SHOULD separate three layers.

### 3.1 Palette tokens

Palette tokens represent raw theme inputs:

- neutral tones
- accent hues
- success/warning/error colors
- light/dark variants
- optional material tint colors

Palette tokens are not component semantics.

### 3.2 Semantic tokens

Semantic tokens describe visual purpose:

- `surface.base`
- `surface.content`
- `surface.raised`
- `surface.transient`
- `text.primary`
- `text.secondary`
- `border.subtle`
- `action.primary`
- `focus.ring`
- `status.error`

Components SHOULD consume semantic tokens rather than raw palette values.

### 3.3 Component tokens

Component tokens specialize semantic decisions for reusable patterns:

- `button.primary.background`
- `button.icon.hover`
- `input.border.focus`
- `dialog.surface`
- `navigation.selected`
- `timer.primary.foreground`

Component tokens MAY alias semantic tokens.

**Rule:** raw palette values SHOULD NOT be referenced directly from feature views when a semantic or component token can express the intended role.

---

## 4. Theme boundaries

Theme variants SHOULD primarily change:

- palette values
- supported material tint/opacity
- platform-adaptive backdrop choices where necessary

Theme variants SHOULD NOT normally change:

- spacing
- component geometry
- typography hierarchy
- navigation structure
- interaction behavior
- focus behavior
- information architecture
- state semantics

A theme is a visual variant of one product system, not a different product.

High-contrast or accessibility themes MAY intentionally override normal visual treatment where necessary for legibility or platform expectations.

---

## 5. Source of truth and change discipline

Before changing UI, identify the authoritative files for:

- tokens
- themes
- components
- styles
- product-specific design rules

Existing approved semantic tokens and reusable components MUST be reused before introducing new values or variants.

Do not create local one-off values for color, opacity, corner radius, spacing, border, shadow, typography, icon size, or motion when an existing semantic token can represent the same role.

If a genuinely new visual primitive or interaction pattern is required, define it at design-system level first, then apply it consistently wherever the same semantic role occurs.

A change to one surface MUST include a coherence check of sibling surfaces that expose the same component or interaction pattern.

Do not modernize one page while leaving equivalent controls visually inconsistent elsewhere.

---

## 6. Duplicate-source-of-truth rule

Overlapping style sources are not allowed to evolve independently.

If two files define the same semantic responsibility, one of the following MUST happen:

1. merge them;
2. split responsibilities clearly and document ownership;
3. formally deprecate one;
4. remove the obsolete source after verifying it is unused.

Examples include duplicate token files, view-local styles duplicating global styles, multiple component classes representing the same semantic role, or old theme files kept active beside new theme files.

The repository should make it obvious where a designer or coding agent must change a given visual decision.

---

## 7. Material and surface hierarchy

The interface MUST have a deliberate surface hierarchy.

Transparency is strongest on transient or navigational layers and weakest on content-heavy or long-lived surfaces.

| Role | Typical surfaces | Default material |
|---|---|---|
| Base | app/window background | opaque or near-opaque; optionally platform backdrop |
| Content | reading/work area, forms, lists, tables | solid or near-solid |
| Raised | navigation, toolbar, selected panel | lightly translucent or tinted |
| Transient | menu, popover, flyout, tooltip | frosted/Acrylic-like with sufficient tint/blur |
| Blocking | modal dialog | opaque/near-opaque over dimming scrim |
| Emphasis | selected/active control | semantic accent/tint; not necessarily more transparent |

MUST NOT apply glass uniformly to every card, content panel, or nested container.

MUST NOT stack multiple translucent surfaces merely for visual richness.

Glass-on-glass is allowed only when it is an intentional, tested composition with clear layer separation.

Long-reading/content-heavy surfaces SHOULD be solid or near-solid.

When the backdrop is unpredictable, increase tint/occlusion and, where appropriate, blur.

Never preserve transparency at the expense of text/control contrast.

---

## 8. OS material and fallback ordering

Where platform material APIs expose multiple fallback options, their ordering is part of the design system and MUST be intentional.

The preferred order MUST reflect the semantic role of the surface.

Examples:

- long-lived app shell → platform backdrop / Mica-like → blur → solid
- transient popover → Acrylic/frosted → blur → tinted solid
- dense content → solid / near-solid directly

Never rely on an accidental fallback sequence.

Every translucent material MUST have a readable opaque or near-opaque fallback for reduced-transparency modes, unsupported composition, remote sessions, compositor restrictions, battery/performance constraints, high-contrast modes, and platform limitations.

The fallback must preserve layout, hierarchy, and interaction semantics.

---

## 9. Color system

All production UI colors MUST use semantic tokens rather than raw component-local values.

At minimum define roles for:

- application/background surfaces
- content surfaces
- raised/transient surfaces
- primary, secondary, muted text
- borders/dividers
- accent/brand
- focus
- selection
- success, warning, error, information
- modal scrim
- material tint/border

Color roles MUST adapt coherently to light, dark, and high-contrast themes.

Accent color SHOULD be concentrated on important actions, selection, focus, progress, and meaningful status.

Color MUST NOT be the sole carrier of state or meaning.

Text contrast SHOULD meet WCAG AA: at least 4.5:1 for normal text and 3:1 for large text. Meaningful non-text graphics SHOULD provide at least 3:1 contrast where WCAG applies.

On translucent surfaces, contrast MUST be evaluated against realistic worst-case backdrops.

---

## 10. Typography

Use a restrained semantic type scale:

`display` → `headline` → `title` → `body` → `label` → `caption`

Typography tokens MUST define family, size, weight, line height, and optional letter spacing.

Components MUST consume semantic typography roles rather than set independent values.

Use the platform/system typeface unless the product has an intentional approved typeface.

Numeric interfaces SHOULD use tabular numerals when changing digit widths would cause movement or reduce scanability.

Never place critical text directly over a busy/dynamic backdrop without a stable material beneath it.

---

## 11. Geometry and corner system

Rounded geometry SHOULD feel soft and contemporary without turning every element into a capsule.

If the product has no established radius scale, use this default starting system:

| Token | Radius | Typical role |
|---|---:|---|
| `radius-xs` | 4 px | tiny controls, badges |
| `radius-sm` | 8 px | compact controls |
| `radius-md` | 12 px | buttons, inputs, small cards |
| `radius-lg` | 16 px | panels, dialogs, menus |
| `radius-xl` | 24 px | large feature surfaces |
| `radius-full` | full | circular controls, intentional pills |

Exact values MAY be adapted, but the number of radius values SHOULD remain small.

Nested rounded surfaces MUST preserve visual concentricity.

Do not round edges flush with the viewport/window when doing so creates awkward gaps or weakens alignment.

Pill shapes SHOULD be reserved for semantics that benefit from them.

---

## 12. Spacing, layout, and density

Spacing MUST be tokenized for reusable semantic intervals.

If no project scale exists, use a 4 px base grid with a stronger 8 px rhythm:

`4, 8, 12, 16, 24, 32, 48, 64`

Do not tokenize every unique structural coordinate merely for tokenization.

Tokenize recurring semantic spacing such as:

- control internal padding
- sibling control spacing
- section spacing
- window padding
- panel padding
- navigation spacing

Use proximity to express relationships.

Prefer whitespace and alignment over unnecessary divider lines or extra cards.

Common edges MUST align.

The hierarchy must survive resizing, localization, text scaling, and increased content length.

Compact/dense modes SHOULD alter density while preserving the same components, hierarchy, color semantics, radii logic, and interaction states.

---

## 13. Depth, borders, and shadows

Depth MUST be semantic and tokenized.

Use the minimum number of elevation levels necessary.

Prefer subtle surface contrast, thin borders, and restrained soft shadow over large dark drop shadows, glowing outlines, heavy inner shadows, or exaggerated 3D effects.

Borders on material surfaces SHOULD clarify edges without becoming decorative frames.

On dark themes, elevated surfaces must remain distinguishable without relying only on shadow.

---

## 14. Components and state completeness

Reusable components are the primary vehicle for visual coherence.

Every interactive component MUST define all applicable states:

`default`, `hover`, `pressed`, `focus`, `selected/checked`, `disabled`, `loading`, `error/invalid`

A component with the same semantic role MUST look and behave consistently across the application.

Pointer targets MUST meet applicable accessibility/platform requirements. Never make a target smaller than WCAG’s 24 × 24 CSS-pixel minimum where that criterion applies, except for its defined exceptions.

Focus MUST be clearly visible. A robust default is an indicator equivalent to at least a 2 px perimeter with sufficient contrast.

Iconography MUST use a coherent family, optical weight, and size scale. Ambiguous icons require labels or accessible names.

---

## 15. Visual size versus interaction target

The visible icon or glyph size and the interactive target size are separate concepts.

A compact visual icon MAY sit inside a larger hit/focus target.

Do not shrink interaction targets merely to make the interface appear cleaner.

Do not disable keyboard focus globally for an entire component class unless that class is genuinely non-interactive or there is a documented platform-specific reason.

---

## 16. Motion and interaction feedback

Motion exists to explain cause, state, hierarchy, and spatial continuity.

Decorative motion that competes with the task SHOULD NOT be used.

If no motion system exists, use these starting ranges:

| Motion class | Typical duration |
|---|---:|
| press/hover feedback | 50–150 ms |
| local state transition | 120–220 ms |
| surface enter/exit | 150–300 ms |
| larger contextual transition | 200–400 ms |

Use semantic duration/easing tokens.

Avoid continuous animation of blur, large transparent areas, shadows, or backdrop sampling when transform/opacity communicates the same state.

Reduced-motion preferences MUST be respected.

Interaction feedback MUST NOT be delayed merely so an animation can finish.

---

## 17. Navigation and information hierarchy

Each surface MUST make visually apparent:

- where the user is
- what the primary content is
- what the primary action is
- which actions are secondary
- which elements are interactive
- what state the system is in

Do not create competing primary actions.

Use progressive disclosure for advanced/infrequent controls.

Navigation surfaces MAY use stronger material effects than content, but geometry, spacing, active-state treatment, and iconography MUST remain consistent.

---

## 18. Dialogs, menus, popovers, and transient UI

Transient UI is the preferred place for stronger frosted-glass treatment.

Menus/popovers SHOULD have clear edge separation, sufficient blur/tint, consistent padding/radius, predictable placement, and consistent hover/selection/focus/disabled states.

Dialogs SHOULD prioritize readability over transparency and use a stable surface plus dimming/smoke scrim where interaction beneath is blocked.

Tooltips SHOULD be concise, high contrast, and visually subordinate.

Do not let transient glass become transparent enough that underlying text merges with foreground text.

---

## 19. Forms and data-heavy surfaces

Forms, tables, logs, settings, editors, and other content-dense surfaces SHOULD use mostly solid or near-solid backgrounds.

Do not place each setting or data row in a separate glass card.

Group through typography, spacing, headings, alignment, and restrained dividers before introducing extra containers.

Validation must be communicated by more than color and must be understandable with keyboard and assistive technology.

---

## 20. Themes and system adaptation

Treat these as first-class system states:

- light mode
- dark mode
- high contrast
- reduced transparency
- reduced motion
- DPI scaling
- text scaling
- window resizing
- composition disabled/unavailable

Theme changes SHOULD swap semantic token values rather than fork component structure.

Do not hard-code effects that assume a particular wallpaper, backdrop, luminance, or theme.

Native/system materials SHOULD be preferred where they improve platform integration. Custom material simulation MUST preserve the same accessibility fallbacks.

---

## 21. Performance constraints

Visual sophistication MUST remain responsive.

Avoid unnecessary overlapping semi-transparent layers, repeated backdrop blurs, enormous blur radii, continuously animated shadows, and excessive off-screen effects.

Prefer one composited material surface over many nested material surfaces.

If an effect causes measurable interaction/scrolling degradation, simplify it before compromising responsiveness.

---

## 22. Hard-coded visual values

Feature views SHOULD NOT contain unexplained raw visual constants for reusable design decisions.

The following usually belong in the design system:

- reusable colors
- recurring opacity levels
- semantic spacing
- corner radii
- reusable control sizes
- typography roles
- border styles
- elevation
- motion durations
- shared icon sizes

Local structural values MAY remain local when they are specific to one layout and do not represent a reusable rule.

A UI review SHOULD explicitly search for raw visual values that bypass the design system.

---

## 23. Anti-patterns

Non-compliant unless explicitly justified:

- glass applied to every surface
- transparency that harms legibility
- nested cards without hierarchy benefit
- arbitrary gradients on unrelated components
- inconsistent radii for equivalent components
- one-off colors/spacing/shadows instead of tokens
- oversized glowing borders
- decorative blur
- motion with no state/causal meaning
- low-contrast focus indicators
- semantically identical controls using different visual roles
- duplicate style sources defining the same responsibility
- redesigning one screen in isolation
- visual novelty replacing information hierarchy

---

## 24. Incremental migration strategy

For an established application, prefer:

1. identify current authoritative design sources;
2. document target design rules;
3. establish palette, semantic, and component tokens;
4. remove/deprecate duplicate style sources;
5. refactor shared components/styles;
6. migrate the highest-value surface;
7. migrate sibling surfaces;
8. migrate supporting/transient surfaces;
9. validate all themes/system modes;
10. add automated or visual regression checks where practical.

Each migrated surface must still work with non-migrated areas during transition.

---

## 25. UI-change workflow for designers and coding agents

### A. Inspect

Identify the affected surface, user task, sibling surfaces, theme/design files, reusable components, semantic tokens, supported system modes, interaction states, and material constraints.

### B. Map

Map every proposed element to an existing component and semantic role where possible. Identify explicitly which primitive is genuinely missing.

### C. Design

Apply material hierarchy, token system, spacing rhythm, typography, geometry, state model, accessibility requirements, and platform conventions.

### D. Implement

Use shared tokens/styles/components. Do not embed unexplained raw design constants in feature code.

### E. Verify source coherence

Check for duplicate styles, hard-coded reusable values, semantically equivalent controls using different tokens, local overrides bypassing the design system, and accidental theme-specific geometry changes.

### F. Verify rendered output

Check actual rendered results across relevant states, themes, sizes, densities, keyboard focus, text scaling, reduced motion, reduced transparency, high contrast, realistic content, and material fallbacks.

### G. Propagate

If the change modifies a shared pattern, update the shared component/token and audit equivalent usages.

---

## 26. Acceptance gate

A UI change is complete only if:

| Check | Required outcome |
|---|---|
| Hierarchy | Primary content/action is immediately identifiable |
| Material | Glass is semantic and restrained |
| Consistency | Equivalent components use the same rules everywhere |
| Tokens | No unjustified local reusable visual constants |
| Geometry | Corner treatment and nesting are coherent |
| Layout | Alignment and spacing follow the shared system |
| States | All applicable interaction states exist |
| Themes | Relevant themes/system modes render correctly |
| Accessibility | Contrast, focus, keyboard, scaling, motion/transparency preferences are respected |
| Fallbacks | Material fallback ordering is intentional and readable |
| Performance | Effects do not degrade normal interaction |
| Source of truth | No conflicting duplicate styles/tokens remain |
| Render review | Final rendered UI has been visually inspected |

If any required item fails, the UI change is not finished.

---

## 27. Recommended design-system representation

For durable human + AI use, store two complementary layers.

### Machine-readable layer

Exact tokens for palette, semantic color, typography, spacing, radius, elevation, motion, and component aliases.

### Human-readable layer

Rationale and application rules for material hierarchy, component semantics, theme boundaries, accessibility, platform adaptation, do/don’t guidance, migration policy, and examples.

Tokens are normative for exact values. Prose is normative for semantics and appropriate use.

Where tooling permits, validate token references, theme completeness, contrast, duplicate definitions, and component-state coverage.

---

## 28. Final design principle

The target is not maximum glass.

The target is **a coherent interface in which material, geometry, color, typography, spacing, motion, and interaction behavior operate as one system**.

Transparency should make hierarchy easier to understand and the product feel more refined while remaining almost invisible as an interaction burden.
