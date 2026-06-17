# Knowledge Source Import Journey

## 1. Context
- Product / service: Vitarish Knowledge module source import
- Journey type: Current-state owner command journey
- Entry point: `/owner/knowledge/corpora` via the Import section of `KnowledgeOwnerSurface`
- User goal: Paste markdown or plain-text content into one existing tenant-scoped corpus.
- Business goal: Convert source content into canonical Knowledge source, document, and chunk state for retrieval and later diagnostics.

## 2. Current Implementation Snapshot
| Area | Current behavior |
| --- | --- |
| Route | `AppRoutes.Knowledge.OwnerCorpora` maps to `/owner/knowledge/corpora`. |
| Page / surface | `KnowledgeOwnerCorporaPage` renders `KnowledgeOwnerSurface`. |
| Command contract | `IKnowledgeService.ImportAsync` is inherited from `IKnowledgeImporter`. |
| Import commands | `SaveKnowledgeImportCommand` and `ReplaceKnowledgeSourceImportCommand` represent persisted import writes. |
| Input formats | The UI offers `markdown` and `plain-text`. |
| Domain state | KnowledgeSource, KnowledgeDocument, KnowledgeChunk, and optional KnowledgeQuestion (for evaluation) are created for the selected KnowledgeCorpus. |
| Feedback | Import result summarizes source, document, and chunk counts. |

## 3. Happy Path
1. Owner opens the Knowledge corpora page after a corpus exists.
2. Owner chooses the target corpus from the Import selector.
3. Owner enters a source name.
4. Owner selects `markdown` or `plain-text`.
5. Owner pastes source content.
6. Owner clicks **Import**.
7. `KnowledgeOwnerSurface` validates tenant id, selected corpus id, and non-empty content.
8. The surface sends a `KnowledgeImportRequest` to `IKnowledgeService.ImportAsync` with tenant id, corpus id, format, source name, and content stream.
9. Import infrastructure persists the canonical source, document, and chunk state through the import command flow.
10. System shows `Import complete.` and count feedback for created sources, documents, and chunks.

## 4. Alternate and Failure Paths
- No selected corpus -> block import and show `CorpusId must be a GUID.`
- Missing tenant id -> block import and show `TenantId is required.`
- Empty pasted content -> block import and show `Paste text before importing.`
- Unsupported or malformed import content -> importer returns an explicit failure and the page shows that message.
- Replacement import path -> `ReplaceKnowledgeSourceImportCommand` is the write boundary for replacing one source without creating ambiguous ownership of source state.

## 5. UX and Guardrail Rules
1. Import is a command journey; diagnostics for imported sources and chunks belong in the diagnostics journey.
2. The selected corpus id and tenant id must be validated before any import write.
3. Source content must become canonical `KnowledgeSource`, `KnowledgeDocument`, and `KnowledgeChunk` state; UI controls must not hold a competing persisted model.
4. The import result may display counts but must not serve as the authoritative long-term source list.
5. Do not index implicitly during import; indexing remains a separate command journey.

## 6. Acceptance Criteria
- Owner can import non-empty markdown or plain-text into a selected corpus.
- The import command creates source, document, and chunk state for the corpus.
- The UI displays imported source, document, and chunk counts.
- Invalid tenant, corpus, or empty content prevents the command with a visible message.
- Import does not silently index, evaluate, or ask questions.
