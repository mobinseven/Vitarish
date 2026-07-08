# ADR-0001: Intelligence transport boundary and file-input ownership

Date: 2026-06-25  
Status: Accepted  
Decision type: Module architecture  
App: Platform  
Module: Intelligence  
Capability: Realtime AI streaming, transcription, rewriting, responder output, and file input  
Owner: Architecture  
Approver: Platform Architecture Steward  
Implementation owner: Intelligence module maintainer  

Related ADRs:
- `docs/architecture/decisions/0003-isolate-module-interface-libraries-with-api-client-contracts.md`
- `docs/architecture/decisions/0004-protect-internal-module-api-endpoints-by-default.md`

Supersedes: None  

---

## Status lifecycle

This ADR uses the following lifecycle:

| Status | Meaning |
| --- | --- |
| `Proposed` | Decision is drafted but not yet binding. Implementation may proceed only behind temporary/report-only validation where explicitly allowed. |
| `Accepted` | Decision is binding. New and changed code must comply. Validation may become fail-fast. |
| `Deprecated` | Decision is still historically relevant but should not be used for new work. |
| `Superseded` | Decision has been replaced by a newer ADR. |
| `Rejected` | Decision was considered but not adopted. |

This ADR may move from `Proposed` to `Accepted` only after:
- the scope ambiguity around transcription upload semantics is resolved by this ADR or a follow-up ADR;
- Intelligence Interface backend dependency violations are removed or explicitly tracked as temporary exceptions;
- tests/static validation exist for the dependency boundary and protected endpoint rules;
- Architecture accepts the decision.

Until then, Intelligence migration validation remains `ReportOnly`.

---

## Context

ADR-0003 requires module Interface libraries to stop referencing backend Application, Domain, and Infrastructure projects.

The Intelligence module currently violates this boundary because Interface services and components directly use:
- Intelligence Application abstractions;
- Intelligence Domain models/options;
- Files Application/Domain types.

Intelligence has higher migration risk than simple request/response modules because it includes:
- non-streaming prompt/result calls;
- server-streamed responder output;
- browser audio capture;
- transcription workflows;
- rewrite streaming;
- file input;
- provider-internal streaming from AI SDKs;
- cancellation and partial-output semantics;
- provider outages and malformed streaming chunks.

ADR-0003 intentionally defers large-file, realtime progress, streaming, resumable upload, and range-request conventions to module-specific design. This ADR records the Intelligence-specific boundary and transport decision required before completing the ADR-0003 migration.

---

## Problem statement

Intelligence needs a frontend-safe transport model that:

1. preserves the ADR-0003 `Interface -> Client/Contracts -> Api -> Application` boundary;
2. supports non-streaming and streaming AI use cases;
3. prevents provider SDKs and provider-specific models from leaking into frontend-facing code;
4. keeps binary file transfer owned by Files;
5. makes cancellation, partial output, and expected failures explicit;
6. avoids introducing realtime hub/session infrastructure before there is a proven requirement.

---

## Decision drivers

The selected design must optimize for:

- frontend-safe dependency boundaries;
- explicit module contracts;
- protected API endpoints by default;
- predictable streaming semantics;
- minimal new infrastructure;
- clear file ownership;
- testable/enforceable architecture rules;
- ability to introduce SignalR/WebSocket later by follow-up ADR if required.

---

## Scope of this ADR

This ADR decides:

- the Intelligence project split required for ADR-0003 compliance;
- allowed dependencies for `Intelligence.Interface`;
- initial approved frontend transport choices;
- default server-to-client streaming approach;
- binary file-input ownership;
- provider-boundary rules;
- minimum cancellation/failure/partial-output semantics;
- minimum progress-envelope expectations;
- validation and enforcement rules.

This ADR does **not** decide:

- exact DTO/property names;
- exact route templates;
- exact stream-frame serialization format;
- exact retry/backoff policy;
- concrete provider SDK behavior;
- direct Intelligence-owned binary upload;
- resumable upload;
- range requests;
- WebSocket transport;
- SignalR hub/session model;
- Server-Sent Events reconnection semantics;
- production transcription upload semantics.

A companion implementation specification may define exact DTOs, chunk envelopes, route names, and tests. That specification must not contradict this ADR.

---

## Decision

Intelligence will use the ADR-0003 project split:

- `src/Modules/Intelligence/Contracts/Intelligence.Contracts.csproj`
- `src/Modules/Intelligence/Client/Intelligence.Client.csproj`
- `src/Modules/Intelligence/Api/Intelligence.Api.csproj`

`Intelligence.Interface` may depend on:

- `Intelligence.Client`
- `Intelligence.Contracts`

`Intelligence.Interface` must not depend on:

- `Intelligence.Application`
- `Intelligence.Domain`
- `Intelligence.Infrastructure`
- Files Application projects
- Files Domain projects
- provider SDK projects/packages

`Intelligence.Api` maps protected HTTP requests and approved streaming endpoints to Application abstractions.

Application and Infrastructure retain ownership of:

- provider dispatching;
- model registry;
- provider SDK usage;
- transcription providers;
- background jobs;
- provider-specific streaming;
- provider-specific error mapping before conversion to contract-safe outcomes.

Binary file transfer remains owned by Files. Intelligence receives file references only.

Direct Intelligence upload, resumable upload, range request behavior, SignalR, WebSocket, and SSE are not approved by this ADR unless this ADR is amended or a follow-up ADR is accepted.

---

## Approved project boundary

```text
Frontend host / Intelligence.Interface
        |
        v
Intelligence.Client
        |
        v
Intelligence.Contracts
        |
        v
Protected HTTP endpoints
        |
        v
Intelligence.Api
        |
        v
Intelligence.Application
        |
        v
Intelligence.Domain / Intelligence.Infrastructure / provider SDKs
```

Rules:

- `Contracts` contains serialization-safe DTOs, enums, result shapes, and route constants only.
- `Client` owns frontend-safe transport calls and stream reading.
- `Api` owns protected endpoint mapping and conversion between contract DTOs and Application requests/results.
- `Application` owns use-case orchestration.
- `Infrastructure` owns provider SDK integration.
- `Interface` owns UI composition only.

---

## Transport classification

| Use case | Initial classification | Approved frontend transport | Decision status | Decision |
| --- | --- | --- | --- | --- |
| Prompt non-streaming request/response | Non-streaming HTTP request/response | HTTP POST | Approved | Use protected JSON HTTP endpoint with contract request/result DTOs. |
| Responder full response without progressive tokens | Non-streaming HTTP request/response | HTTP POST | Approved | Use protected JSON HTTP endpoint with contract request/result DTOs. |
| Responder token streaming | Server-to-client streaming | HTTP streaming | Approved | Use protected HTTP streaming endpoint returning framed contract chunks. |
| Rewrite streaming | Server-to-client streaming | HTTP streaming | Approved | Same as responder token streaming with rewrite-specific contract route and outcome shape. |
| Browser audio capture | Local browser capture | Browser MediaRecorder / JS interop | Approved as UI concern | Capture remains local to Interface/browser. Captured data may be sent only through an approved Client transport. |
| Transcription upload/streaming | Request upload/streaming semantics | Not approved by this ADR | Deferred | Production upload/streaming semantics require follow-up approval. This ADR only requires that any future design preserves Contracts/Client/Api isolation and does not make Intelligence own binary storage. |
| File input | File reference input | Files module upload/download/client contracts | Approved | Files owns binary transfer. Intelligence receives file references and metadata only. |
| Provider-internal streaming | Backend provider stream | Backend-only provider SDK streams | Approved as backend-only | Application/Infrastructure own provider streams and map them to contract-safe chunks at Api boundary. |
| Background Intelligence job status | Coarse status polling | HTTP GET polling | Not approved for current scope | May be approved later only for coarse background job status, not token/rewrite streaming. |

---

## Transport alternatives

### HTTP streaming

Accepted as the default for responder and rewrite server-to-client streaming.

Rationale:
- preserves ADR-0003 Client/Contracts/Api isolation;
- works naturally with protected endpoints;
- avoids hub lifecycle and connection-state complexity;
- fits request-scoped generated output;
- keeps provider streaming backend-only.

### Server-Sent Events

Rejected for the initial migration.

Rationale:
- explicit HTTP streaming frames are sufficient for the current requirement;
- SSE-specific event naming, reconnection, and replay semantics are not currently required;
- SSE may be reconsidered by follow-up ADR if those semantics become necessary.

### SignalR

Rejected as the initial primary transport.

Rationale:
- no current requirement justifies hub lifecycle, reconnect semantics, connection groups, or server-side realtime session state;
- SignalR may be reconsidered by follow-up ADR if Intelligence needs bidirectional realtime sessions, server push unrelated to a request, shared collaboration state, or long-lived realtime interaction.

### WebSocket

Rejected as the initial primary transport.

Rationale:
- current approved responder/rewrite streaming is server-to-client and request-scoped;
- WebSocket may be reconsidered only if a future transcription or realtime session design requires true low-latency bidirectional streaming that cannot be adequately implemented with protected HTTP request/response streaming.

### Polling

Rejected for token/rewrite/transcription progress.

Rationale:
- polling increases latency;
- duplicates state;
- creates inefficient repeated requests;
- is a poor fit for generated token streams.

Polling may be reconsidered only for coarse background job status in a future workflow.

### Direct Intelligence file upload

Rejected.

Rationale:
- Files already owns binary transfer, metadata, access control, temporary upload references, previews, and future storage conventions;
- Intelligence should consume file references, not become another file transport/storage owner.

---

## File input decision

Uploaded/input files are transferred only through Files.

Intelligence contracts may include:

- file ID;
- filename;
- content type;
- size;
- checksum;
- thumbnail or preview reference when already owned by Files;
- temporary upload reference issued by Files;
- caller-provided display label.

Intelligence contracts must not include:

- binary file content;
- raw base64 large file data;
- Files Domain models;
- Files Application DTOs;
- object-storage keys unless already approved as Files contract data;
- provider-specific uploaded-file handles unless mapped behind backend Application/Infrastructure.

Provider-specific file-upload handles, if needed by a provider SDK, must remain backend-owned and must not appear in frontend contracts.

---

## Tenant, user, session, and provider context

- Authentication and user identity are resolved at the protected endpoint boundary.
- Tenant context is resolved by host/API middleware and passed to Application through approved Application abstractions.
- Interface and Client must not accept arbitrary tenant identifiers as trusted authority.
- Session IDs may appear in contracts only for UI conversation/session continuity.
- Session IDs must not be treated as authorization authority.
- Provider selection may be requested through provider-safe contract options.
- Application/Infrastructure remain the source of truth for allowed providers, model registry, tenant/user provider access, and default provider routing.
- Background processing receives tenant/user/session/provider context through Application-owned commands/jobs, not Interface state.

---

## Minimum streaming contract semantics

Exact DTO names and serialization details belong in the implementation specification. However, every approved stream must preserve these semantics:

- every emitted chunk must be contract-safe;
- every stream must have an operation identifier or equivalent correlation value;
- every chunk must be ordered by sequence number, timestamp, or monotonic order value;
- terminal success must be explicit;
- terminal failure must be explicit;
- cancellation must be explicit;
- partial output must be explicitly marked;
- expected failures must be represented as contract-level outcomes, not leaked as provider exceptions;
- malformed stream frames must stop stream processing and return/yield a contract-level malformed-chunk outcome;
- client code must dispose response/readers/streams on cancellation, terminal failure, malformed chunk, or deserialization failure.

---

## Minimum failure semantics

All Client methods and streams return or yield explicit contract-level outcomes.

The Client must not collapse expected failures into raw exceptions.

| Failure | Required semantics |
| --- | --- |
| Cancellation | Caller-requested cancellation stops transport, disposes response/readers, and returns/yields a `Canceled` outcome. Partial output already received may remain visible and must be marked partial. |
| Timeout | Client or server timeout returns/yields `Timeout` or `ProviderTimeout`, depending on whether timeout occurred before or after provider dispatch. |
| Provider failure | Api maps Application/provider unavailable/outage failure to `ProviderUnavailable` or `ProviderFailed` without leaking provider internals. |
| Partial output | Streams may emit successful chunks before failure. Terminal chunk/result must indicate partial output and include the final error outcome. |
| Malformed chunk | Client stops reading, disposes the stream, returns/yields `MalformedChunk`, and marks accumulated output partial. |
| Deserialization failure | Client returns/yields `DeserializationFailure`, disposes transport, and does not retry automatically. |
| Authorization failure | Protected endpoint returns forbidden semantics when authenticated user lacks permission. Client maps to `Forbidden`. |
| Authentication failure | Protected endpoint returns unauthorized semantics when no valid identity/token is present. Client maps to `Unauthorized`. |
| Validation failure | Api returns structured validation error. Client maps to `ValidationFailure`. |
| Unsupported media type | Api returns unsupported media type. Client maps to `UnsupportedMediaType`. |
| File unavailable | Api/Application maps missing or inaccessible file reference to `FileUnavailable`. |
| Too-large payload | Api maps configured payload limits to `TooLargePayload`. |
| Unexpected failure | Api returns platform-safe unexpected failure. Client maps to `UnexpectedFailure`. |

Unexpected transport defects and programmer errors may still throw exceptions where returning a contract outcome would hide an implementation bug. Expected user, authorization, validation, timeout, provider, and malformed-stream failures must use contract-level outcomes.

---

## Minimum progress conventions

Exact enum names and DTO property names may be defined in the implementation specification. The following conventions are binding:

Every progress chunk must be contract-safe and include, where applicable:

- operation ID or correlation value;
- phase;
- sequence number, timestamp, or monotonic order value;
- optional percent only when meaningful;
- optional item/file reference;
- partial flag when output is incomplete;
- terminal flag for completion/failure/cancellation;
- error payload when terminal failure occurs.

The implementation specification may define module-specific phases such as:

- queued;
- provider selected;
- prompt accepted;
- token delta;
- rewrite delta;
- tool call requested;
- tool call approval required;
- tool call completed;
- completed;
- partial failed;
- canceled;
- file selected;
- file upload started;
- file upload progress;
- file upload completed;
- file reference attached;
- file rejected;
- file unavailable.

Transcription-specific phases must not be treated as production-approved until transcription upload/streaming semantics are accepted by this ADR amendment or a follow-up ADR.

---

## Validation and enforcement

The migration must satisfy the following checks:

- ADR-0003 validation reports no Intelligence Interface backend project references.
- `Intelligence.Interface` does not import `Application`, `Domain`, `Infrastructure`, or Files backend namespaces.
- `Intelligence.Interface` depends only on frontend-safe module libraries.
- `Intelligence.Api` endpoints use `MapProtectedModuleGroup`.
- `Intelligence.Client` methods accept `CancellationToken`.
- Streaming methods document cancellation and disposal semantics.
- Streaming Client implementation disposes transport resources on cancellation and terminal failure.
- Contracts remain serialization-only DTOs/enums/results/routes.
- Provider SDK types do not appear in Contracts, Client, or Interface.
- Files Application/Domain types do not appear in Intelligence Contracts, Client, or Interface.
- Temporary Intelligence exceptions remain `ReportOnly` while this ADR is `Proposed`.
- After this ADR is `Accepted`, migration is implemented, tests are added, and static validation passes, temporary Intelligence exceptions must be removed and Intelligence validation must become `FailFast`.

---

## Testing expectations

At minimum, implementation should include tests or validation for:

- forbidden project references;
- forbidden namespace imports;
- protected endpoint mapping;
- Client cancellation behavior;
- stream disposal behavior;
- malformed chunk handling;
- deserialization failure handling;
- partial-output terminal failure behavior;
- provider failure mapping;
- file-reference-only contracts;
- absence of provider SDK types in frontend-safe projects.

These tests may be unit tests, integration tests, static architecture tests, or a combination.

---

## Consequences

### Positive

- Intelligence Interface becomes frontend-safe.
- ADR-0003 dependency isolation becomes enforceable for Intelligence.
- Provider SDK and provider dispatch remain backend-only.
- File transfer ownership stays with Files.
- Streaming failure and partial-output semantics become explicit.
- The initial implementation avoids unnecessary realtime hub/session infrastructure.
- Future transport escalation requires explicit architecture approval.

### Negative

- Requires new Client, Api, and Contracts projects.
- Requires contract mapping between current Domain/Application models and API-safe DTOs.
- Requires careful stream framing and disposal implementation.
- Requires tests/static validation for boundary rules.
- Browser audio/transcription workflows may require a follow-up ADR before production streaming upload can be implemented.

### Neutral / trade-offs

- HTTP streaming is intentionally selected as the simplest approved transport for current responder/rewrite needs.
- The design keeps a path open for SignalR/WebSocket later, but does not pay that complexity cost now.
- Some implementation detail moves into companion specs rather than being frozen in this ADR.

---

## Alternatives considered

### Keep Application abstractions in Interface

Rejected.

Reason:
- violates ADR-0003;
- pulls backend code into frontend libraries;
- makes provider/internal model leakage more likely;
- prevents clean frontend/client/api separation.

### Move Domain models directly to Contracts

Rejected.

Reason:
- Contracts are serialization shapes only;
- Domain models may contain behavior, invariants, provider assumptions, or backend-only meaning;
- copying Domain models into frontend contracts would turn Contracts into a disguised Domain dependency.

### Use SignalR as the primary transport immediately

Rejected.

Reason:
- no current requirement justifies hub lifecycle, reconnect semantics, groups, or server-side connection state;
- SignalR may be approved later only if realtime session semantics are actually needed.

### Use WebSocket as the primary transport immediately

Rejected.

Reason:
- current approved responder/rewrite streaming is request-scoped server-to-client streaming;
- WebSocket should be reserved for proven bidirectional low-latency requirements.

### Use SSE as the primary transport immediately

Rejected.

Reason:
- current requirements do not need SSE reconnection/event semantics;
- explicit framed HTTP streaming is sufficient for the initial migration.

### Upload files directly through Intelligence endpoints

Rejected.

Reason:
- Files owns binary transfer and metadata;
- direct Intelligence upload would create a second file-transport boundary;
- any future exception requires explicit architecture approval.

### Approve transcription upload semantics in this ADR

Rejected for now.

Reason:
- transcription may require request streaming, chunk upload, SignalR, WebSocket, or a hybrid model;
- approving it without an exact upload/backpressure/cancellation design would create implementation ambiguity;
- a follow-up ADR should decide the exact transport if production transcription streaming is required.

---

## Follow-up decisions

Create follow-up ADRs if any of the following become required:

- production transcription upload/streaming semantics;
- true bidirectional realtime Intelligence sessions;
- SignalR hub model;
- WebSocket transport;
- SSE reconnect/replay semantics;
- direct Intelligence-owned upload;
- resumable upload;
- range requests;
- background Intelligence job polling/status model;
- provider-specific file-upload handle lifecycle visible outside backend Application/Infrastructure.

---

## Notes

This ADR intentionally approves the smallest transport surface needed for the Intelligence ADR-0003 migration.

It approves protected JSON HTTP endpoints for non-streaming work and protected HTTP streaming for responder/rewrite server-to-client streaming.

It does not approve direct large-file upload, resumable upload, range requests, SignalR, WebSocket, SSE, or polling as primary Intelligence transports.

Those require explicit architecture approval before implementation.
