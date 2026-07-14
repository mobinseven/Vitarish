# Standardize SEA HTTP authorization

Date: 2026-07-14
Status: Accepted
Scope: SEA module HTTP authorization
Owner: SEA API
Related: ADR-0003, ADR-0004, ADR-0005

## Context

SEA HTTP access was enforced by Application-service authorization decorators. Those decorators mixed HTTP caller authorization with use-case orchestration, translated denied access into exceptions, and made capability reporting a second permission API. The module needs standard HTTP semantics without moving ASP.NET Core authorization types or principal access into Domain or Application.

SEA event access already has one authoritative state model: user read/write role assignments and event read/write role assignments. Management access is an identity concern and is independent of those event assignments.

## Decision

Authentication remains enforced by the protected SEA module route group.

Management access uses the named ASP.NET Core authorization policy `Sea.Manage`. The policy succeeds for a platform administrator or the current tenant's owner. It evaluates the current principal through the shared identity definitions and `TenantAccessEvaluator`; SEA does not construct tenant-role strings itself.

Object-level HTTP access uses `IAuthorizationService.AuthorizeAsync` with SEA operation requirements and identifier-only resource records. API endpoints authorize the authenticated caller before invoking a mutation or returning protected data.

SEA user-role and event-role assignments remain the authoritative source of event Read and Write decisions. Write implies Read. Read does not imply Write. Attachment access derives from the attachment's owning event. Project and procedure Read require at least one readable contained event unless the caller has elevated management access.

Capability endpoints invoke the same registered authorization policies as enforcement. Capability DTO values are outputs only and are never accepted as enforcement inputs.

Ordinary SEA Application services remain use-case orchestration boundaries. They do not read HTTP principals, depend on ASP.NET Core authorization types, or decorate themselves with endpoint-access behavior.

## Artifact ownership

| Artifact | Classification | Responsibility |
| --- | --- | --- |
| SEA requirements, operations, resources, and handlers | Presentation/API security | Decide whether the current principal may invoke an HTTP operation. |
| Existing SEA services | Application/use-case orchestration | Perform requested work after the API authorization boundary. |
| Attachment validation | Domain | Own attachment kind and filename invariants. |
| Capability and attachment DTOs | Contracts | Serialize authorization decisions and attachment metadata. |
| EF configuration and migration | Infrastructure/persistence | Persist and migrate attachment kind and filename. |
| Authorization, endpoint, contract, and migration tests | Tests | Verify observable security, contract, and persistence behavior. |

## Consequences

- Denied authenticated requests return standard 403 responses; unauthenticated requests remain 401 through the protected route group.
- Missing tenant-scoped resources return 404 without exposing data, while existing tenant-scoped resources denied by a policy return 403.
- Request-scoped handlers may cache the authenticated user's SEA role assignments and batched event-role lookups for the request lifetime.
- Endpoint mappings become explicit about their authorization classification.
- The legacy event write-permission endpoint remains temporarily for client compatibility, but it delegates to the Event Write authorization operation and is deprecated in OpenAPI.
- Resource authorization handlers may depend on SEA Application query abstractions. Domain and Application remain independent of ASP.NET Core authorization.

## Alternatives considered

### Rename the decorators or add a permission service

Rejected. A renamed `SeaAccessService`, permission manager, or service decorator would preserve the duplicate authorization pipeline and mixed ownership.

### Keep authorization in Application services

Rejected for this HTTP-access boundary. It would require ordinary use cases to understand principals or transport authorization and would prevent standard ASP.NET Core policy reuse for capabilities.

### Calculate capabilities from role DTOs in endpoints

Rejected. It would duplicate the enforcement rule and allow capability output to drift from authorization behavior.

## Validation and enforcement

- Repository search must find no legacy SEA authorization decorator or registration.
- Handler tests cover identity resolution, management elevation, read/write role semantics, resource ownership, tenant isolation, and request-scoped caching.
- Endpoint tests cover 401, 403, 404, pre-mutation denial, filtering/redaction, capabilities, and OpenAPI metadata.
- PostgreSQL-backed runtime checks cover reader, writer, tenant owner, platform administrator, and cross-tenant access.
- OpenAPI and module-boundary validators remain required delivery gates.
