# ADR-0002: Authenticated request-scoped WebSocket transcription

Date: 2026-07-17<br>
Status: Accepted<br>
Decision type: Module architecture<br>
App: Platform<br>
Module: Intelligence<br>
Capability: Live voice transcription<br>
Owner: Architecture<br>
Approver: Platform Architecture Steward<br>
Implementation owner: Intelligence module maintainer<br>

Related ADRs:
- `docs/platform/modules/intelligence/decisions/0001-intelligence-realtime-streaming-and-file-input.md`
- `docs/architecture/decisions/0003-isolate-module-interface-libraries-with-api-client-contracts.md`
- `docs/architecture/decisions/0004-protect-internal-module-api-endpoints-by-default.md`

Supersedes: None<br>

## Context

Clinicians need low-latency, explicitly started and stopped dictation while editing a procedure note. The browser must send a sequence of transient audio chunks while the server returns confirmed transcript segments. Protected HTTP response streaming approved by Intelligence ADR-0001 is one-way and cannot carry this request-scoped bidirectional flow without buffering or a parallel upload mechanism.

Audio and transcript text are sensitive clinical content. The design must preserve browser-cookie authentication, tenant resolution, origin protection, bounded resource use, cancellation, deterministic ordering, and zero recording retention. It must not make Intelligence a file-storage owner or change the existing offline transcription APIs.

## Decision

Intelligence will expose one authenticated, request-scoped WebSocket endpoint for live transcription and one protected HTTP capability query.

The WebSocket is a transport adapter around an Application-owned session. It is not a durable hub, shared realtime channel, background job, or stored upload. One upgraded connection owns exactly one transcription session and is discarded after one stable terminal outcome.

The dependency flow is:

```text
Clinic React hook / browser MediaRecorder
        |
        v
Protected Intelligence WebSocket + capability query
        |
        v
Intelligence Application live-transcription session
        |
        v
Application-owned provider-neutral transcription port
        |
        v
Google Speech-to-Text V2 Infrastructure adapter
```

### Trust and privacy boundary

- The capability query and WebSocket endpoint inherit the protected Intelligence module authentication policy.
- Cookie-authenticated WebSocket upgrades require a present `Origin` header that exactly matches the host's configured trusted CORS origins. Missing, opaque, wildcard, or unconfigured origins are rejected before upgrade.
- Tenant resolution continues to use the host request context; client-supplied tenant or session identifiers are never authorization authority.
- Audio is streamed only to the configured speech provider. It is not persisted, attached, cached, replayed, or placed in background jobs.
- Audio bytes, transcript text, credentials, and provider payloads must not appear in logs, traces, metrics, exceptions returned to clients, or generated artifacts.
- Telemetry may record content-free lifecycle state, duration, byte counts, outcome code, and provider operation category.

### Protocol

The protocol is ordered and deliberately small:

1. After upgrade, the first client text message must be `start` with one advertised language and MIME type.
2. The server opens the provider stream and replies with `started` only after the provider stream is ready.
3. The client sends binary audio messages in WebSocket order.
4. The server sends only confirmed `segment` messages. Each segment has a strictly increasing sequence number.
5. The client explicitly sends one `stop` text message. The server completes provider input and drains confirmed results.
6. The server sends exactly one terminal `completed`, `failed`, or `canceled` message and then closes normally when the transport remains writable.

Malformed control JSON, text/binary messages in the wrong phase, duplicate start/stop messages, unsupported language or MIME type, oversized frames, total-payload overflow, duration expiry, network idleness, and unavailable providers terminate the session with a stable contract error. A disconnected client cancels work even when no terminal frame can be delivered.

The capability response exposes only availability, supported languages, supported MIME types, and client-relevant frame, total-payload, duration, and idle limits. It does not expose provider names, model identifiers, credentials, recognizer resources, project IDs, quota details, or diagnostic payloads.

### Limit and cancellation ownership

- API transport owns control-frame parsing, WebSocket message bounds, protocol phase validation, trusted-origin validation, and close behavior.
- Application owns allowed language/MIME policy, bounded audio buffering, total payload, maximum duration, network-idle outcome, provider-operation timeout, segment sequence, session transitions, and the single terminal outcome.
- Infrastructure owns Google request chunking, Google stream cleanup, provider cancellation, provider response validation, and mapping provider failures to stable Application errors.
- The browser hook owns MediaRecorder timeslicing, client resource cleanup, ordered Blob conversion/sending, stale-session suppression, and preservation of the latest editable draft.

The retry owner is the clinician. No layer automatically retries a live stream because replaying transient audio would require retained content and could duplicate transcript segments.

## Consequences

### Positive

- The transport fits true bidirectional low-latency audio without introducing SignalR, durable sessions, or file upload ownership.
- Application behavior remains provider-neutral and deterministically testable.
- Provider and protocol details remain outside clinic components.
- Sensitive audio has a short, explicit lifetime with no persistence path.

### Negative

- The host must enable WebSocket middleware and preserve upgrade support through its reverse proxy.
- Browser-cookie WebSockets require explicit origin validation because normal antiforgery headers cannot be added by the browser WebSocket API.
- Real browser, codec, network, and provider wiring still require pre-release hot validation in addition to deterministic tests.

## Alternatives considered

### Buffered HTTP upload

Rejected because it cannot return confirmed segments while audio is still being captured and would increase memory and latency.

### HTTP request streaming with a separate response stream

Rejected because browser support and correlation/cleanup are more complex than one request-scoped bidirectional connection.

### SignalR

Rejected because groups, reconnect state, hub lifetime, and additional protocol machinery are not required for a single request-scoped stream.

### Direct browser-to-Google streaming

Rejected because it would expose provider authorization and configuration to an untrusted client and bypass module authorization, limits, redaction, and provider abstraction.

### Retained audio or Files/Sea attachment flow

Rejected because dictation is transient input, not a clinical attachment. Retention would expand privacy, consent, storage, authorization, and deletion scope.

## Validation / enforcement

- Architecture validation must continue to keep provider SDK types out of Application, Contracts, and React code.
- Endpoint metadata tests must prove protected authentication and documented OpenAPI exclusion for the WebSocket upgrade route.
- Security tests must cover unauthenticated upgrades, missing/untrusted origins, malformed protocol input, and bounded payload rejection.
- Application tests must cover state transitions, ordering, cancellation, idle/duration/provider timeouts, limits, cleanup, and one terminal outcome.
- Adapter tests must cover Google model/config mapping, final-only results, request chunking, cancellation, and stable provider failures without content leakage.
- React tests must cover support and permission gating, start/stop states, edit preservation, ordered insertion, failure preservation, read-only gating, and cleanup.
- Pre-release validation must exercise HTTPS in supported Chrome and Edge with `fa-IR` and `en-US` against the configured Google Speech-to-Text V2 project.

## Notes

This ADR approves only live, transient, request-scoped transcription. Intelligence ADR-0001 remains authoritative for responder/rewrite HTTP streaming, Files-owned stored inputs, and existing offline transcription behavior.
