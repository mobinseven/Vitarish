---
name: platform-external-integration
description: Owns Infrastructure adapters for remote providers: configuration, capability, quotas, payloads, timeout/retry/cancellation, error mapping, cleanup, and redacted telemetry.
---

# Platform External Integration

## Rules

1. Implement the narrow Application port without leaking provider types inward.
2. Validate configuration at composition/capability boundaries; distinguish unavailable from misconfigured.
3. Define provider limits, batching/chunking, ordering, cancellation propagation, timeout ownership, and cleanup.
4. Retry only at the initiating boundary and only with defined idempotency for mutations.
5. Map failures to stable application outcomes while retaining safe diagnostic telemetry.
6. Redact credentials and sensitive payloads; do not log bodies, media, transcripts, clinical content, or tokens without explicit policy.
7. Keep provider registration/configuration in Infrastructure/composition.
8. Test mapping with deterministic fakes and use a real-provider hot scenario when mocks cannot prove wiring/runtime behavior.

## Stop

- Provider types/status/configuration leak inward.
- Retry, timeout, cancellation, quota, or cleanup ownership is undefined.
- Secrets or sensitive payloads can reach logs, errors, generated output, or source control.
