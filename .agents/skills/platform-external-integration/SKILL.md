---
name: platform-external-integration
description: Designs and implements infrastructure adapters for vendor SDKs and remote services, including configuration, capability checks, quotas, payload shaping, timeout, retry, cancellation, error mapping, and redacted telemetry. Use whenever a backend crosses an external provider boundary.
---

# Platform External Integration

## Purpose

Contain provider-specific behavior in Infrastructure while presenting a stable Application-owned capability. This skill owns adapter mechanics, not application policy or transport/UI behavior.

## Use when

- Adding or changing a cloud service, third-party API, vendor SDK, remote model, queue, object store, search engine, or other nondeterministic provider.
- Provider availability, credentials, regions, models, quotas, payload limits, timeouts, or provider errors affect delivery.

## Do not use

- The task only consumes an existing stable application port and adapter.

## Procedure

1. Read the Application port and map it to the narrowest provider operations; do not leak provider types inward.
2. Validate required configuration at composition/capability boundaries and distinguish unavailable from misconfigured.
3. Define provider limits, chunking/batching, ordering, cancellation propagation, timeout ownership, and connection cleanup.
4. Put retries only at the initiating boundary; require idempotency before retrying state-changing calls.
5. Map provider failures into stable application outcomes while retaining safe diagnostic detail in telemetry.
6. Redact credentials and sensitive payloads. Never log request bodies, media, transcripts, clinical content, or provider tokens unless an explicit approved policy permits it.
7. Use deterministic fakes for adapter mapping tests and at least one runtime hot scenario with real provider wiring when confidence depends on it.
8. Keep provider-specific configuration and registration in Infrastructure/composition, not Domain/Application.

## Hard stops

- Provider types, status codes, or configuration leak into Domain/Application contracts.
- Retry, timeout, cancellation, quota, or cleanup ownership is undefined.
- Sensitive payloads or secrets can reach logs, exceptions, generated output, or source control.

## Handoff

Route application policy to `platform-backend-use-case`, protocol exposure to `platform-transport-contract`, security/privacy to `platform-trust-boundary`, and runtime verification to `platform-test-validation`.
