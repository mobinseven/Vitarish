# App agent routing

Applies to .NET app aggregators and app-owned backend slices. This directory is
not the React Web workspace.

| Touched decision | Load |
| --- | --- |
| Domain/Application/Core behavior, commands, queries, workflows, or ports | `platform-backend-use-case` |
| API, Contracts, routes, DTOs, Client transport, or OpenAPI | `platform-transport-contract` |
| Infrastructure persistence, persisted formats, or migrations | `platform-data-evolution` |
| Remote provider adapter | `platform-external-integration` |
| Authentication, authorization, tenant ownership, privacy, or untrusted input | `platform-trust-boundary` |
| App/module placement, dependency direction, shared ownership, or ADR need | `platform-artifact-architecture` |

Do not load React, Web-route, or Web-layout skills unless the change also
touches `src/Hosts/Web`. Keep app composition free of module business rules.

## Native AOT readiness

- **[MUST]** Apply the root Native AOT readiness rule. Keep app aggregation
  explicit or generated; isolate incompatible infrastructure and dynamic
  behavior behind its owned boundary.
