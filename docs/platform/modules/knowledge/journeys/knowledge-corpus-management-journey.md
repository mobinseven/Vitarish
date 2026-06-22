# Knowledge Corpus Management Journey

## 1. Context
- Product / service: Vitarish Knowledge module owner corpus administration
- Journey type: Current-state owner command journey
- Entry point: `/owner/knowledge/corpora` via `AppRoutes.Knowledge.OwnerCorpora`
- User goal: Create or update one tenant-scoped corpus that can receive source imports, indexing, evaluation, and QA testing.
- Business goal: Keep corpus identity, tenant scope, activation status, and descriptive metadata in one canonical Knowledge state model.

## 2. Current Implementation Snapshot
| Area | Current behavior |
| --- | --- |
| Route | `AppRoutes.Knowledge.OwnerCorpora` maps to `/owner/knowledge/corpora`. |
| Page / surface | `KnowledgeOwnerCorporaPage` renders `KnowledgeOwnerSurface`. |
| Command contract | `IKnowledgeService.SaveCorpusAsync` persists `KnowledgeCorpus`. |
| Query contract | `IKnowledgeService.ListCorporaAsync` refreshes selectable corpora for follow-on import, index, and evaluation actions. |
| Domain state | `KnowledgeCorpus` owns corpus id, tenant id, name, description, and active flag. |
| Guardrails | Tenant id, corpus id GUID, and name are validated before save; status shows success or the explicit exception message. |

## 3. Happy Path
1. Owner opens the Knowledge corpora page.
2. System shows the tenant id, editable corpus id, name, description, and active flag.
3. Owner enters a valid corpus GUID and non-empty name.
4. Owner clicks **Save corpus**.
5. `KnowledgeOwnerSurface` validates tenant id, corpus GUID, and name.
6. The surface sends a `KnowledgeCorpus` to `IKnowledgeService.SaveCorpusAsync`.
7. The surface refreshes corpus options with `IKnowledgeService.ListCorporaAsync`.
8. The saved corpus becomes selected for import, indexing, and evaluation controls.
9. System shows `Corpus saved.`

## 4. Alternate and Failure Paths
- Missing tenant id -> block save and show `TenantId is required.`
- Invalid corpus id -> block save and show `CorpusId must be a GUID.`
- Missing name -> block save and show `Name is required.`
- Save dependency failure -> show the explicit failure message and do not claim persistence completed.
- Existing corpus id -> update the same tenant-scoped corpus rather than creating a parallel corpus identity.

## 5. UX and Guardrail Rules
1. Corpus save is a command; it must not be used as the source for diagnostic reads.
2. Corpus listing is a query; it may refresh page selections but must not mutate corpus state.
3. The corpus id and tenant id together are the corpus boundary for downstream source, chunk, index, question, and trace state.
4. Do not create a second Knowledge corpus state model in UI documentation or implementation.
5. Keep source import, indexing, evaluation, QA testing, and diagnostics as separate journeys even when surfaced on the same page.

## 6. Acceptance Criteria
- Owner can save one tenant-scoped corpus with valid id and name.
- Invalid tenant id, corpus id, or name prevents the command and displays an actionable message.
- Saved corpus appears in the page's corpus selectors after refresh.
- The journey does not combine save behavior with diagnostic source/chunk/trace queries.
