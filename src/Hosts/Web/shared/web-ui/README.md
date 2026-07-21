# @vitarish/web-ui

Shared React Web UI package for the Vitarish Web host family.

## Purpose

This package provides presentation-only artifacts shared across Web apps. It is the source of truth for reusable React presentation artifacts inside the Web host family.

## What belongs here

- Design tokens and Web theme bridge utilities
- Documented raw HeroUI and HeroUI Pro composition patterns
- Shared empty, loading, error, navigation, and form composition contracts
- Icon usage helpers and presentation-only composition helpers
- Reusable component contracts and examples when direct HeroUI usage is not enough

## What does NOT belong here

- Generated API clients, DTOs, query keys, or `apiFetch`
- React Query hooks or feature data hooks
- Backend projects or domain logic
- App routes, pages, or feature components
- App localization catalogs or providers
- Non-Web host implementation files
- Custom design-system wrappers that hide or replace HeroUI's component model

## Promotion rules

A UI artifact may move into this package only when it satisfies all of these conditions:

1. It is Web presentation-only.
2. It has no generated API, React Query, route, backend, host runtime, or feature workflow dependency.
3. Its public props use generic UI language instead of business nouns.
4. It can be tested without feature data or backend contracts.
5. It improves reuse across at least two Web modules or apps, or protects a major Web design artifact from duplication.
6. It does not create a custom wrapper around a raw HeroUI or HeroUI Pro component that should be used directly.

## Usage

```tsx
import { DashboardFrame, EmptyStateSurface, LoginPanel } from "@vitarish/web-ui";
import {
  PageContainer,
  PublicDocumentShell,
  RevealHeader,
  StandaloneDocumentPage,
} from "@vitarish/web-ui/layout";
```

Do not deep-import from `@vitarish/web-ui/src/**`. Use only public exports.

Import `@vitarish/web-ui/layout.css` exactly once from each app's root global stylesheet.

## Reveal headers

`RevealHeader` renders a separate compact header pinned within the nearest shared page or workspace layout. Place its `Boundary`
where the compact representation should take over; it does not own the hero, primary header, or
page content.

```tsx
<RevealHeader compactContent={<CompactHeader />} />

<Hero>
  <PrimaryHeader />
  <RevealHeader.Boundary />
</Hero>

<MainContent />
```

It observes document scrolling by default, detects an inherited workspace scroll owner, or accepts
an explicit `scrollRoot`. `positioning="overlay"` is the default; use `positioning="inset"` to
reserve the compact header's height. Within `ContainedPage` and `ImmersiveRegion`, it stays inside
the available layout boundary rather than covering dashboard chrome. Hidden headers are inert and
hidden from assistive technology.

## Layout selection

| Surface | Composition | Primary page-axis scroll owner |
| --- | --- | --- |
| Public or marketing document | `PublicDocumentShell` + `StandaloneDocumentPage`; sections use `PageContainer` | Browser document |
| Standalone auth, access, session, or loading state | `StandaloneCenteredPage` | Browser document |
| Normal dashboard page | `DashboardFrame(contentMode="page")` + `ContainedPage` | HeroUI AppLayout main |
| Framed gate or centered dashboard state | `DashboardFrame(contentMode="page")` + `CenteredRegion` | HeroUI AppLayout main |
| Dashboard tool that owns its full available height | `DashboardFrame(contentMode="workspace")` + `ImmersiveRegion` | Workspace root or intentional panes |
| Standalone full-height tool | `StandaloneImmersivePage` | Workspace root or intentional panes |

Choose `PageWidth` before composing content: `compact` for narrow forms, `reading` for prose, `content` for normal pages, and `fluid` for deliberately wide content. `PageContainer` and `ContainedPage` default to `content`.

The three scroll models are distinct:

- Document layouts grow with content and let the browser scroll.
- Frame-page layouts make HeroUI AppLayout's main region the only page scroller.
- Workspace layouts keep the outer frame fixed and give overflow to the designated workspace root or panes.

Framed pages must not create horizontal viewport scrolling at any breakpoint. The shared frame clips accidental inline overflow at the page boundary; feature controls must still reflow or otherwise remain operable within the available inline size. Intentional horizontal workspace panes remain an explicit workspace concern.

## HeroUI ownership boundary

HeroUI Pro `AppLayout` is the sole dashboard owner of `<main>`, sidebar state, responsive navigation, focus behavior, breakpoints, and the optional contextual aside. `DashboardFrame` only selects Vitarish page/workspace policy and supplies native HeroUI composition. Apps and feature packages must not import `AppLayout` directly, create a second dashboard `<main>`, measure the viewport in JavaScript, or recreate sidebar/aside behavior.

Standalone layout primitives own `<main>`; `ContainedPage`, `CenteredRegion`, and `ImmersiveRegion` are neutral regions intended for an existing `DashboardFrame`.

Representative compositions:

```tsx
<PublicDocumentShell header={<Header />} footer={<Footer />}>
  <StandaloneDocumentPage>
    <PageContainer width="reading">{content}</PageContainer>
  </StandaloneDocumentPage>
</PublicDocumentShell>

<DashboardShell contentMode="page">
  <ContainedPage width="content">{content}</ContainedPage>
</DashboardShell>

<DashboardShell contentMode="workspace">
  <ImmersiveRegion>{workspace}</ImmersiveRegion>
</DashboardShell>
```

## Current shared exports

- Auth presentation: `AuthMethodList`, `LoginPanel`.
- Account/settings presentation: `SettingsSurface`, `SettingsSection`, `SettingsBackLink`, `OptionSelector`.
- Dashboard shell presentation: `DashboardFrame`, `IconButton`.
- Responsive layout policy: document, contained, centered, and immersive primitives plus `PageWidth`.
- Feedback and state presentation: `StatusAlert`, `LoadingStateSurface`, `EmptyStateSurface`, `ErrorStateSurface`.
- Chat presentation: `ChatEmptyState`, `ChatLoadingMessage`.

Apps keep route constants, localized copy, session/auth hooks, generated clients, React Query hooks, and workflow decisions in the app package. They pass ready-to-render labels, state, handlers, and navigation targets into these shared components.
