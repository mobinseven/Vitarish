# Security audit operating contract

Audit owns the event persistence contract, review mechanics, capacity state,
and review surface. Event
meaning and resource authorization remain with the initiating use case.

## Event and append invariants

- **[MUST]** Security events are typed and contain UTC timestamp, event
  category/name, outcome, actor when known, tenant/scope, operation/resource
  reference, correlation/idempotency data, and a redacted reason where
  applicable. Request-originated events contain the host-normalized source IP;
  system events use an explicit non-client source representation rather than a
  fabricated IP. Do not accept opaque request-body/payload dumps.
- **[MUST]** Never persist credentials, tokens, OTP/passkey material, private
  or HMAC keys, patient payloads, or secrets. Integrity digests may be
  persisted by their owner but never exposed through review DTOs. An unknown
  actor uses a non-identifying representation; do not synthesize a real user
  identity.
- **[MUST]** Append events without product update/delete operations. Duplicate
  idempotency keys have an explicit outcome. Existing rows are migrated
  forward-only with nullable new fields; legacy values are not fabricated.

## Availability, review, and retention

- **[MUST]** One configured positive storage threshold produces an in-product
  warning for existing authorized reviewers when the threshold is reached.
  Audit owns capacity measurement only; Main owns operational email/SMS
  delivery to deployment-configured administrator contacts. Do not add
  recipients, provider calls, or a product outbox to Audit.
- **[MUST]** Audit-store write failure or exhausted storage emits one redacted
  structured host error and does not replace the initiating operation result.
  The review query returns dependency-unavailable when the store cannot be
  read.
- **[MUST]** Review is a side-effect-free, paged DTO query supporting filter
  and sort by actor, UTC date/time, and event type at minimum. Enforce
  platform/tenant scope in the authorized Application boundary; never return
  persistence entities, chain key material, or sensitive payloads.
- **[MUST]** Review queries are side-effect-free, including denied reads; never
  append a review-attempt event from an Audit query. Platform-admin login or
  access-attempt auditing belongs to its authentication/access boundary.
  Audit state-changing management commands through a recursion-safe path.
  Reading audit data must not mutate queried events or acknowledge unrelated
  notices.
- **[MUST]** Preserve normalized source IP when known and represent anonymous
  and system actors explicitly. Do not synthesize an IP, actor, or payload.

## Migration and proof

- **[MUST]** Event shape, category, source IP, actor representation, capacity,
  and review contracts are compatibility boundaries. Use generated
  forward-only migrations and state mixed-version behavior.
- **[MUST]** Focused Audit tests cover required fields/redaction, catalog
  coverage, idempotency, legacy rows, capacity threshold behavior, write/read
  dependency failures, tenant isolation, and actor/time/type filter+sort.
  Audit review or browser-visible notices also require real two-host E2E proof.
