# Files module operating contract

Files owns byte transfer, computed checksums, storage verification, quarantine,
and verified file references. It does not own the caller's tenant/resource
authorization, business purpose, or data classification.

## Authorization and transfer

- **[MUST]** The owning module/app authorizes tenant, subject, resource, and
  purpose before Files opens a download/import stream or commits an upload.
  A valid file ID, presigned URL, checksum, transfer metadata, or successful
  virus/integrity check never grants access.
- **[MUST]** Treat filenames, paths, media types, sizes, claimed checksums,
  object metadata, presigned callbacks, and storage responses as untrusted.
  Normalize and validate at the boundary; do not use user input as an object
  path/key without an owned safe mapping.
- **[MUST]** Stream content with explicit cancellation and size ceilings. Do
  not buffer an unbounded file. Clear rented buffers containing sensitive data
  before return and remove temporary files on success, cancellation, and every
  failure path.

## Integrity and storage

- **[MUST]** Compute SHA-256 server-side while receiving content, compare any
  claimed value in constant-time form where applicable, and persist the
  computed value as authoritative. Never trust a browser/provider checksum as
  the stored integrity decision.
- **[MUST]** Validate object existence, length, metadata, and content checksum
  before marking a file verified. A mismatch or missing object transitions to
  an explicit unavailable/quarantined state, blocks download/import, records
  audit evidence, and returns only a safe authorized remediation path.
- **[MUST]** Storage writes, completion callbacks, retries, background scans,
  and legacy enrollment define idempotency, concurrency, partial-failure,
  cancellation, and resumability. The initiating boundary owns retry; do not
  stack SDK and job retries around the same mutation without a documented
  reason.
- **[MUST]** Remote object storage uses the shared safe TLS boundary with
  hostname and certificate validation. Do not add storage-specific certificate
  bypasses or permit plaintext remote storage in a supported topology.
- **[MUST]** File integrity state, checksum algorithm/value, object identity,
  quarantine state, and verified-reference contracts are persisted/public
  compatibility boundaries. Use generated forward-only migrations and
  additive contracts; never hand-edit migration output or generated clients.

## Proof

- **[MUST]** Tests cover oversized and disallowed content, forged checksum,
  cross-tenant/resource denial before stream access, corrupt/missing objects,
  quarantine, buffer/temp cleanup, cancellation, duplicate completion,
  dependency failure, retry/idempotency, legacy enrollment, and blocked export.
  Run focused Files tests plus the endpoint/client validators required by the
  parent module contract; browser-visible transfer changes require real
  two-host E2E proof.
