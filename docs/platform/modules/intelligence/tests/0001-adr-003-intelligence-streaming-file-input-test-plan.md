# ADR-003 Intelligence streaming and file-input test plan

Date: 2026-06-25
Status: Suggested
App: Platform
Module: Intelligence
Capability: ADR-003 frontend/backend isolation, accepted streaming, and Files-owned file input
Related ADRs: docs/platform/modules/platform/decisions/0003-isolate-module-interface-libraries-with-api-client-contracts.md; docs/platform/modules/platform/decisions/0004-protect-internal-module-api-endpoints-by-default.md; docs/platform/modules/intelligence/decisions/0001-intelligence-realtime-streaming-and-file-input.md
Confidence: Medium

## Objective

Protect the Intelligence ADR-003 migration by proving contracts serialize safely, the frontend client maps transport failures explicitly, protected API endpoints adapt contracts to Application without leaking backend types, streaming disposes resources, and file input remains Files-owned.

## Required unit and contract tests

| Test | Category | Target | Expected result |
| --- | --- | --- | --- |
| Contract serialization round trip | Contract | Intelligence request/result/error/chunk/file-reference DTOs | DTOs serialize with no Domain, Application, provider SDK, binary content, or Files backend DTO dependency. |
| Client non-streaming failure mapping | Unit | Intelligence client HTTP status/transport mapping | Validation, unauthorized, forbidden, unsupported media type, file unavailable, too-large payload, provider unavailable, provider timeout, cancellation, deserialization, and unexpected failures map to `IntelligenceErrorCode`. |
| Client malformed chunk handling | Unit | Streaming reader | Malformed or out-of-order newline JSON chunks emit `MalformedChunk` or `DeserializationFailure` terminal failures. |
| API mapping tests | Integration | `MapIntelligenceApi` endpoints | Protected endpoints use contract DTOs, call Application abstractions, and return contract responses/chunks. |

## Required hot test scenarios

| Scenario | Risk protected | Environment | Trigger | Expected evidence |
| --- | --- | --- | --- | --- |
| Streaming cancellation and disposal | Response/reader leaks after cancellation | Browser or integration host with streaming provider stub | Cancel responder/rewrite stream mid-output | Client emits cancellation/failure state and server request aborts without leaked response/content/stream resources. |
| Partial stream failure | Provider stream dies after partial output | API host with provider stub that throws mid-stream | Start responder/rewrite stream | Terminal `PartialStreamFailure` or failed stream chunk is visible; partial text remains presentation state only. |
| Provider timeout/outage | Provider unavailability leaks raw backend exceptions | API host with provider timeout/outage stub | Non-streaming and streaming calls | Contract error is `ProviderTimeout` or `ProviderUnavailable`; no provider SDK type leaves backend. |
| Browser audio capture failure | UI media APIs unavailable or denied | Browser with denied microphone permission | Start transcription capture | Interface reports supported/recording state safely and does not call backend with trusted tenant/user data. |
| File input rejection | Intelligence becomes binary upload owner | Browser/API host with Files reference flow | Attach unsupported/unavailable/oversized file reference | Files owns transfer; Intelligence receives only reference metadata and returns explicit file/size/media errors. |
| Tenant/user authorization | Interface/client cannot forge authority | Authenticated host with multiple tenants/users | Attempt cross-tenant provider/model/file use | Protected API/host context determines identity; Application rejects unauthorized provider/model/file access. |

## CI / execution guidance

Run ADR-003 static validation after code changes:

```bash
python tools/architecture/validate_interface_boundaries.py --config tools/architecture/interface_boundary_config.json
```
