# Authenticated Identity and Tenant Authorization Rule

Backend module endpoints should resolve the current authenticated user id through `AuthenticatedUserIdResolver` from `Vitarish.Modules.Base.Core.Identity`. Endpoints should not parse `HttpContext.User`, `ClaimTypes.NameIdentifier`, or `sub` directly for user identity. Existing endpoints that parse claims locally (e.g. account password/email/phone change) should be migrated to the centralized resolver in follow-up work.

The resolver is the backend source of truth for browser-cookie and bearer-authenticated principals. Missing subject ids must be handled as explicit authentication failures rather than masked by local fallbacks.

Tenant role and capability checks must use the centralized backend tenant-access result produced by `TenantAccessEvaluator`. React guards may consume the session `tenantAccess` shape only to hide, show, or redirect UI; backend policies and endpoint authorization remain the security boundary.

Future React clinic guards must consume `useTenantAuthorization` and must not duplicate tenant-owner or tenant-user role rules in layouts, pages, or feature-local hooks.
