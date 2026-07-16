---
name: platform-trust-boundary
description: Reviews and implements authentication, authorization, ownership and tenant isolation, origin and CSRF controls, untrusted input validation, payload and abuse limits, privacy, secret handling, and redacted observability. Use whenever a change crosses a trust boundary or handles sensitive data.
---

# Platform Trust Boundary

## Purpose

Make security and privacy behavior explicit at the boundary that owns it. This skill owns trust decisions, not feature behavior or provider mechanics.

## Use when

- Authentication, authorization, tenant/owner scope, cookies, tokens, WebSocket origins, CORS/CSRF/antiforgery, uploads, external content, browser permissions, sensitive data, secrets, or abuse/resource limits are involved.
- Existing security behavior may be weakened by moving or adding a route, protocol, background worker, or provider.

## Do not use

- The change is purely internal, preserves an already-enforced trust boundary, and handles no new untrusted or sensitive data.

## Procedure

1. Draw the trust boundary: caller, identity context, resource owner/tenant, server, external provider, storage, logs, and generated artifacts.
2. Require authentication and the narrowest authorization/ownership check at the correct boundary; preserve read-only versus writable distinctions.
3. Validate untrusted shape, sequence, MIME/type, origin, size, duration, count, and rate/resource limits before expensive work.
4. For cookie-authenticated realtime connections, validate configured origins and reject missing/untrusted origins according to repository policy.
5. Define secret source, least privilege, rotation expectations, and startup/capability behavior when secrets/configuration are absent.
6. Minimize retention. State whether sensitive bytes/content are persisted, cached, logged, traced, or sent externally.
7. Redact telemetry and errors; keep them actionable without payload content or credentials.
8. Add negative tests for unauthenticated, unauthorized, cross-tenant, untrusted-origin, malformed, oversized, and unavailable cases that apply.

## Hard stops

- Authorization, tenant ownership, retention, or external disclosure is ambiguous.
- The design requires weakening TLS, auth, origin checks, validation, isolation, or redaction to function.
- Sensitive data may be logged or retained without an explicit approved need.

## Handoff

Route protocol mechanics to `platform-transport-contract`, provider disclosure to `platform-external-integration`, UI permissions/feedback to `platform-react-feature`, and coverage to `platform-test-validation`.
