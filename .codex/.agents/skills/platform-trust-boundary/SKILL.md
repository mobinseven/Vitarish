---
name: platform-trust-boundary
description: Owns authentication, authorization, ownership/tenant isolation, origin/CSRF controls, untrusted input, abuse limits, privacy, secrets, retention, and redacted observability.
---

# Platform Trust Boundary

## Rules

1. Map caller, identity, owner/tenant, server, provider, storage, logs, and generated artifacts across the trust boundary.
2. Enforce authentication and the narrowest authorization/ownership check at the correct boundary; preserve read-only/write distinctions.
3. Validate untrusted shape, sequence, MIME/type, origin, size, duration, count, and rate/resource limits before expensive work.
4. For cookie-authenticated realtime connections, enforce configured origin policy, including missing origins.
5. Define secret source, least privilege, rotation expectations, and absent/misconfigured capability behavior.
6. Minimize retention; state what sensitive data is persisted, cached, logged, traced, or sent externally.
7. Redact telemetry/errors while keeping them actionable.
8. Add applicable negative tests: unauthenticated, unauthorized, cross-tenant, untrusted-origin, malformed, oversized, and unavailable.

## Stop

- Authorization, ownership, retention, or external disclosure is ambiguous.
- The design weakens TLS, auth, origin checks, validation, isolation, or redaction.
- Sensitive data may be logged or retained without approved need.
