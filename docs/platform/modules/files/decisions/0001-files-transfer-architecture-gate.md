# Files transfer architecture gate

## Status

Accepted for current migration scope.

## Decision

The current Files UI migration supports simple metadata lookup, checksum duplicate lookup, file upload, and file download through the module HTTP client/API boundary. The current UI does not require resumable uploads, signed URLs, virus scanning workflow state, external direct-to-blob upload, or long-running processing orchestration.

## Gate outcomes

| Capability | Required now? | Decision |
| --- | --- | --- |
| Large file transfer | No | Current browser-selected file upload uses a single HTTP request and preserves UI progress only. |
| Resumable upload | No | Not implemented without a separate approved architecture. |
| Range requests | No | The migrated content endpoint returns full content only. |
| Signed URLs | No | Downloads route through the Files API. |
| Upload/download progress conventions | Yes, UI-local only | Existing upload progress remains Interface state; no platform-wide convention is introduced. |
| Realtime transfer progress | No | Progress is local to the uploading component stream. |
| Streaming as primary transport | No | HTTP multipart upload is the API transport; streaming remains an implementation detail at the API/Application boundary. |
| Virus scanning status | No | Not represented in the current UI. |
| External blob storage direct upload | No | Storage authority remains server-side. |
| Long-running processing | No | Upload completes within the request lifecycle. |

## Consequences

- `Files.Contracts` contains serialization shapes only.
- `Files.Client` owns HTTP transfer behavior and cancellation/error mapping.
- `Files.Api` owns HTTP binding and maps to existing Application services.
- User and tenant authority are resolved server-side by existing Application/Infrastructure context rather than Interface input.
