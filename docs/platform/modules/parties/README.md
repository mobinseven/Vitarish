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
