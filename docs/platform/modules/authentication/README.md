# Authentication Documentation

Status: Active  
Owner: Platform module: authentication  
Scope: App-agnostic module documentation  
Last reviewed: 2026-07-09

This folder contains app-agnostic module documentation. App-specific usage belongs in the owning app's flat collections under `docs/apps/<app>/`.

## Tenant handoff

When a signed-in user creates their own clinic, the creation response carries a
one-time handoff token so they continue, signed in, on the clinic's own host.
`IssueTenantHandoffHandler` issues it only for the signed-in user themself and
stores a SHA-256 hash of it in `TenantHandoffTickets`, bound to that user and
the target hostname. A ticket expires after 60 seconds and works once.

`POST /api/modules/authentication/handoff/redeem` is anonymous and rate
limited, and is mapped only when the authentication profile allows
`TenantHandoff`; the `TenantHandoff` pre-authentication action must also be
permitted by the stored policy. It accepts the ticket only on the host it was
issued for, taken from the request's trusted origin, refuses users denied by the
failed-attempt policy, and starts a normal host-only session through the sign-in
service. Unknown, malformed, expired, replayed, and wrong-host tickets get the
same refusal, and the user signs in normally instead. Issue and redemption are
audited as `authentication.tenant-handoff.issued` and
`authentication.tenant-handoff.redeemed`.

The evaluated product profile does not allow the handoff. A deployment whose
stored policy predates the action has it off until a platform administrator
enables it in Settings. Rolling back means turning it off there or dropping
the `TenantHandoffTickets` table; nothing else reads it.
