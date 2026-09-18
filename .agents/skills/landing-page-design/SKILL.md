---
name: landing-page-design
description: Design or review public landing, marketing, product-launch, and homepage surfaces using an Apple-like narrative structure and HeroUI/HeroUI Pro primitives. Use for visual landing-page work; do not use for authenticated dashboard or workflow screens.
---

# Landing Page Design

Use this skill for public-facing landing pages in the Vitarish Web workspace or similar React/HeroUI work. The target is a persuasive, editorial page—not a dashboard made from a grid of cards.

## Outcome

Produce a landing-page design or implementation plan that explains:

- the visitor narrative and section order;
- the visual role of each section and its responsive behavior;
- the HeroUI/HeroUI Pro primitive selected for each interaction;
- the custom media, layout, typography, and motion that HeroUI does not provide;
- localization, RTL, accessibility, and visual-proof requirements;
- non-goals and deferred interactions.

If implementation is requested, implement the smallest complete slice and verify it with an inspected desktop and mobile screenshot. Do not imply that a component/typecheck pass proves visual fidelity.

## First-pass workflow

1. Read the applicable `AGENTS.md` files and inspect only the relevant landing route, shared marketing UI, theme CSS, localization catalog, assets, and existing tests. Preserve unrelated dirty work.
2. Classify the surface as public marketing, product launch, portfolio/editorial, or authenticated workflow. Keep public marketing composition separate from dashboard/workspace shells unless the product explicitly requires both.
3. Define the narrative before selecting components. A strong default is:

   `header → hero promise → product/visual reveal → feature stories → interactive proof → comparison or trust → FAQ/disclosure → final CTA → footer`

   Remove sections that do not advance the visitor's decision.
4. Use the Apple-style design lens: one dominant idea per section, generous whitespace, large optical type hierarchy, full-bleed or art-directed media, alternating visual pacing, restrained surfaces, and progressive disclosure for secondary detail.
5. Build a section-to-component map using [references/component-map.md](references/component-map.md). Reuse existing repository marketing primitives when they fit; otherwise compose raw HeroUI/HeroUI Pro directly.
6. Before writing component code, verify the exact installed API. Call the HeroUI MCP `list_components` first, then `get_component_docs` for every selected component. The current MCP is HeroUI v3 only; never infer v2 props or compound anatomy from memory. If a Pro component is used, inspect the installed/local Pro contract as well.
7. Separate responsibilities:

   - app/feature owns route, copy, media, analytics, actions, and localization keys;
   - shared UI owns presentation-only primitives or documented compositions;
   - HeroUI owns standard interaction semantics and accessibility;
   - CSS/layout owns art direction, section geometry, aspect ratios, and responsive composition;
   - a centralized motion layer owns reveal/scroll enhancement.

8. Validate the visual result with the Web screenshot workflow when working in Vitarish (`src/Hosts/Web/scripts/screenshot.ps1`) and inspect the generated image. Visual-only work does not require the real two-host E2E proof; API/auth/tenant/persistence changes do.

## Component rules

- Prefer raw HeroUI/HeroUI Pro components and existing compositions. Do not introduce wrappers whose only purpose is to rename or conceal `Card`, `Button`, `Disclosure`, `Navbar`, or another HeroUI primitive.
- Use `ViewportReveal` from `@vitarish/ui` for ordinary viewport-entry reveals. Do not recreate its `IntersectionObserver`, `opacity`, or `translateY` behavior in a landing feature.
- Use `Navbar`/`Button`/`Link` for the public header and CTAs; use `Drawer` or the Pro navbar menu behavior for small screens.
- Use `Card`, `Surface`, `ItemCard`, or `ItemCardGroup` for bounded content with a clear grouping purpose. Do not turn every editorial section into a card.
- Use `Tabs` or `ToggleButtonGroup` only for parallel views that the visitor may actively switch between; do not hide the primary narrative in tabs.
- Use `Carousel`/`ScrollShadow` for genuinely scrollable media or product rails. A decorative horizontal row must not pretend to be a carousel.
- Use `Accordion`, `DisclosureGroup`, and `Disclosure` for FAQ, specifications, feature detail, and other secondary content. The existing private-practice landing work treats disclosure-style progressive disclosure as a normal landing-page pattern; preserve that intent while checking the live API. Keep high-value proof and the main CTA visible.
- Use `Tooltip` for icon-only controls and follow the host's Font Awesome icon convention. Decorative icons are hidden from assistive technology.
- Use semantic HTML headings, sections, lists, landmarks, and links even when a HeroUI surface is present. A component catalog is not an information architecture.
- Keep buttons on a dedicated CTA line below explanatory copy. Use filled/primary only for the main action and outline/ghost/secondary for escape or supporting actions.

## Motion and visual craft

Use one declarative, progressive-enhancement motion system for ordinary reveals and a deliberately isolated art-direction layer for exceptional scenes. Do not create a separate animation script for each section. Motion should clarify hierarchy and continuity, not delay access to content.

### Standard section reveal

Every ordinary content section should use the same reveal contract:

```text
section enters viewport
        ↓
opacity 0 → 1
Y 24px → 0
        ↓
section settles
```

In Vitarish, use `ViewportReveal` from `@vitarish/ui` for this contract. It owns the shared viewport observation, CSS transition, delay, semantic element selection, and unsupported-observer fallback. Implement the behavior with CSS/`view()` timelines only when the page has a documented browser-support reason to do so; do not recreate the observer or transition in the consuming page. Keep the section visible when JavaScript, the observer, or scroll-timeline support is unavailable. Do not add GSAP or a bespoke observer to implement this basic effect.

### Art-directed scroll scenes

For ordinary reveals, let the shared primitive own pacing: an inset viewport entry line, an 800ms opacity fade, and a 1000ms decelerating 24px settle. Do not apply the steep movement curve to opacity: it makes the fade appear to pop. Record several seconds with a pause before entry and after settling, and inspect intermediate frames as well as endpoints. A fast scroll-to-bottom clip does not establish motion quality.

Use a separate scroll-scene contract only when the section needs spatial choreography that a normal reveal cannot express:

```text
hero
  ↓
product remains sticky
  ↓
scroll
  ↓
product scale / movement
  ↓
copy fades in
  ↓
next scene
```

Prefer native CSS `scroll()` and `view()` timelines for simple scroll-linked progress, transforms, and viewport-entry effects. Apple presented these timelines in the WWDC25 Safari/WebKit session as a way to connect animation to scrolling without JavaScript. Treat them as progressive enhancement and verify the supported browser matrix before relying on them.

Use GSAP `ScrollTrigger` only for genuinely art-directed scenes that require coordinated pinning, scrubbed multi-property sequencing, scene handoff, or timeline control that native CSS cannot express cleanly. Keep GSAP code isolated to that scene, scope it to the component lifecycle, clean up triggers on unmount, and do not turn the whole landing page into an animation framework. Do not add GSAP as a dependency merely for ordinary reveals.

- Prefer transform/opacity and compositor-friendly properties.
- Keep motion interruptible for gesture-driven interactions.
- Use restrained, critically damped motion by default; reserve bounce for momentum-driven gestures.
- Respect `prefers-reduced-motion`, `prefers-reduced-transparency`, and `prefers-contrast`.
- Preserve a usable static page when JavaScript or the observer is unavailable.
- For RTL, mirror directional movement and directional icons; do not blindly mirror upload, download, undo, trend, or vertical movement symbols.

## Vitarish Web constraints

- Follow the repository rule that raw HeroUI/HeroUI Pro remains the default and `@vitarish/ui` must not become a hidden design-system replacement.
- Reuse the existing shared marketing owner at `src/Hosts/Web/shared/ui/src/marketing.tsx` before adding another pattern. Its current compositions include marketing header/action links, hero, section heading, feature item cards, pricing cards, feature carousel, final CTA, and footer.
- Keep `home`/`landing` route composition app-owned. Shared UI receives localized display values and must not resolve app routes, backend URLs, workflow decisions, or localization catalogs.
- Define every displayed string in the owning app/feature catalog and run `npm run validate:localization` from `src/Hosts/Web`.
- Use Font Awesome CSS classes or `FaIcon` for interface icons. Do not embed SVG path data for interface icons; SVG remains allowed for logos, illustrations, diagrams, and data visualizations.
- For visual implementation, use `scripts/screenshot.ps1` from the Web host root and inspect the screenshot. Report unrun checks explicitly.

## Review checklist

Before handoff, confirm:

- [ ] The page has a clear visitor decision and narrative order.
- [ ] Each section has one primary purpose and one dominant visual focus.
- [ ] The component choice is mapped to a verified HeroUI v3 or installed Pro API.
- [ ] Disclosure/accordion content is secondary and keyboard accessible.
- [ ] Media has intentional crop, loading, alt text, and mobile behavior.
- [ ] CTAs have a clear hierarchy and no redundant decorative icons.
- [ ] Copy is localized; RTL layout, directional icons, and dates are handled centrally.
- [ ] Reduced-motion and no-JavaScript behavior remain usable.
- [ ] Ordinary sections use the standardized opacity/Y reveal; GSAP is absent unless a scene is genuinely art-directed.
- [ ] Ordinary sections use the shared `ViewportReveal` primitive rather than page-local reveal code.
- [ ] Native `scroll()`/`view()` timelines are considered before JavaScript for scroll-linked effects.
- [ ] Existing shared marketing primitives and dirty files were inspected before adding new code.
- [ ] Visual proof was captured and inspected at relevant desktop/mobile widths.

Read [references/component-map.md](references/component-map.md) when selecting a section pattern or deciding whether a visual requires custom layout instead of another HeroUI surface.
