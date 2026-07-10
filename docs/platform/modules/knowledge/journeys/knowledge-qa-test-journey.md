# Knowledge QA Test Journey

## 1. Context
- Product / service: Vitarish Knowledge module grounded QA testing
- Journey type: Current-state owner/tester query journey
- Entry point: `/owner/knowledge/qa` via `AppRoutes.Knowledge.QA`
- User goal: Ask a question against one corpus and inspect the grounded answer, retrieved context, missing information, and citations.
- Business goal: Validate retrieval-grounded answer behavior without changing corpus, source, chunk, or index state.

## 2. Current Implementation Snapshot
| Area | Current behavior |
| --- | --- |
| Route | `AppRoutes.Knowledge.QA` maps to `/owner/knowledge/qa`. |
| Page / surface | `KnowledgeTestQaPage` renders `KnowledgeTestQaSurface`. |
| Query contract | `IKnowledgeService.AskAsync` accepts `KnowledgeAskRequest`. |
| Retrieval / answer contracts | `IKnowledgeRetriever` retrieves hits and `IKnowledgeAnswerer` produces the grounded answer. |
| Trace marker | UI sends `TraceKind = "manual-ui-test"`. |
| Displayed output | IsAnswered, answer text, missing information, retrieved context, and citations. |

## 3. Happy Path
1. Owner or tester opens the Knowledge QA page.
2. User enters tenant id, corpus id, question, and TopK.
3. User clicks **Ask**.
4. `KnowledgeTestQaSurface` validates tenant id, corpus GUID, non-empty question, and TopK of at least 1.
5. The surface sends a `KnowledgeAskRequest` to `IKnowledgeService.AskAsync`.
6. Retrieval returns ranked `KnowledgeHit` context for the corpus.
7. Answering returns `KnowledgeAnswer` with answer/refusal status, citations, and missing-information detail.
8. System shows `Ask complete.` with retrieved context cards and citation cards.

## Component Coverage Stages

| Stage | Components | User-visible role |
| --- | --- | --- |
| Open QA test surface | `KnowledgeTestQaSurface` | Present owner/tester corpus selection, question entry, TopK control, and management navigation. |
| Ask test question | `KnowledgeTestQaSurface` | Validate selected corpus, question, and TopK before issuing the read-only ask query. |
| Review answer | `KnowledgeQuestionAnswer` | Display answer status, answer text, missing information, retrieved context, and citations. |
| Report query status | `KnowledgeOwnerStatus` when reused by hosting surfaces | Show ask failures or completion without mutating Knowledge state. |

## 4. Alternate and Failure Paths
- Missing tenant id -> block ask and show `TenantId is required.`
- Invalid corpus id -> block ask and show `CorpusId must be a GUID.`
- Empty question -> block ask and show `Question is required.`
- TopK below 1 -> block ask and show `TopK must be at least 1.`
- No sufficient grounding -> return an unanswered/refusal result with missing information rather than an ungrounded answer.
- Retrieval or answer dependency failure -> show the explicit failure message.

## 5. UX and Guardrail Rules
1. Ask is documented here as a query journey; it must not alter corpus, source, document, chunk, or index content.
2. Any trace persistence is diagnostic telemetry and must not become answer source state.
3. The answer must remain grounded in retrieved chunks and citations.
4. Missing information must be displayed when the answerer cannot safely answer.
5. TopK must remain bounded by UI constraints and backend validation.

## 6. Acceptance Criteria
- User can ask a valid question against a tenant-scoped corpus.
- The result shows answer status, answer text, missing information, retrieved context, and citations.
- Invalid tenant, corpus, question, or TopK blocks the query with a visible message.
- The journey does not save corpus data, import source content, index chunks, or run evaluation.
