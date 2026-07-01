# Design Rules

These rules define the current Web host visual policy. They are product-level ideas, not implementation details.

The current policy is **HeroUI v3.2.1 off-the-shelf appearance**. The product must look like standard HeroUI components as shipped by the framework, not like a separate Vitarish design system.

Implementation details, class names, package names, CSS imports, and code-level rules belong in `component-patterns.md` and `heroui-v3.2.1-off-the-shelf-reset.md`.

---

## Off-the-Shelf Framework Fidelity

The UI should feel like the framework's own documented examples. A developer who knows HeroUI should immediately recognize the page as normal HeroUI, without needing to learn a Vitarish-specific visual language.

The product must not invent a separate surface system, theme system, card system, badge system, app shell, navigation treatment, or layout aesthetic when HeroUI already provides a standard visual result.

The best visual decision is usually the one that deletes custom appearance code and lets the component library speak for itself.

---

## Default Is a Feature

Default styling is intentional. The built-in HeroUI look is not a placeholder to be replaced; it is the required visual baseline.

A component that looks plain, generic, and framework-native is correct under this policy. A component that looks branded, glassy, atmospheric, highly decorated, or custom-designed is suspect.

The interface should not try to impress through visual invention. It should remain predictable, familiar, and maintainable.

---

## No Product-Specific Visual Identity

The React Web host must not carry a custom Vitarish visual identity unless a future accepted decision explicitly changes this policy.

That means no bespoke glassmorphism, no custom ambient backgrounds, no custom card depth language, no custom accent system, no decorative orbs, no brand-specific gradients, no custom shadows, no custom radii, no invented status badges, and no product-owned theme tokens.

Product identity may exist in words, navigation labels, page titles, domain concepts, and feature behavior. It must not be encoded as a separate visual styling system.

---

## Theme Humility

Light, dark, and system themes should follow the component library's built-in theme behavior.

Dark mode should not be treated as an opportunity to create a second custom aesthetic. It should simply use the framework's own dark appearance.

Theme selection may persist user preference, but it must not override the visual identity of the component library.

---

## Layout Without Decoration

Pages may use structural layout to make content usable: grouping, stacking, grids, responsive flow, and readable boundaries.

Layout must not become visual branding. Spacing and grouping exist to make screens understandable, not to create a signature look.

A page shell should be boring. It should give HeroUI components room to render in their own style.

---

## Components Over Decorations

Prefer real framework components over hand-built visual elements.

A button should be a framework button. A card should be a framework card. An alert should be a framework alert. A table should be a framework table. A form field should be a framework form field.

Do not replace framework affordances with custom visual approximations.

---

## Accessibility Must Survive the Reset

Removing custom visuals must not remove accessibility.

Focus visibility, labels, descriptions, error messages, disabled states, loading states, keyboard behavior, and semantic structure remain mandatory.

When a custom visual layer was hiding accessibility mistakes, fix the accessibility directly through framework-supported component APIs rather than reintroducing custom styling.

---

## Minimalism Means Less Code

The cleanest UI is the one with the least visual code.

Remove visual wrappers, custom classes, global token overrides, and one-off styling before adding anything new.

A page that needs many visual classes to look acceptable is not aligned with this policy. The correct response is usually to use a more appropriate HeroUI component or simplify the layout.

---

## Prohibited Visual Outcomes

The product must not feel:

- **Custom branded** — no separate Vitarish look on top of HeroUI.
- **Glassy** — no frosted glass surfaces, translucent panes, blur layers, or luminous cards.
- **Atmospheric** — no decorative gradient backgrounds, glow fields, or ambient orbs.
- **Over-designed** — no custom shadows, radii, borders, or motion systems for ordinary components.
- **Theme-invented** — no custom light/dark color system replacing HeroUI's built-in themes.
- **Wrapper-driven** — no local design-system primitive whose main purpose is to restyle HeroUI.
- **Patchworked** — no mixture of old custom design classes and default HeroUI components.
- **One-off styled** — no component-specific visual improvisation that makes one screen look different from another.

---

## Required Visual Outcome

The product should feel:

- **Framework-native** — visibly standard HeroUI.
- **Predictable** — controls behave and appear as documented.
- **Plain in a good way** — no unnecessary visual drama.
- **Maintainable** — easy to upgrade with HeroUI because local visual overrides are absent.
- **Consistent** — consistency comes from the framework, not from a custom CSS layer.
- **Accessible** — standard component accessibility is preserved and completed through proper props and semantics.
- **Quiet** — the UI gets out of the user's way.

---

## Opt-Out Principles

These rules are the default.

A component may deviate only when there is a specific functional requirement that cannot be satisfied through standard HeroUI components, standard HeroUI props, or non-visual structural layout.

A valid reason names the rule being overridden and explains why the component genuinely cannot follow it. "Looks better", "matches old Vitarish", "adds identity", "feels premium", or "the screen is too empty" are not valid reasons.

Deviations are exceptions, not new defaults. They must not introduce a reusable custom visual language.
