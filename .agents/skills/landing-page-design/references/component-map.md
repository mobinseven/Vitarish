# Landing Section and Component Map

This is a selection guide, not a replacement for the live HeroUI MCP. Verify exact APIs before implementation.

## Narrative map

| Landing need | Preferred composition | Custom responsibility |
| --- | --- | --- |
| Public navigation | HeroUI Pro `Navbar`, HeroUI `Button`, `Link`, `Tooltip` | sticky/translucent treatment, brand lockup, responsive menu composition |
| Hero promise | semantic `h1`/`p`, `Button`, `Link`, optional `Typography` | type scale, max-width, hero art, split/centered geometry |
| Product reveal | `Card` or `Surface` only when a bounded surface helps | full-bleed media, object position, device/product staging, loading |
| Feature story | `ItemCard`/`ItemCardGroup` for compact benefits; `Card` for bounded proof | alternating editorial layout, visual rhythm, media/text relationship |
| Parallel feature views | `Tabs` or `ToggleButtonGroup` | synchronize selected content and media; keep primary story visible |
| Media/product rail | HeroUI Pro `Carousel`, `ScrollShadow`, `Button` | snap behavior, item widths, edge fades, mobile overflow |
| Comparison/specification | `Table`, `Tabs`, `Chip`, `Badge` | responsive table-to-stack transformation, semantic labels |
| Trust or social proof | `Card`, `Avatar`, `AvatarGroup`, `Typography` | quote hierarchy, logo treatment, restrained trust signals |
| FAQ and secondary details | `Accordion`, `DisclosureGroup`, `Disclosure` | answer grouping, initial state, URL/deep-link behavior if needed |
| Final conversion | `Card` or `Surface` plus `Button`/`Link` | CTA hierarchy, contrast, full-bleed treatment, form handoff |
| Footer | `Link`, `Separator`; `DisclosureGroup` on mobile when useful | desktop columns, mobile grouping, legal/localization content |

## Verified HeroUI v3 shortlist

The live MCP catalog includes these relevant OSS components:

`Accordion`, `Avatar`, `AvatarGroup`, `Badge`, `Button`, `ButtonGroup`, `Card`, `Chip`, `Disclosure`, `DisclosureGroup`, `Drawer`, `Link`, `Modal`, `Popover`, `ScrollShadow`, `Separator`, `Surface`, `Table`, `Tabs`, `ToggleButton`, `ToggleButtonGroup`, `Tooltip`, and `Typography`.

The catalog is broader than this shortlist and is authoritative for availability. The MCP workflow is:

1. `list_components`
2. `get_component_docs({ components: [...] })`
3. optionally inspect source/styles only when composition or customization is unclear

The current MCP documents HeroUI v3. Do not copy v2 examples or assume flat props where v3 uses compound components.

## Relevant local HeroUI Pro patterns

The Vitarish Web workspace currently contains or uses:

- `Navbar`
- `ItemCard` and `ItemCardGroup`
- `Carousel`
- `PromptInput`
- `ListView`
- `EmptyState`
- `AppLayout`, `Sidebar`, and `Navbar` for application shells

Check the installed `@heroui-pro/react` package and the existing usage site before introducing a new Pro component. Keep marketing content and routes in the consuming app.

## Disclosure guidance

Use disclosure-style components when content is useful but not part of the first reading path:

- FAQ answers;
- long feature explanations;
- technical specifications;
- pricing-plan detail;
- mobile footer navigation;
- optional onboarding or eligibility detail.

Use a `DisclosureGroup` when several disclosures form one semantic collection and their opening behavior should be coordinated. Use individual `Disclosure` when the item stands alone or belongs to a different visual section. Use `Accordion` when the component's accordion semantics and visual treatment are the better fit.

Do not use disclosure to hide:

- the primary value proposition;
- the main product image or demonstration;
- required legal/consent text;
- the principal CTA;
- critical error or security state.

Always verify the current HeroUI API and ensure headings, keyboard operation, focus visibility, and expanded state are exposed correctly.

## Existing Vitarish shared marketing owner

Before adding a new shared composition, inspect:

`src/Hosts/Web/shared/ui/src/marketing.tsx`

Current reusable compositions include:

- `MarketingHeader`
- `MarketingActionLink`
- `MarketingHero`
- `MarketingSection`
- `MarketingSectionHeading`
- `FeatureItemCard`
- `FeatureItemGroup`
- `PricingCard` and `PricingGrid`
- `FeatureCarousel`
- `MarketingFinalCta`
- `MarketingFooter`

Reuse these when their responsibility matches. Do not expand them merely to hide a one-off page layout or to create a second design-system API.

## Custom-layout boundary

HeroUI is not the source of truth for:

- Apple-style art direction;
- full-bleed image composition;
- device/product mockups;
- section height and narrative pacing;
- responsive crop and focal point;
- scroll-driven reveal choreography;
- editorial typography;
- brand-specific gradients, textures, or diagrams.

Keep those concerns in the landing feature's semantic markup and CSS, with one centralized motion enhancement layer where needed.

## Motion standard

### Default reveal for ordinary sections

Use one shared reveal behavior for normal sections:

```text
section enters viewport
        ↓
opacity 0 → 1
Y 24px → 0
        ↓
section settles
```

The implementation order is:

1. static content is usable without motion;
2. Vitarish uses `ViewportReveal` from `@vitarish/ui` for ordinary sections;
3. CSS `view()` timelines are considered for supported browsers when a page has a documented reason to bypass the shared primitive;
4. `prefers-reduced-motion: reduce` disables translation and keeps only a gentle opacity/static state.

Do not create per-section observers, duplicate the shared reveal CSS, or import GSAP for this effect.

### Exceptional art-directed scene

Reserve a scroll scene for a product or hero sequence with a real spatial relationship:

```text
hero → sticky product → scroll → product scale/movement → copy fade-in → next scene
```

Use native CSS `scroll()` or `view()` timelines when the scene is a simple scroll-linked transform or visibility progression. Use GSAP `ScrollTrigger` only when the scene genuinely needs pinned choreography, scrubbed multi-property sequencing, or controlled handoff between scenes that native CSS cannot express clearly. Keep the trigger lifecycle local, clean it up on unmount, and isolate the dependency to the art-directed feature.

The page should still render as a coherent static sequence if native timelines or GSAP are unavailable. Do not make ordinary section order, copy, product identity, or primary CTA dependent on animation.
