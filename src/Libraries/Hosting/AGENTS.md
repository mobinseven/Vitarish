# Hosting security boundary contract

Hosting owns reusable HTTP/runtime mechanics: trusted-proxy normalization,
secure shared client defaults, endpoint metadata enforcement, production
security-option validation, and host clock adaptation. It does not own module
authorization, data meaning, session policy, or audit event meaning.

## Inbound trust and metadata

- **[MUST]** Accept forwarded scheme, host, and client IP only from explicitly
  configured immediate proxies/networks. Normalize once and expose the narrow
  trusted result; downstream code must not reparse forwarding headers.
- **[MUST]** Endpoint metadata/filtering may require an authentication profile,
  anonymous justification, audit disposition, transfer classification, and
  global size ceiling. It never grants tenant/resource authorization and must
  reject missing/invalid declarations before expensive dependency activation.
- **[MUST]** Antiforgery, credentialed CORS, secure/HTTP-only cookie behavior,
  authentication/session validation, exception mapping, and rate limiting
  remain ordered explicitly. A production configuration that disables or
  weakens a mandatory control fails validation before traffic is served.

## Outbound and internal transport

- **[MUST]** Shared HTTP clients use TLS 1.2/1.3, native RFC 5280 certificate
  validation, RFC 6125 hostname/reference-identity validation, explicit
  timeout/cancellation, and a declared revocation strategy. Never expose an
  invalid-certificate callback/bypass as provider configuration.
- **[MUST]** Do not equate private addressing with authenticated encryption.
  A separately deployed PostgreSQL, Redis, object store, provider, or internal
  service requires its supported secure channel; otherwise composition must
  enforce and prove same-machine placement.
- **[MUST]** The initiating boundary owns retry. Shared handlers must not retry
  mutations unless the owning operation supplies idempotency and explicitly
  selects that policy; do not stack framework, client, and job retries.

## Time, failure, and proof

- **[MUST]** Expose UTC wall time through the owned clock boundary and use
  monotonic elapsed time for durations where available. Do not hide wall-clock
  jumps in lockout/session/retry logic or let tests depend on real sleeps.
- **[MUST]** Audit, session, integrity, certificate, proxy, or mandatory
  configuration failure has an explicit safe result. Do not log-and-continue,
  return success, or replace a required failure with an empty value.
- **[MUST]** Changes to shared Hosting security behavior require focused option,
  ordering, invalid-configuration, proxy-spoofing, certificate-failure, and
  dependency-failure tests plus the real Web-to-E2E workflow and screenshot
  inspection required by the parent library contract.
