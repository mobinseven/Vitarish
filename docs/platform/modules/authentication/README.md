# Authentication Documentation

Status: Active  
Owner: Platform module: authentication  
Scope: App-agnostic module documentation  
Last reviewed: 2026-07-09

This folder contains app-agnostic module documentation. App-specific usage belongs in the owning app's flat collections under `docs/apps/<app>/`.

## Password sign-in with an additional code

From account settings, a signed-in person without a password can set their
first password. Existing passwords are changed using the current password. The
first password uses the same effective password policy as other password
writes. Setting it requires a recent sign-in; it cannot be used to set another
person's password.

Account settings also let the person turn two-step sign-in on or off. Enabling
it requires a password, a confirmed delivery contact, and an effective policy
that permits the selected password-and-code action. Both changes require a
recent sign-in. Authentication records the original sign-in time in its
server-side session and carries it through a tenant handoff without refreshing
it. The current limit is ten minutes. Sign in again when the limit expires.

When a password account has two-factor enabled, sign-in requires a one-time
code after the password. Authentication prefers a confirmed phone and sends an
SMS; if there is no confirmed phone, it uses the confirmed email address. If
neither contact is confirmed, sign-in is refused. The code has the existing
challenge lifetime and failed-attempt controls, and successful verification
starts the normal browser session. Codes are not included in audit records.

The profile requires the corresponding phone and email password-code actions
and mechanisms when this behavior is enabled. Existing stored policies are
reconciled with those requirements at read time; no per-user setting or data
migration is needed. During a mixed-version rollout, an email challenge issued
by an updated node cannot be completed on an older node; restart sign-in on an
updated node after deployment. Rolling back removes email-code support, so an
affected account needs a confirmed phone or the updated release. This is a
password followed by a code delivered to a contact: whether it provides
independent factors depends on how that contact is protected.

The account setting and authentication policy are stored separately. If the
policy changes while the setting is being enabled, the flag can remain on
even though its selected channel is no longer allowed. Sign-in and code
verification check the current policy again and refuse that sign-in; they do
not fall back to password alone. After a policy change, the person can retry
or turn the setting off after a recent sign-in.

The authentication-time columns are nullable for existing sessions and
handoff tickets. Those records cannot satisfy the recent-sign-in check until a
new sign-in establishes a session with an authentication time. Apply the
forward migration before starting the updated server; mixed versions do not
provide the new setting consistently. Rolling back the application removes the
new settings controls and first-password path; existing password and two-step
state remain stored. Follow the database rollback procedure before removing
schema columns.

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
