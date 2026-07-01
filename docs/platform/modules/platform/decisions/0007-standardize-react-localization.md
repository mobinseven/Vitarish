# ADR-0007: Standardize React Localization

Date: 2026-07-01  
Status: Accepted  
App: Web Host  
Module: Platform  
Capability: React localization  
Owner: Platform Architecture  
Related apps: Web  
Related modules: Platform, Authentication, Account, Journey, Clinic, Knowledge  
Supersedes: None  
Superseded by: None  
Related:
- docs/platform/modules/platform/decisions/0003-isolate-module-interface-libraries-with-api-client-contracts.md
- docs/platform/modules/platform/decisions/0005-standardize-openapi-endpoint-conventions.md

## Context

The legacy Blazor UI used `IStringLocalizer` as its presentation-layer localization boundary. The React Web host does not have an equivalent mechanism, so user-facing English strings have accumulated across pages, components, feature UI, metadata, empty states, validation messages, and error displays.

Hard-coded copy makes language support inconsistent, makes locale-sensitive formatting difficult to validate, and risks leaking backend `ProblemDetails` text directly to users. The Web host also consumes generated OpenAPI clients, so localization must not be added to generated transport artifacts or backend contracts.

## Decision

The Web host owns a central TypeScript localization catalog and React provider under `src/Hosts/Web/src/localization/**`.

Locale resolution is centralized in the Web localization module. UI formatting uses shared `Intl`-based helpers from the same boundary. React presentation components consume localized messages through the provider/hook or metadata helper rather than declaring feature-local English copy.

Backend, application, API, and contract layers expose stable result, status, and error codes rather than translated display text. React maps those stable codes to localized UI messages at the presentation boundary.

The initial approved locales are Persian (`fa`, default) and English (`en`). Persian is the default because the primary user base is Persian-speaking. English is the secondary locale for international accessibility. The `defaultLocale` is `fa`, and locale direction is `rtl` for Persian, `ltr` for English.

## Boundaries

Allowed dependencies:

- React presentation components, Web-host pages, and UI-facing adapters may consume `src/Hosts/Web/src/localization/**`.
- Tests that render localized UI may use localization-aware render helpers.
- Feature-specific UI adapters may map stable API enum/status values to localization keys.

Forbidden dependencies:

- Generated clients, generated DTOs, generated query keys, and `apiFetch` must remain transport/infrastructure and must not translate user-facing text.
- Domain, Application, Contracts, backend Infrastructure, and OpenAPI generation output must not depend on Web localization.
- Backend APIs must not return translated strings as their default user-facing contract.

## Consequences

All React UI copy migrates to catalog keys. New React UI changes require static localization validation, and component tests should render through localization-aware helpers. Page metadata must resolve strings from the catalog. Dates, times, numbers, currencies, percentages, lists, and plural-sensitive messages must use central localization helpers.

The initial approved locales are Persian (`fa`, default) and English (`en`). Additional locales are explicit future additions that require catalog coverage, validation, and direction configuration.

## Alternatives Considered

### Keep hard-coded English strings

Rejected. It preserves the current inconsistency and prevents reliable locale validation.

### Feature-local English constants

Rejected. Feature-local constants create multiple sources of truth for product copy and duplicate shared actions, states, errors, and metadata.

### Return translated strings from backend APIs

Rejected. Backend translation couples API/application behavior to presentation language and makes generated clients carry display concerns.

### Copy Blazor resource patterns directly into React

Rejected. React needs a Web-owned TypeScript catalog/provider boundary rather than direct reuse of Blazor `IStringLocalizer` patterns.

### Translate errors inside `apiFetch`

Rejected. `apiFetch` is transport infrastructure. It may normalize error shape, but presentation code maps stable codes to localized messages.

## Validation / Enforcement

- React localization validation reports hard-coded UI strings under `src/Hosts/Web/src/app/**`, `components/**`, `features/**`, and `design-system/**`.
- Tests cover locale resolution, catalog completeness, and API error code mapping.
- API boundary validation protects generated clients and transport infrastructure.
- Architecture validation protects frontend/backend isolation.

## Notes

This ADR establishes the React localization boundary and migration direction. The initial scope includes Persian (`fa`, default) and English (`en`). Additional locales beyond these require catalog coverage, validation, and an explicit ADR update.
