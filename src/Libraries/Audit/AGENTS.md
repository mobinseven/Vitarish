# Security audit operating contract

Audit owns the event persistence contract, append integrity, review mechanics,
retention representation, capacity state, and emergency audit path. Event
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
- **[MUST]** Append an event and its tenant-local sequence/chain head in one
  transaction. Retry only bounded concurrency conflicts at the audit boundary.
  Duplicate idempotency keys have an explicit outcome; no caller may interpret
  an uncertain/failed append as confirmed success.
- **[MUST]** Integrity keys remain outside the audit database. Preserve old
  key IDs for verification after rotation. Never fabricate hashes, sequence,
  or chain links for legacy rows.
- **[MUST]** Scheduled verification detects modified events, deleted sequence,
  invalid chain/HMAC, missing required keys, and unauthorized retention gaps.
  Failure produces critical durable state and must not repair or conceal the
  evidence automatically.

## Availability, review, and retention

- **[MUST]** Warning/critical capacity thresholds produce durable authorized
  notices and an external alert event. Primary-store failure/full behavior uses
  the explicitly bounded protected emergency path and restricted mode; never
  silently discard a mandatory event or overwrite retained history.
- **[MUST]** Required security mutations fail closed when neither the primary
  nor approved emergency audit contract can confirm the required event. Keep
  recovery and reconciliation idempotent, ordered, and observable.
- **[MUST]** Review is a side-effect-free, paged DTO query supporting filter
  and sort by actor, UTC date/time, and event type at minimum. Enforce
  platform/tenant scope in the authorized Application boundary; never return
  persistence entities, chain key material, or sensitive payloads.
- **[MUST]** Audit every allowed and denied review/management attempt through a
  recursion-safe owned path. Reading audit data must not mutate the queried
  events or acknowledge unrelated notices.
- **[MUST]** Preserve normalized source IP for the complete retained-event
  lifetime unless an evaluator-approved representation says otherwise.
  Authorized retention must remain distinguishable from unauthorized deletion
  and must create its own evidence.

## Migration and proof

- **[MUST]** Event shape, sequence/head, key ID, retention, capacity, notice,
  and emergency/reconciliation state are compatibility boundaries. Use
  generated forward-only migrations and state mixed-version behavior.
- **[MUST]** Focused Audit tests cover required fields/redaction, idempotency,
  append/head atomicity, concurrency, key rotation/loss, row change/deletion,
  capacity thresholds, primary/emergency failure, reconciliation, retention,
  tenant isolation, and actor/time/type filter+sort. Audit review or
  browser-visible notices also require real two-host E2E proof.
