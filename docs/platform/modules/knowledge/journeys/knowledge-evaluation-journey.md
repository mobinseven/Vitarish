# Knowledge Evaluation Journey

## 1. Context
- Product / service: Vitarish Knowledge module retrieval evaluation
- Journey type: Current-state owner command journey
- Entry point: `/owner/knowledge/corpora` via the Evaluation section of `KnowledgeOwnerSurface`
- User goal: Run evaluation for one corpus and review recall-oriented quality metrics.
- Business goal: Provide a repeatable owner-facing check of retrieval quality against canonical Knowledge questions and expected chunks.

## 2. Current Implementation Snapshot
| Area | Current behavior |
| --- | --- |
| Route | `AppRoutes.Knowledge.OwnerCorpora` maps to `/owner/knowledge/corpora`. |
| Page / surface | `KnowledgeOwnerCorporaPage` renders `KnowledgeOwnerSurface`. |
| Command contract | `IKnowledgeService.EvaluateAsync` delegates evaluation behavior exposed by `IKnowledgeEvaluator`. |
| Evaluation source | `KnowledgeQuestion` stores questions, optional expected answers, and expected chunk ids. |
| Displayed metrics | `recall@5`, `recall@10`, and average expected chunk rank. |
| Related cleanup | `RemoveKnowledgeQuestionsForChunksCommand` removes questions tied to chunks that should no longer drive evaluation. |

## 3. Happy Path
1. Owner prepares corpus sources, chunks, index state, and evaluation questions.
2. Owner selects the corpus in the Evaluation selector.
3. Owner clicks **Run evaluation**.
4. `KnowledgeOwnerSurface` validates tenant id and selected corpus id.
5. The surface sends a `KnowledgeEvaluationRequest` to `IKnowledgeService.EvaluateAsync`.
6. Evaluation uses `IKnowledgeEvaluator` to compare retrieval results against expected chunk ids from `KnowledgeQuestion`.
7. System shows `Evaluation complete.` and a summary with recall@5, recall@10, and average expected rank.

## 4. Alternate and Failure Paths
- No selected corpus -> block evaluation and show `CorpusId must be a GUID.`
- Missing tenant id -> block evaluation and show `TenantId is required.`
- No evaluation questions -> return an explicit no-question/zero metric outcome rather than fake quality.
- Missing or stale chunks -> evaluation should fail or score honestly based on current canonical chunk state.
- Questions referencing removed chunks -> cleanup must use `RemoveKnowledgeQuestionsForChunksCommand` so evaluation does not rely on orphaned expectations.

## 5. UX and Guardrail Rules
1. Evaluation is a command journey because it runs a quality operation and may produce run output; it must remain separate from QA ask queries.
2. `KnowledgeQuestion` is the source of truth for expected retrieval checks.
3. Evaluation metrics must be displayed as retrieval quality indicators, not as a guarantee of generated answer correctness.
4. Do not create evaluation questions implicitly from ad-hoc QA asks.
5. Keep source/chunk/trace diagnosis in the diagnostics journey.

## 6. Acceptance Criteria
- Owner can run evaluation for a selected tenant-scoped corpus.
- Evaluation summary displays recall@5, recall@10, and average expected rank.
- Invalid tenant or corpus input prevents evaluation with a visible message.
- Missing evaluation data is reported honestly.
- Evaluation does not import, index, or ask ad-hoc questions implicitly.
