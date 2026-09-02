# Users module security contract

Users owns account records, password/credential persistence operations, roles,
claims, security stamps, WebAuthn credentials, personal access tokens, and
temporary authentication links. Authentication owns effective login,
failed-attempt, password-policy, and browser-session behavior. Follow the
parent module contract and keep this boundary non-cyclic.

## Accounts, passwords, and credentials

- **[MUST]** Every password-writing path—account creation, invitation,
  add/change/reset, and administration—invokes the same effective
  Authentication-owned validator through a narrow non-cyclic boundary. Do not
  keep a local minimum length or complexity rule that can diverge from it.
- **[MUST]** Password hashes, raw passwords, PATs, temporary-link codes,
  WebAuthn credential material, security stamps, tokens, and recovery data are
  never returned through general user DTOs or written to logs, audit reasons,
  snapshots, or fixtures. Store only approved protected/hashed forms and show
  a raw one-time secret at most once through its narrow authorized contract.
- **[MUST]** Temporary authentication links and PATs have explicit purpose,
  audience/subject, scope, expiry, single-use or revocation semantics, secure
  comparison, concurrency, and replay behavior. A destination/redirect is an
  exact owned allowlist decision, not a prefix or client-supplied trust claim.

## Roles and active security state

- **[MUST]** Role, claim, account-state, password, credential, or security-stamp
  mutations are security-management commands. Require named authorization,
  validate tenant/subject ownership, use optimistic concurrency where
  competing updates are possible, and audit allowed/denied outcomes.
- **[MUST]** Multiple roles combine by documented permission union only; that
  union never bypasses tenant, subject, resource, or capability checks. Keep
  tenant-role naming/normalization at one owner and test conflicting-role
  access directly.
- **[MUST]** Username, assigned-role, authorization-affecting-claim, and user
  deletion mutations reject with a typed conflict while an active session
  exists, before changing user data. Other credential, contact, passkey,
  security-stamp, and profile mutations do not revoke sessions as a side
  effect. Use the Authentication Application active-session query port; do
  not add a reverse Users-to-Authentication Infrastructure reference.
- **[MUST]** Queries for users, roles, claims, or credentials are side-effect
  free and tenant/subject scoped. Reading access history or security state must
  not acknowledge, rotate, repair, or revoke it implicitly.

## Compatibility and proof

- **[MUST]** Role names, claim types, normalization, password hashes,
  credential IDs, PAT/link formats, security stamps, and persisted account
  state are security compatibility boundaries. Use additive contracts and
  generated forward-only migrations; state token/session invalidation and
  mixed-version effects.
- **[MUST]** Focused Users tests cover every password-writing path against the
  same policy, role-union and tenant/resource denial, allowed/denied management
  audit, session revocation before confirmation, secret redaction, PAT/link
  expiry/replay/concurrency, and dependency failure. Endpoint changes also run
  the parent validators; browser-visible account/security changes require real
  two-host E2E proof.
