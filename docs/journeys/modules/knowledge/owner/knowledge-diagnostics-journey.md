# Knowledge Diagnostics Journey

## 1. Context
- Product / service: Vitarish Knowledge module owner/support diagnostics
- Journey type: Current-state read-only diagnostic journey with debug ask query support
- Entry point: Diagnostic components used with a tenant id and corpus id
- User goal: Inspect sources, chunks, ask debug output, and ask traces for one corpus.
- Business goal: Make Knowledge pipeline state explainable without mutating corpus, source, chunk, index, question, or answer state.

## 2. Current Implementation Snapshot
| Area | Current behavior |
| --- | --- |
| Components | `KnowledgeSourcesPanel`, `KnowledgeChunksPanel`, `KnowledgeAskDebugPanel`, and `KnowledgeAskTracesPanel`. |
| Diagnostic contract | `IKnowledgeDiagnosticsService` exposes source, chunk, trace, pipeline, debug ask, and index diagnostics. |
| Source query | `GetSourcesAsync` lists imported sources for a corpus. |
| Chunk query | `GetChunksAsync` filters chunks by source, document, heading text, and chunk text. |
| Debug ask query | `DebugAskAsync` returns retrieved hits, packed context ids, answer, failure stage, and failure reason. |
| Trace query | `GetAskTracesAsync` filters traces by answered status, trace kind, and question text. |
| Domain state inspected | `KnowledgeSource`, `KnowledgeDocument`, `KnowledgeChunk`, `KnowledgeAskTrace`, and `KnowledgeIndexRun`. |

## 3. Happy Path
1. Owner or support user opens a diagnostic surface with known tenant id and corpus id.
2. User reviews sources in `KnowledgeSourcesPanel` to confirm imported source metadata.
3. User filters chunks in `KnowledgeChunksPanel` by source, document, heading, or text.
4. User opens a chunk to inspect full stored text and metadata.
5. User runs a debug ask in `KnowledgeAskDebugPanel` to inspect retrieval hits, packed context, answer, citations, and failure stage.
6. User filters ask traces in `KnowledgeAskTracesPanel` by answered state, trace kind, or question text.
7. User opens a trace to inspect generated answer, citations, retrieved chunk ids, and missing information.

## 4. Alternate and Failure Paths
- Invalid corpus id -> component parsing fails with `CorpusId must be a GUID.` for panels using guarded parsing.
- Optional source or document filters that are malformed GUIDs -> chunk filtering fails explicitly instead of silently widening the result set.
- Empty debug question -> debug ask does not run.
- No sources, chunks, or traces -> panels render empty result lists without inventing diagnostic data.
- Retrieval/generation failures -> debug ask reports `KnowledgeFailureStage` and failure reason where available.

## 5. UX and Guardrail Rules
1. Source, chunk, trace, pipeline, and index diagnostics are queries; they must not mutate Knowledge domain state.
2. Debug ask exists to explain the QA pipeline; it must not replace the normal QA test journey for user-facing answer validation.
3. Filters must narrow diagnostics only; invalid filters must not silently broaden access.
4. Full chunk text is shown only after the user opens a specific chunk.
5. Diagnostic traces are evidence of past asks, not authoritative source content.

## 6. Acceptance Criteria
- Owner/support can list sources for a tenant-scoped corpus.
- Owner/support can filter and open chunks for detailed inspection.
- Owner/support can run debug ask and see failure stage, context ids, answer, citations, and retrieved hits.
- Owner/support can filter and open ask traces.
- Diagnostic journeys do not save corpus data, import source content, index chunks, or run evaluation.
