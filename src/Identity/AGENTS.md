# Identity agent routing

Identity changes are security-sensitive. Load `platform-trust-boundary` for any
change that can affect identity, authentication, authorization, credentials,
tokens, clients, tenant isolation, privacy, or untrusted input.

| Additional touched decision | Also load |
| --- | --- |
| Domain/Application behavior, workflows, or ports | `platform-backend-use-case` |
| Hosting/API transport, public DTOs, routes, or protocols | `platform-transport-contract` |
| Persistence, schemas, migrations, or persisted formats | `platform-data-evolution` |
| Remote identity/authentication provider adapter | `platform-external-integration` |
| Cross-project placement, dependency direction, or ADR need | `platform-artifact-architecture` |

Do not load React, Web-route, or Web-layout skills for Identity-only work.
Preserve redaction, least privilege, compatibility, and explicit failure paths.
