# User-data protection delivery plan

## Outcome and boundary

Close UD-1 through UD-11 for existing user-owned records, metadata, authenticated
user data, stored file content, and their existing JSON and binary transfer routes.
This work does not add routes, export formats, bulk export, rate limiting,
providers, protocols, malware scanning, automatic restoration, external alerts,
or an administrative workflow.

The currently checked discovery source is `dotnet run --project
tools/architecture -- security-baseline`. It enumerates every mapped endpoint,
emits its user-data operation disposition (owner source, trusted session,
operation, authorization declaration, transfer, format/size boundary, secure
channel, audit disposition, and rate selection), and rejects an incomplete
disposition. It remains source-level evidence; it does not prove that an
Application use case enforces every declared authorization rule at runtime.

Import-frequency restrictions are deliberately not selected: the questionnaire
makes them optional and the product has no current requirement for them.

## Current repository evidence and gaps

| Surface | Current owner and evidence | Required correction |
| --- | --- | --- |
| Files multipart upload | `src/Modules/Files/Api/FilesApiEndpointMapping.cs` maps the only `MultipartUpload` route. Its boundary declares a 64 MiB content ceiling plus bounded multipart overhead, normalizes matching declared and actual media types, authorizes through `IFileTransferAuthorization` before opening the content stream, and records boundary denials. `FilesService` accepts only the existing MIME-map-recognized format set, bounds streamed bytes, compares the claimed SHA-256 in constant time, clears consumed buffers, only reuses a same-tenant verified record, and writes a redacted import outcome through the audit writer. | Resource/purpose authority remains the owning caller's responsibility; the direct Files route is tenant-scoped by its trusted request context. |
| Files download | The same mapping owns `BinaryDownload`; `FilesDbContext` has a tenant query filter. `FileIntegrityRecord` now holds a per-file object key, digest, length, type, state, projection version, and verification time; unrecorded legacy content is unavailable and verified downloads recompute SHA-256 before streaming. An integrity record exists only while its file remains an expected protected object: controlled deletion removes it, while an unexpected physical disappearance remains observable. The existing Files recurring scheduler enrolls and verifies one unresolved legacy item per tenant at a time; after legacy enrollment is complete, it rechecks the least-recently-verified file. Pending records remain eligible for provider recovery and every result is audited. Allowed and denied export attempts use the same audit writer, and provider verification failures return a safe dependency failure with failed audit evidence. | Owning callers must authorize resource and purpose before invoking Files; the direct Files route is tenant-scoped by its trusted request context. |
| File storage | `AwsS3ObjectStorageService` is the only production object-storage adapter. Its read API now returns a provider response stream rather than copying an object into a `MemoryStream`; the consumer owns disposal. | Keep dependency failure behavior and live-provider stream disposal covered by focused tests. |
| Knowledge file import | `KnowledgeImportFileReader` opens Files content for the existing knowledge-import feature. It now asks Files to verify the tenant-scoped integrity record and uses its opaque per-file object key before activating the provider-facing content stream. | The owning Knowledge Application must continue to authorize the knowledge source's resource and purpose before this reader is invoked. |
| SEA attachments | SEA's existing attachment endpoints authorize through the Application-owned `ISeaResourceAccess` boundary before attachment DTO/content metadata is loaded. `AttachmentIntegrityProjection` is an explicit versioned Domain projection; an additive generated record is transactionally synchronized with attachment writes. A generated no-action relationship preserves an expected record when an attachment disappears unexpectedly, so recurring verification marks it unavailable and audits failure; an authorized attachment deletion explicitly removes its record in the same write transaction. Tenant-scoped recurring verification enrolls legacy attachments, rechecks verified records, marks a mismatch unavailable, records the integrity result, and the attachment query filter hides legacy/pending/unavailable data. | Keep the Application access boundary authoritative and add any newly discovered owner-specific controls through the same pattern. |
| Sensitive non-file records | SEA attachment data now has a versioned projection, generated additive record, baseline enrollment, recurring verification, unavailable state, query-level safe response, and an audit result. | Discover and implement the same owner-specific control for every other sensitive non-file owner; do not treat the endpoint inventory alone as integrity coverage. |

## Ownership and dependency direction

- Owning Applications retain tenant, active subject, session-limit, resource,
  capability, and purpose decisions. Transport, a file identifier, tenant query
  filtering, browser visibility, or a storage check do not grant access.
- Files owns byte streaming, declared transfer validation, checksum calculation,
  object verification, quarantine, and verified file references. It does not
  decide a caller's business purpose.
- Each sensitive non-file record owner defines its projection; shared storage
  must not infer business fields by reflection or generic serialization.
- Audit uses the existing `ISecurityAuditWriter` and
  `SecurityAuditEventCatalog`; no parallel logger or audit store is introduced.
- API maps transport and safe results only. Infrastructure owns provider and EF
  implementation. Migrations are generated from model changes and are
  forward-only.

## Independently valid delivery slices

1. Extend the checked endpoint/user-data inventory so every enabled sensitive
   operation names its Application owner, authorization boundary, direction,
   category, media/format/size declaration, secure channel, and audit operation.
   The evaluated profile fails closed for unclassified sensitive routes.
2. Make the Files upload/download path bounded, tenant-safe, checksum-verified,
   and auditable, with generated additive file integrity persistence and legacy
   enrollment. Verify the direct Files route and SEA attachment callers before
   stream/provider access.
3. Add owner-specific integrity projections and records for the discovered
   non-file sensitive owners, followed by resumable baseline enrollment and the
   minimum recurring checks. A mismatch or missing expected object becomes an
   authorized safe unavailable/quarantined result and audit warning.
4. Add focused owner, endpoint, and real two-host regression proof. Inspect the
   fresh authenticated artifact and retain only the required proof artifacts.

## Acceptance and failure criteria

Every slice must prove authorization denial before stream/provider access,
rejection before persistence for unsupported/oversize input, cancellation
cleanup, no cross-tenant duplicate disclosure, safe dependency failure, and
audited integrity success/failure. Existing public routes, operation IDs,
request/response shapes, and generated client inputs remain stable unless an
authoritative contract source requires an additive change and its generator is
run.

The implementation stops for a revision when a new MIME type, format, route,
bulk export, rate rule, provider, or transfer destination is requested.
