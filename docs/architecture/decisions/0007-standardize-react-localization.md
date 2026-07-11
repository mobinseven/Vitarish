# ADR-0007: Standardize React Localization

Date: 2026-07-01  
Status: Accepted  
App: Web Host  
Module: Platform  
Capability: React localization  
Owner: Platform Architecture  
Related apps: Web  
Related modules: Platform, Authentication, Account, Journey, Clinic, Knowledge, Sea
Supersedes: None  
Related:
- docs/architecture/decisions/0003-isolate-module-interface-libraries-with-api-client-contracts.md
- docs/architecture/decisions/0005-standardize-openapi-endpoint-conventions.md

## Context

The legacy Blazor UI used `IStringLocalizer` as its presentation-layer localization boundary. The React Web host does not have an equivalent mechanism, so user-facing English strings have accumulated across pages, components, feature UI, metadata, empty states, validation messages, and error displays.

Hard-coded copy makes language support inconsistent, makes locale-sensitive formatting difficult to validate, and risks leaking backend `ProblemDetails` text directly to users. The Web host also consumes generated OpenAPI clients, so localization must not be added to generated transport artifacts or backend contracts.

ADR-0008 now requires React Web workflow feature code to move from app feature folders into Web workspace feature packages under `src/Hosts/Web/shared/<feature>-web`. A single app-owned catalog would make those packages depend on the first app and would turn app composition into the source of truth for feature copy. App-owned route/shell surfaces such as `home` and `landing` may keep route-composition copy outside feature packages.

## Decision

React localization is composed through Web workspace packages instead of one app-owned central catalog.

Create a core Web localization package under the Web workspace, named `@vitarish/web-localization` unless a later implementation ADR chooses a narrower name. The package owns locale identities, default locale, text direction, provider/context, message registration contract, metadata helpers, and `Intl`-based formatting helpers for dates, times, numbers, currencies, percentages, lists, and plural-sensitive messages.

Each Web feature package owns its message namespace and localized catalogs for its feature UI. For example, `@vitarish/account-web` owns account messages, `@vitarish/clinic-web` owns clinic messages, and `@vitarish/sea-web` owns Sea messages. Feature packages register or export their namespace through the core localization package contract. Web apps compose the provider with the feature namespaces used by their routes.

React presentation components consume localized messages through the provider/hook, namespace helpers, or metadata helper rather than declaring hard-coded display copy. App route files may own only route-composition copy that is not part of a feature package; they must not recreate feature catalogs.

Backend, application, API, and contract layers expose stable result, status, and error codes rather than translated display text. React maps those stable codes to localized UI messages at the presentation boundary.

The initial approved locales are Persian (`fa`, default) and English (`en`). Persian is the default because the primary user base is Persian-speaking. English is the secondary locale for international accessibility. The `defaultLocale` is `fa`, and locale direction is `rtl` for Persian, `ltr` for English.

## Boundaries

Allowed dependencies:

- React presentation components, Web-host pages, and UI-facing adapters may consume `@vitarish/web-localization` provider, hooks, metadata helpers, and formatting helpers.
- Web feature packages may own package-local message namespaces and catalogs.
- Web apps may compose the localization provider with the feature namespaces used by their route tree.
- Tests that render localized UI may use localization-aware render helpers.
- Feature-specific UI adapters may map stable API enum/status values to localization keys in the owning feature namespace.

Forbidden dependencies:

- Generated clients, generated DTOs, generated query keys, and `apiFetch` must remain transport/infrastructure and must not translate user-facing text.
- Domain, Application, Contracts, backend Infrastructure, and OpenAPI generation output must not depend on Web localization.
- Backend APIs must not return translated strings as their default user-facing contract.
- Localization catalogs must not contain business rules, authorization rules, validation ownership, route ownership, or generated DTO/client behavior.
- Generated clients and DTOs must not import localization packages, message catalogs, or UI components.

## Consequences

All React UI copy migrates to localization namespaces. New React UI changes require static localization validation, and component tests should render through localization-aware helpers. Page metadata must resolve strings from the composed localization boundary. Dates, times, numbers, currencies, percentages, lists, and plural-sensitive messages must use the core localization helpers.

The initial approved locales are Persian (`fa`, default) and English (`en`). Additional locales are explicit future additions that require namespace coverage, validation, and direction configuration.

## Alternatives Considered

### Keep hard-coded English strings

Rejected. It preserves the current inconsistency and prevents reliable locale validation.

### App-owned central TypeScript catalog

Rejected. A single app catalog was acceptable before feature packages became the mandatory React Web owner. It now makes the first app a feature-copy source of truth and blocks package-level ownership.

### Feature-local English constants

Rejected when they are disconnected from the localization boundary. Feature packages may own localized message namespaces, but not unvalidated English constants.

### Return translated strings from backend APIs

Rejected. Backend translation couples API/application behavior to presentation language and makes generated clients carry display concerns.

### Copy Blazor resource patterns directly into React

Rejected. React needs Web workspace package composition rather than direct reuse of Blazor `IStringLocalizer` patterns.

### Translate errors inside `apiFetch`

Rejected. `apiFetch` is transport infrastructure. It may normalize error shape, but presentation code maps stable codes to localized messages.

## Validation / Enforcement

- React localization validation reports hard-coded UI strings under `src/Hosts/Web/apps/*/src/app/**`, app-local UI folders, and `src/Hosts/Web/shared/*/src/**`, excluding generated artifacts.
- Tests cover locale resolution, namespace/catalog completeness, formatting helpers, and API error code mapping.
- API boundary validation protects generated clients and transport infrastructure.
- Architecture validation protects frontend/backend isolation.

## Notes

This ADR establishes the React localization boundary and migration direction. The initial scope includes Persian (`fa`, default) and English (`en`). Additional locales beyond these require namespace coverage, validation, and an explicit ADR update.
