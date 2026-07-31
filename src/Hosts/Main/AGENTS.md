# Main host agent routing

This host is an outer composition and runtime boundary. Do not add domain or
use-case behavior here to avoid changing its owner.

| Touched decision | Load |
| --- | --- |
| HTTP/OpenAPI endpoints, host routes, DTO mapping, WebSocket, or SSE | `platform-transport-contract` |
| Authentication, authorization, cookies, antiforgery, CORS, tenant isolation, privacy, or secrets | `platform-trust-boundary` |
| Host-owned persistence, migrations, or persisted configuration formats | `platform-data-evolution` |
| Host-owned remote provider adapter | `platform-external-integration` |
| Composition boundaries, project references, placement, or ADR need | `platform-artifact-architecture` |

Do not load `platform-backend-use-case`, React, Web-route, or Web-layout skills
for backend host wiring. Move business behavior to its owning App, Module, or
Identity Application/Domain slice.

## Native AOT readiness

- **[MUST]** Apply the root Native AOT readiness rule. Keep host composition,
  endpoint mapping, dependency registration, configuration, and JSON metadata
  explicit or generated.
