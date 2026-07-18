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
```

Do not deep-import from `@vitarish/web-ui/src/**`. Use only public exports.

## Icons

The authoritative Web icon policy is defined in [`../../AGENTS.md`](../../AGENTS.md). Interface icons use Font Awesome CSS classes, with the actual Font Awesome icon name visible in the source:

```tsx
import { FaIcon } from "@vitarish/web-ui";

<FaIcon aria-hidden="true" className="size-6" icon="users" />
```

A plain `<i aria-hidden="true" className="fa-solid fa-users" />` is also acceptable for simple local markup. Do not paste SVG paths or use SVG icon components. Keep SVG for explicitly reviewed non-icon artwork such as logos and illustrations.

## Current shared exports

- Auth presentation: `AuthMethodList`, `LoginPanel`.
- Account/settings presentation: `SettingsSurface`, `SettingsSection`, `SettingsBackLink`, `OptionSelector`.
- Dashboard shell presentation: `DashboardFrame`, `IconButton`.
- Feedback and state presentation: `StatusAlert`, `LoadingStateSurface`, `EmptyStateSurface`, `ErrorStateSurface`.
- Chat presentation: `ChatEmptyState`, `ChatLoadingMessage`.

Apps keep route constants, localized copy, session/auth hooks, generated clients, React Query hooks, and workflow decisions in the app package. They pass ready-to-render labels, state, handlers, and navigation targets into these shared components.
