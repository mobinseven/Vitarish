# Parties Documentation

Status: Active  
Owner: Platform module: parties  
Scope: App-agnostic module documentation  
Last reviewed: 2026-07-09

This folder contains app-agnostic module documentation. App-specific usage belongs in the owning app's flat collections under `docs/apps/<app>/`.

## Tenant addresses

A tenant can claim its own address, `<subdomain>.<base domain>`, where the base
domain is the `Vitarish:TenantBaseDomain` setting. The subdomain rules and the
reserved-name list live in `TenantSubdomain` and `ReservedTenantSubdomains`
(Parties Domain). A hostname held by any tenant indicator, whatever its method
or status, is taken.

`GET /api/modules/parties/onboarding/subdomain-availability?subdomain=<label>`
answers `Available`, `Malformed`, `Reserved`, or `Taken` for any signed-in
user. It answers `404` while `Vitarish:Features:TenantOnboarding` is off or no
base domain is configured.

The host trusts an active tenant's own address as a browser origin, in addition to
the exact `Cors:AllowedOrigins` list, for CORS, WebSocket upgrades, and the audited
client host: `https://<subdomain>.<base domain>` on any port while an active
subdomain indicator holds that hostname (`http` only under a `localhost` base
domain). There is no wildcard origin. `TenantOriginTrust` caches each answer for a
few minutes per instance; a save that changes an indicator drops the cached answers
for its hostnames on that instance.

## Self-service tenant creation

`POST /api/modules/parties/onboarding/tenants` lets a signed-in user create their
own tenant and become its owner. The creator comes from the session, never the
body, and needs a verified phone number. The registered
`SelfServiceTenantProfile` fixes the app id and active modules (the Clinic app
registers `ClinicTenant`), the address is always an active primary subdomain
indicator, and the request carries an `Idempotency-Key` header: the same creator
and key always name the same tenant, so a retry returns the first result and
the same key with a different request answers 409. A creator may create at most
three tenants a day, and each client IP ten an hour.

`SelfServiceTenantStore` writes the owner, tenant, and address in a serializable
Parties transaction that commits last. Before Users makes the creator the owner
(`AssignNewTenantOwnerCommand`), the creator's active sessions end as replaced
by a new session, because no role is assigned while its user has an active
session; the creator continues on the new tenant's own address. If Users fails,
the Parties transaction rolls back; if the Parties commit fails, the owner
assignment is undone. Creation and its refusals are audited as
`parties.tenant.self-service-created`.

People sign up and create a tenant on the onboarding host,
`Vitarish:OnboardingUrl`, which startup binds to the platform-owned
`OnboardingTenant`; it holds no practice data.
