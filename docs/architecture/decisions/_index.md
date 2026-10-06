# Global Architecture Decision Records

Status: Active  
Owner: Platform Architecture  
Scope: Global architecture governance  
Last reviewed: 2026-07-11

This index is the authoritative navigation point for global ADRs. Scoped platform module decisions remain in their owning `docs/platform/modules/<module>/decisions/` folders. Scoped app decisions remain in flat app-owned `docs/apps/<app>/decisions/` folders.

## Current global decisions

| ADR | Title | Status | Date |
|---|---|---|---|
| [0001](0001-record-architecture-decisions.md) | Record Architecture Decisions | Accepted | YYYY-MM-DD |
| [0002](0002-persist-instants-as-utc-datetime.md) | Persist instants as UTC DateTime | Accepted | YYYY-MM-DD |
| [0003](0003-isolate-module-interface-libraries-with-api-client-contracts.md) | Isolate module Interface libraries with API, Client, and Contracts projects | Accepted | 2026-06-24 |
| [0004](0004-protect-internal-module-api-endpoints-by-default.md) | Protect internal module API endpoints by default | Accepted | 2026-06-25 |
| [0005](0005-standardize-openapi-endpoint-conventions.md) | Standardize OpenAPI Endpoint Conventions | Accepted | 2026-06-27 |
| [0006](0006-standardize-platform-journey-routing.md) | Standardize Platform Journey Routing | Accepted | 2026-06-29 |
| [0007](0007-standardize-react-localization.md) | Standardize React Localization | Accepted | 2026-07-01 |
| [0008](0008-standardize-app-experience-boundaries.md) | Standardize App Experience Boundaries | Accepted | 2026-07-09 |
| [0009](0009-decommission-blazor-interfaces.md) | Decommission Blazor/Razor Component UI and Interface Projects | Accepted | 2026-07-10 |
| [0010](0010-prefer-statically-analyzable-native-aot-compatible-design.md) | Prefer Statically Analyzable and Native AOT-Compatible Design for the .NET Backend | Accepted | 2026-07-11 |
| [0011](0011-server-side-agents-sdk-runtime.md) | Run Clinic agents through a server-side OpenAI Agents SDK runtime | Accepted | 2026-07-29 |
| [0012](0012-module-owned-agents-with-guarded-composition.md) | Module-owned agents with guarded composition | Accepted | 2026-08-12 |
| [0013](0013-separate-e2e-host-with-shared-hosting-composition.md) | Separate E2E host with shared hosting composition | Accepted | 2026-08-15 |
| [0014](0014-centralize-product-file-lifecycles-in-files.md) | Centralize Product File Lifecycles in Files | Accepted | 2026-09-29 |

## Related global architecture documents

- [ADR-0003 implementation conventions](../conventions/adr-0003-implementation-conventions.md)
- [OpenAPI endpoint conventions](../conventions/openapi-endpoint-conventions.md)
- [ADR-0003 interface boundary validation](../validation/adr-0003-interface-boundary-validation.md)
- [ADR-0010 static analyzability and Native AOT engineering standard](../conventions/adr-0010-static-analyzability-native-aot.md)

## Superseded or deprecated decisions

| ADR | Title | Status | Replaced By | Date |
|---|---|---|---|---|
| [0003](0003-isolate-module-interface-libraries-with-api-client-contracts.md) | Isolate module Interface libraries with API, Client, and Contracts projects | Superseded (UI portion only) | [0009](0009-decommission-blazor-interfaces.md) | 2026-07-10 |
