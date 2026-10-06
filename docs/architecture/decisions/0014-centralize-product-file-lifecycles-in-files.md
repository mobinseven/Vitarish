# ADR-0014: Centralize product file lifecycles in Files

## Status

Accepted for staged implementation on 2026-09-29.

## Decision

Files is the authoritative owner of product runtime file bytes, identity,
metadata, integrity, storage, temporary lifecycle, verified delivery, and file
transfer audit events. The Files Web package owns selection, transfer state,
previews, downloads, and localized file errors. This decision does not apply to
source assets, configuration, certificates, application logs, database storage,
build output, or developer tooling, which retain their current infrastructure
owners.

Owning features remain authoritative for whether a subject may attach, import,
or export data for a resource; for relationship metadata and persistence; and
for feature content such as Knowledge parsing, medical import rules, CSV
contents, and transcription. Files requests those narrow authorization
capabilities through Application-owned ports registered in composition; Files
does not reference feature implementations. Audit remains authoritative for
event persistence, catalog, and review.

Files will expose additive preparation, bounded receive, authorized open,
reference claim/release, and generated-delivery capabilities. A preparation
response carries a Files-owned transfer identity, effective policy, and expiry.
The server independently validates streamed size, declared media type, detected
format, and container structure, computes SHA-256, verifies stored content,
and maintains opaque storage keys. A file's temporary/persistent lifecycle is
separate from its integrity state. Unclaimed uploads expire after 24 hours;
cleanup runs every five minutes and retries failed deletion. Claims and cleanup
are idempotent, with reservations protecting content while the owning feature
confirms its relationship.

The existing 64 MiB ceiling remains. Multipart uploads use bounded parsing even
when `Content-Length` is absent, and browser uploads retain the standard
authenticated transport's antiforgery protection. The default transfer-start
limit is ten attempts per minute per authenticated tenant/user, including
rejected attempts. Generic attachment policy permits PDF, PNG/JPEG/WebP,
TXT/Markdown/CSV/JSON, DOCX/XLSX, and WebM/WAV/MP3/MP4; narrower policies own
Knowledge, avatar, and audio restrictions.

## Rollout and compatibility

Deploy additive Files contracts first, preserving existing routes and mapping
their responses additively to the Files reference. Migrate feature attachments
and previews next, then imports, exports, and retained provider artifacts.
Finally, use generated forward-only migrations and resumable backfill to move
legacy bytes and relationships. Backfill verifies bytes and ownership before a
reference changes, preserves source content until reconciliation succeeds, and
leaves unknown ownership inaccessible pending authorized remediation. Applied
migrations are never rewritten.

Compatibility adapters must use the same Files capabilities and owner
authorization; they may not retain unrestricted storage access. Remove them
only after all supported consumers migrate, reconciliation completes, and one
release cycle reports no legacy use. Rollback during expansion retains legacy
data and does not reverse applied migrations.

## Consequences

This removes feature-specific browser file readers, transfer serialization,
Files route construction, preview URL management, download anchors, direct
object storage coordination, and consumer integrity checks as each consumer
migrates. Intelligence continues to own live microphone capture and realtime
transcription, but any retained, previewed, uploaded, or downloaded recording
uses Files. Browser cleanup revokes object URLs and aborts transfers on
replacement, cancellation, unmount, and authentication/tenant changes; it must
not be represented as guaranteed secure erasure.

Successful transfers, denials, failures, integrity blocking, and cleanup
outcomes use typed audit catalog values with redacted reasons. Authentication
denials before endpoint execution are captured at the host boundary without
inventing tenant or actor identities.

## Rejected alternatives

- Letting each feature coordinate object storage and checksum verification
  duplicates a cross-user security boundary and permits policy drift.
- Giving Files feature authorization implementations couples the provider to
  feature internals and reverses Application dependency direction.
- Treating live audio streams as files would blur Intelligence's realtime
  streaming responsibility with retained file processing.
- Replacing existing routes before consumer migration would break generated
  clients and supported integrations.
