# Core authorization matrix

The server is the authorization owner. The route group and endpoint metadata
are the executable source for HTTP authentication; the application/use-case
boundary remains the owner of tenant, subject, resource, and mutation rules.
The OpenAPI endpoint validator and endpoint metadata tests are the synchronization
check for new routes.

| Boundary | Anonymous | Authenticated tenant user | Tenant owner | Platform admin | Internal service |
| --- | --- | --- | --- | --- | --- |
| Authentication, login, challenge, antiforgery bootstrap | Explicitly public where the authentication contract requires it; rate limited | Same public contract | Same public contract | Same public contract | Not a service API |
| Public landing/contact/webhook | Explicitly public only with typed anonymous justification, input limits, and rate limiting or provider verification | Same public contract | Same public contract | Same public contract | Not a service API |
| Protected module/app read | 401 | Allowed only after the owning application/resource check | Allowed within the active tenant and resource scope | Allowed only where the owning policy grants it | Internal credential only where the endpoint explicitly declares that boundary |
| Protected module/app command | 401 | 403 unless the owning capability and tenant/resource checks succeed | Allowed only for owner-level operations or granted capability | Allowed only for platform-owned operations | Internal credential only where explicitly declared; browser cookies are not accepted |
| Platform administration | 401 | 403 | 403 | Allowed by the platform policy | 403 unless separately declared |
| Agent-runtime callback | 401 for a missing, invalid, expired, or replayed signed credential | Not a browser route | Not a browser route | Not a browser route | Signed short-lived execution credential, envelope binding, replay guard, and application capability check |

## Required decision for every new operation

Every endpoint uses `MapProtectedModuleGroup`,
`MapInternalServiceModuleGroup`, or `MapAnonymousModuleGroup`; every operation uses a `MapModule*` helper with a
stable operation ID. An anonymous operation must provide a justification and
must be independently safe when called directly. Protected operations inherit
`BrowserOrBearer` unless their owning policy narrows the decision. Policy and
resource checks must remain at the owning application or module boundary, so a
UI route guard or caller-supplied tenant/resource identifier cannot grant
access.

Internal callbacks are not public compatibility routes: their endpoint metadata
activates the host internal-service boundary, they are excluded from OpenAPI,
validate credentials before dispatch, reject missing credentials with 401,
reject replay with 409, and never use browser authentication as a service
identity. Compatibility shims retain the same decision and are covered by the
same callback tests.

## Verification

The required proof is the generated endpoint inventory plus endpoint metadata
tests, application authorization tests, and direct requests through the local
ingress for anonymous, authenticated, tenant-user, tenant-owner,
platform-admin, and service callers. A passing Web UI test alone is not
authorization evidence.
