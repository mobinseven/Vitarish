# Knowledge Indexing Journey

## 1. Context
- Product / service: Vitarish Knowledge module retrieval indexing
- Journey type: Current-state owner command journey
- Entry point: `/owner/knowledge/corpora` via the Index section of `KnowledgeOwnerSurface`
- User goal: Index the chunks of one tenant-scoped corpus so retrieval and grounded QA can use them.
- Business goal: Keep searchable retrieval state aligned with canonical imported chunks while tracking index runs.

## 2. Current Implementation Snapshot
| Area | Current behavior |
| --- | --- |
| Route | `AppRoutes.Knowledge.OwnerCorpora` maps to `/owner/knowledge/corpora`. |
| Page / surface | `KnowledgeOwnerCorporaPage` renders `KnowledgeOwnerSurface`. |
| Command contract | `IKnowledgeService.IndexAsync` delegates indexing behavior exposed by `IKnowledgeIndexer`. |
| Index state | `KnowledgeIndexRun` records corpus id, tenant id, indexed chunk count, scope, and optional source id. |
| Source state | `KnowledgeChunk` is the canonical content unit being indexed. |
| Feedback | Index result displays indexed chunk count. |

## 3. Happy Path
1. Owner imports or otherwise prepares chunks for a corpus.
2. Owner selects that corpus in the Index selector.
3. Owner clicks **Index corpus**.
4. `KnowledgeOwnerSurface` validates tenant id and selected corpus id.
5. The surface calls `IKnowledgeService.IndexAsync`.
6. Indexing reads stored chunks for the tenant-scoped corpus and updates the retrieval index.
7. Indexing records a `KnowledgeIndexRun` for observability.
8. System shows `Index complete.` and the indexed chunk count.

## 4. Alternate and Failure Paths
- No selected corpus -> block indexing and show `CorpusId must be a GUID.`
- Missing tenant id -> block indexing and show `TenantId is required.`
- Corpus has no chunks -> indexer returns an explicit zero-count or failure result rather than inventing indexed content.
- Search/index dependency failure -> show explicit failure and leave the owner in control to retry.
- Source-specific reindex is available through `IKnowledgeIndexer.ReindexSourceAsync` but is not the primary current owner page flow.

## 5. UX and Guardrail Rules
1. Indexing is a command; source and chunk inspection remain read-only diagnostic queries.
2. The indexed representation is derived from `KnowledgeChunk`; it is not a second source of truth for content.
3. Index completion feedback must state chunk counts, not claim answer quality.
4. Do not combine import and index into one hidden action in this journey.
5. Keep index diagnostics separate from the indexing command so owners can distinguish write completion from retrieval health.

## 6. Acceptance Criteria
- Owner can index a selected tenant-scoped corpus.
- Invalid tenant or corpus input prevents indexing with a visible message.
- A `KnowledgeIndexRun` records the indexing outcome.
- Indexed chunk count is visible after success.
- The journey does not mutate source content or run evaluation implicitly.
