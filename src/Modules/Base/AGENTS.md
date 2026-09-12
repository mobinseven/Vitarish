# Module Base security-mechanics contract

Base owns reusable module endpoint conventions, metadata, filters,
authenticated internal-client mechanics, idempotency primitives, and shared
background execution. It follows the parent module contract and must not absorb
module/app business policy.

- **[MUST]** Endpoint profiles and metadata are typed, explicit, and have one
  authoritative definition. Authentication profile, anonymous justification,
  antiforgery, audit disposition, transfer classification, content type, and
  operation ID must not be inferred from route text or handler names.
- **[MUST]** Metadata and filters classify/enforce transport mechanics only.
  They never grant tenant, subject, resource, purpose, or business capability
  authorization and never define a module's data sensitivity or format rules.
- **[MUST]** Apply global request-size/type rejection before body buffering,
  provider activation, storage access, or module handler execution. Preserve
  cancellation and safe error mapping; do not read an unbounded body in a
  filter to make a classification decision.
- **[MUST]** Audit disposition has exactly one responsible path. A use case
  marked as Application-audited must prove its required start/outcome events;
  a filter-audited endpoint must not also append a competing event sequence.
  Under the approved AU-8 product selection, an audit-store failure is logged
  once as a redacted host error and does not replace the initiating result.
- **[MUST]** Internal-service metadata is not authorization by itself. Internal
  endpoints validate the approved short-lived credential, issuer/audience,
  expiry, replay/idempotency, tenant/scope, and exact operation at the endpoint
  boundary; `AllowAnonymous` is only transport plumbing for that explicit
  authentication mechanism.
- **[MUST]** Shared idempotency, retry, and background primitives expose
  explicit outcomes and preserve initiating-owner policy. Do not retry a
  mutation without an idempotency contract or run tenant work without trusted
  tenant context, cancellation, bounded failure, and audit disposition.
- **[MUST]** Changes to endpoint conventions/filters run focused Base tests,
  `dotnet run --project tools/architecture -- openapi-endpoints`, affected
  module endpoint tests, and real two-host E2E proof when runtime behavior
  changes. Add negative fixtures proving missing/contradictory metadata fails.
