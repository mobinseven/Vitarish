# Knowledge Source Import Journey

## 1. Context
- Product / service: Vitarish Knowledge module source import
- Journey type: Current-state owner command journey
- Entry point: `/owner/knowledge/corpora` via the Import section of `KnowledgeOwnerSurface`
- User goal: Paste supported text, markdown, QA JSON, or evaluation JSON content into one existing tenant-scoped corpus, or explicitly replace one existing source.
- Business goal: Convert source content into canonical Knowledge source, document, and chunk state for retrieval and later diagnostics.

## 2. Current Implementation Snapshot
| Area | Current behavior |
| --- | --- |
| Route | `AppRoutes.Knowledge.OwnerCorpora` maps to `/owner/knowledge/corpora`. |
| Page / surface | `KnowledgeOwnerCorporaPage` renders `KnowledgeOwnerSurface`. |
| Command contract | `IKnowledgeService.ImportAsync` and `IKnowledgeService.ReplaceSourceAsync` are inherited from `IKnowledgeImporter`. |
| Import commands | `SaveKnowledgeImportCommand` and `ReplaceKnowledgeSourceImportCommand` represent persisted import writes. |
| Input formats | The UI offers `markdown`, `plain-text`, `techqa-json`, `rag-eval-json`, and `plain-qa-json`; parsing rules remain centralized in `KnowledgeImportContentReader`. |
| Domain state | KnowledgeSource, KnowledgeDocument, KnowledgeChunk, and optional KnowledgeQuestion (for evaluation) are created for the selected KnowledgeCorpus. |
| Feedback | Import result summarizes source, document, and chunk counts. |

## 3. Happy Path
1. Owner opens the Knowledge corpora page after a corpus exists.
2. Owner chooses the target corpus from the Import selector.
3. Owner enters a source name.
4. Owner selects `markdown`, `plain-text`, `techqa-json`, `rag-eval-json`, or `plain-qa-json`.
5. Owner pastes source content or a JSON payload matching the selected format.
6. Owner clicks **Import**.
7. `KnowledgeOwnerSurface` validates tenant id, selected corpus id, selected format, and non-empty pasted content without duplicating importer JSON-shape rules.
8. The surface sends a `KnowledgeImportRequest` to `IKnowledgeService.ImportAsync` with tenant id, corpus id, format, source name, and content stream.
9. Import infrastructure persists the canonical source, document, chunk, and optional evaluation-question state through the import command flow.
10. System shows `Import complete.` and count feedback for created sources, documents, and chunks.

## Component Coverage Stages

| Stage | Components | User-visible role |
| --- | --- | --- |
| Choose source lifecycle action | `KnowledgeOwnerManagementSurface`, `KnowledgeOwnerCorpusCollection`, `KnowledgeOwnerCorpusCollectionItem` | Keep source import, replacement, and deletion scoped to one visible corpus card. |
| Import file-backed source | `KnowledgeSourceImporter`, `KnowledgeSourcePayloadFileInput` | Capture source name, input format, and uploaded file payload before issuing an explicit import command. |
| Review imported source | `KnowledgeOwnerSourceCollectionItem`, `KnowledgeOwnerSourceFileCollectionItem` | Display imported source and file metadata as collection items inherited from canonical source state. |
| Replace existing source | `KnowledgeSourceReplacer`, `KnowledgeSourceSelector`, `KnowledgeSourcePayloadFileInput` | Select one existing source and submit a new payload as an explicit replacement command. |
| Delete source | `KnowledgeSourceDeleter`, `KnowledgeOwnerSourceCollectionItem` | Confirm deletion of one imported source without hiding source ownership or deleting implicitly. |
| Label source format and report status | `KnowledgeSourceInputFormatLabels`, `KnowledgeOwnerStatus` | Keep supported format labels centralized and show import/replacement/deletion outcomes. |

## 4. Alternate and Failure Paths
- No selected corpus -> block import and show `CorpusId must be a GUID.`
- Missing tenant id -> block import and show `TenantId is required.`
- Empty pasted content -> block import and show `Paste content before importing.`
- Unsupported or malformed import content -> importer returns an explicit failure and the page shows that message.
- Owner selects or pastes an existing source id and clicks **Replace source** -> the page validates tenant id, corpus id, source id, selected format, and content, then sends the same `KnowledgeImportRequest` shape to `IKnowledgeService.ReplaceSourceAsync`.
- Missing replacement source id -> block replacement and show `Source ID is required for replacement.`
- Replacement import path -> `ReplaceKnowledgeSourceImportCommand` is the write boundary for replacing one source without creating ambiguous ownership of source state.

## 5. UX and Guardrail Rules
1. Import is a command journey; diagnostics for imported sources and chunks belong in the diagnostics journey.
2. The selected corpus id and tenant id must be validated before any import write.
3. Source content must become canonical `KnowledgeSource`, `KnowledgeDocument`, and `KnowledgeChunk` state; UI controls must not hold a competing persisted model.
4. The import result may display counts but must not serve as the authoritative long-term source list.
5. Do not index implicitly during import or replacement; indexing remains a separate command journey.
6. Create-import and replace-import remain separate state-changing controls so owners explicitly choose whether to add a source or replace one.

## 6. Acceptance Criteria
- Owner can import non-empty markdown, plain-text, TechQA JSON, RAG evaluation JSON, or Plain QA JSON into a selected corpus.
- Owner can select or enter an existing source id and explicitly replace that source with supported pasted content.
- The import command creates source, document, chunk, and optional evaluation-question state for the corpus.
- The UI displays imported source, document, and chunk counts.
- Invalid tenant, corpus, source id for replacement, format, or empty content prevents the command with a visible message.
- Import does not silently index, evaluate, or ask questions.
