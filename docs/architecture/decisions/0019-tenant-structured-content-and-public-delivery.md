# ADR-0019: Tenant-structured content and public delivery

Date: 2026-11-05
Status: Accepted
Scope: Contents module and `shared/contents`, Landing app, landing and clinic web apps
Owner: Architecture
Supersedes:
Superseded by:
Related: [ADR-0004](0004-protect-internal-module-api-endpoints-by-default.md), [ADR-0012](0012-module-owned-agents-with-guarded-composition.md), [ADR-0017](0017-conversational-front-door-and-shared-agent-interaction.md)

## Context

Marketing and editorial copy for the landing page, clinic pages and future
sites lives in localization catalogs and page components. Contents stores
items with a fixed type list and free-form body blocks, exposes them only to
the tenant owner, and has no publication model, so no website reads from it.
The owner wants one content system that every site uses, edited through a UI
and by AI assistants, with each tenant able to define its own content shapes.

## Decision

1. **Sites and content types belong to a tenant.** A site is a publishing
   target inside a tenant. A content type is a stored, versioned schema of
   fields (text, long text, rich text, number, boolean, date, media, link,
   reference, list, group), each optionally localized. Schema changes are
   additive: a field's key, kind and localization never change. Each content
   item pins the schema version it was written against.
2. **Rich text is Tiptap JSON** with a versioned node and mark allow-list,
   size and depth limits, and only `https` or site-relative links. It is
   validated on the server and rendered as React elements, never as HTML.
3. **Revisions are immutable and authoritative.** Every save writes a new
   revision with a version allocated inside the save transaction. An item has
   a working revision and at most one published revision. Publishing points
   the published revision at an exact revision; restoring creates a new
   working revision. Saves carry the expected version and fail with a
   conflict when it is stale. Drafts may be incomplete; publishing requires a
   complete, schema-valid revision.
4. **Publication side effects go through a Contents outbox** written in the
   publishing transaction. A background processor synchronizes the search
   index from the published snapshot, with retries and no duplicate effect.
5. **Public delivery is anonymous and read-only.** Endpoints return only
   published snapshots of the site resolved from the request's tenant host,
   reject requests with no resolved tenant, are rate limited, and return
   `ETag`, `Cache-Control` and `Vary` on the tenant-selecting headers.
   Published content is public by definition, so tenant selection by host
   header grants nothing beyond what the site already publishes. Website
   servers forward their own host when fetching during rendering.
6. **Authoring stays with the tenant owner.** Management endpoints remain
   protected and require the owner of the resolved tenant. The landing site
   is edited on the landing host by the owner of the existing landing tenant;
   clinic sites are edited in the clinic owner app. Platform administration
   grants no authoring access to another tenant.
7. **AI writes drafts only.** `Contents.Agents` tools call the same
   authorized, concurrency-checked draft command; they cannot publish.
8. **Marketing copy moves page by page.** Each page reads its published
   entry and keeps its catalog copy as a fallback only until that page's
   seeded entry is published and verified; then the catalog copy is removed.
   Seeds create missing records and never overwrite editorial changes.

## Consequences

- Websites change copy without a release. Content is eventually visible:
  delivery responses and rendered pages may be up to the cache lifetime old.
- The legacy item fields (fixed type and body blocks) and the existing item
  endpoints stay readable and writable during a compatibility window. They
  are removed when no consumer uses them and existing items are migrated to
  content types.
- The schema is expanded with nullable columns first; constraints follow once
  data is backfilled.
- Public media delivery needs a published-reference path from Files; until it
  exists, media fields are limited to already-public URLs.
- Per-tenant schemas cost a schema validator and a type editor that
  code-defined types would not need.

## Alternatives Considered

### Code-defined content types

Simpler and type-checked, but every new shape needs a release. Rejected by the
owner in favour of tenant-defined types.

### A new content aggregate beside `ContentItem`

Cleaner model, but two parallel content paths. Rejected; `ContentItem` is
extended and its legacy fields retired through a compatibility window.

### Authoring landing content from platform administration

Would require cross-tenant writes from a platform role. Rejected; the landing
tenant's owner authors on the landing host.

### Do nothing

Copy stays in releases and no site reads Contents.
