# Knowledge Content Management Test Plan

Date: 2026-06-28
Status: Draft
App: Main
Module: Knowledge
Capability: Source deletion, owner management collection queries, TechQA row-local evaluation import
Owner: Knowledge module
Related journey: —
Related roadmap: —
Related ADRs: —
Related tasks: —
Source code/files: `src/Modules/Knowledge/Application`, `src/Modules/Knowledge/Infrastructure`, `src/Modules/Knowledge/Contracts`
Confidence: Medium

## 1. Objective

Protect knowledge source deletion against incomplete cleanup, owner management collection queries against invalid input and missing data, and TechQA row-local evaluation import against incorrect chunk mapping and retrieval. Knowledge supports content management workflows where data integrity and correct retrieval are essential.

## 2. Scope

### In Scope

- `DeleteKnowledgeSourceCommand`: owner validation, corpus/source existence, cascade cleanup (documents, chunks, questions), search index removal.
- `KnowledgeOwnerCollectionQuery`: hierarchy rendering (corpora→sources→files with chunk counts), invalid input handling, empty collection safety, projection field preservation.
- TechQA row-local evaluation: import mapping (row→chunks→questions with expected chunk IDs), paraphrased answers, context arrays, fallback behavior, evaluation hit detection, Top10 retrieval.

### Out of Scope

- Full ingestion pipeline tests (chunking, embedding, indexing).
- UI component tests for knowledge owner pages.
- Infrastructure integration tests (database, search index, background workers).
- Hot scenarios requiring running search engine or LLM service.

## 3. Code / Risk Analysis

| Code area | Risk trigger | Why it matters | Test response |
|---|---|---|---|
| `DeleteKnowledgeSourceCommand` handler | Owner validation, corpus/source existence, cascade deletion | Missing cleanup leaves orphaned data | Unit test all validation and deletion paths |
| Knowledge source repository | Cascade removal of documents, chunks, questions | Incomplete cascade leaves stale references | Unit test cascade behavior |
| Search index deletion | Stale indexed chunks remain retrievable | Deleted sources still appear in search results | Unit test indexer delete call |
| `KnowledgeOwnerCollectionQuery` handler | Null/empty input, missing owner, empty collections | EF/runtime exceptions or null references | Unit test input validation and empty safety |
| `KnowledgeOwnerCollectionProjector` | Field preservation in read model | Parsed display labels instead of explicit fields | Unit test projection correctness |
| TechQA importer | Row→chunk→question mapping, context arrays, fallback | Wrong chunk IDs break evaluation accuracy | Unit test mapping and fallback logic |
| TechQA evaluator | Gold chunk hit detection, Top10 retrieval | Missed hits or insufficient retrieval depth | Unit test hit counting and TopK behavior |

## 4. Required Unit Tests

| Test | Behavior protected | Target | Input / edge case | Expected result | Requirement reason |
|---|---|---|---|---|---|
| Delete — owner deletes existing source | Valid owner deletes source and returns success | `DeleteKnowledgeSourceCommand` handler | Valid tenant, owner, corpus ID, source ID | Successful command result, source removed | Happy path |
| Delete — rejects unauthorized owner | Missing owner identity is rejected before mutation | `DeleteKnowledgeSourceCommand` handler | Null/empty owner ID | Authorization failure, no mutation | Security boundary |
| Delete — rejects missing corpus | Missing corpus returns not-found failure | `DeleteKnowledgeSourceCommand` handler | Non-existent corpus ID | Corpus-not-found failure, no mutation | Data integrity |
| Delete — rejects missing source | Missing source returns not-found failure | `DeleteKnowledgeSourceCommand` handler | Existing corpus, non-existent source ID | Source-not-found failure, no mutation | Data integrity |
| Delete — cascade removes dependent records | Deleting source removes documents, chunks, and questions | Knowledge source repository | Source with documents, chunks, questions | All dependent records removed in same unit of work | Cleanup completeness |
| Delete — removes search index entries | Source deletion calls indexer delete | `KnowledgeService` or repository | Source with indexed chunks | Indexer delete called with tenant, corpus, source filters | Search consistency |
| Collection query — hierarchy renders correctly | Corpora with nested sources and files with chunk counts | `KnowledgeOwnerCollectionQuery` handler | Tenant with 2 corpora, multiple sources, documents, chunks | Read model contains correct hierarchy and counts | Data accuracy |
| Collection query — invalid tenant input | Null/empty/whitespace tenant returns empty model | `KnowledgeOwnerCollectionQuery` handler | `null`, `""`, `"  "` for tenant ID | `KnowledgeOwnerCollectionReadModel.Empty`, no exception | Input safety |
| Collection query — missing owner | Missing owner returns empty model | `KnowledgeOwnerCollectionQuery` handler | Null/empty owner ID | `KnowledgeOwnerCollectionReadModel.Empty`, no exception | Input safety |
| Collection query — empty collection safety | Empty sources/files render as empty collections, not null | `KnowledgeOwnerCollectionQuery` handler | Corpus with no sources, source with no documents | Empty collections (not null) in read model | Null safety |
| Collection query — projection preserves fields | Explicit read-model fields populated correctly | `KnowledgeOwnerCollectionProjector` | Corpus with description, active state, source type, import timestamp, document title, language, text length, chunks | Each field preserved in explicit read-model properties | Projection correctness |
| TechQA importer — maps rows to chunks | Two TechQA rows produce correct question→chunk mapping | TechQA importer | Two TechQA rows with row-local documents | Each question's `ExpectedChunkIds` contains only chunks from its own row | Import accuracy |
| TechQA importer — paraphrased answer | Paraphrased answer still maps to row-local chunks | TechQA importer | Row with paraphrased answer | Question maps to chunks from same row-local document | Paraphrase handling |
| TechQA importer — context arrays | Context/document/passage arrays become row-local evidence | TechQA importer | Row with `contexts` array | Arrays joined/extracted into row-local evidence, mapped to `ExpectedChunkIds` | Context extraction |
| TechQA importer — fallback when no row-local | Rows without row-local evidence use ExpectedContextTexts | TechQA importer | Row without document/context | Fallback to `ExpectedContextTexts` | Graceful degradation |
| TechQA evaluator — any gold chunk counts as hit | Retrieval of any expected chunk counts as context hit | TechQA evaluator | Question with multiple expected chunk IDs, retrieval returns one | Expected context hit counted | Hit detection correctness |
| TechQA evaluator — retrieves at least Top10 | Evaluation retrieves enough hits for Top10 metric | TechQA evaluator | `TopK` configured < 10 | Internally retrieves at least 10 hits for Top5/Top10 reporting | Metric accuracy |

## 5. Suggested Unit Tests

| Test | Behavior protected | Target | Notes |
|---|---|---|---|
| Delete — concurrent deletion safety | Two simultaneous deletes of same source | `DeleteKnowledgeSourceCommand` handler | One succeeds, one gets not-found or conflict |
| Collection query — large hierarchy | Performance with many corpora/sources | `KnowledgeOwnerCollectionQuery` handler | Ensure no N+1 query issues |
| TechQA importer — malformed JSON rows | Graceful handling of corrupt input | TechQA importer | Row with missing required fields |
| TechQA evaluator — zero expected chunks | Question with no expected chunks handled | TechQA evaluator | Edge case for evaluation scoring |

## 6. Required Hot Test Scenarios

| Scenario | Risk protected | Runtime need | Trigger | Expected evidence | Execution profile |
|---|---|---|---|---|---|
| Source deletion with real database | Cascade deletion works with actual EF Core constraints and transactions | Host with seeded knowledge corpus in database | Delete source with documents and chunks via API | Source, documents, chunks, and questions removed; search index updated | Semi-automated, nightly |
| Owner collection query with real data | Hierarchy renders correctly with actual database joins | Host with seeded multi-level knowledge data | Query collection for tenant/owner | Read model matches seeded data structure | Semi-automated, nightly |
| TechQA import and evaluation e2e | Import→chunk→evaluate pipeline produces correct metrics | Host with TechQA dataset and search index | Import TechQA rows, run evaluation | Gold chunk hits counted correctly, Top10 metric accurate | Semi-automated, nightly |

## 7. Suggested Hot Test Scenarios

| Scenario | Risk protected | Runtime need | Reason suggested |
|---|---|---|---|
| Full ingestion pipeline with evaluation | End-to-end content→chunk→embed→search→evaluate | Running host with LLM and search engine | Highest-value integration, but complex environment |
| Concurrent source operations | Multiple users deleting/importing simultaneously | Multi-user host | Race condition detection |

## 8. Deferred or Rejected Tests

| Test/scenario | Decision | Reason |
|---|---|---|
| UI component tests for owner pages | Deferred | Frontend migration not yet owning these artifacts |
| LLM-based answer quality tests | Deferred | Non-deterministic, requires human evaluation |
| Search engine integration tests | Deferred | Requires running search infrastructure |
| Background ingestion worker tests | Deferred | Requires message broker and worker runtime |

## 9. Test Task Derivation

| Task | Source | Type | Requirement | Target suite | Notes |
|---|---|---|---|---|---|
| Delete command handler tests | §4 delete rows | Unit | Required | `Knowledge.Tests.Application` | Owner validation, cascade, index removal |
| Collection query handler tests | §4 collection rows | Unit | Required | `Knowledge.Tests.Application` | Input validation, hierarchy, projection |
| TechQA importer tests | §4 importer rows | Unit | Required | `Knowledge.Tests.Infrastructure` | Row mapping, context arrays, fallback |
| TechQA evaluator tests | §4 evaluator rows | Unit | Required | `Knowledge.Tests.Infrastructure` | Hit detection, Top10 retrieval |

## 10. Existing Coverage / Gaps

No test code exists in the working tree. All tests are created fresh based on this plan.

**Coverage targets by layer:**

| Layer | Classes under test | Planned test classes | Coverage approach |
|---|---|---|---|
| Application | DeleteKnowledgeSourceCommand handler, KnowledgeOwnerCollectionQuery handler | 1 (KnowledgeApplicationTests) | Mock repositories and services |
| Infrastructure | TechQA importer, evaluator, repository cascade | 1 (KnowledgeInfrastructureTests) | Mock LLM/search, verify repository calls |

## 11. CI / Execution Guidance

| Suite | Execution mode | Command/procedure | Required environment |
|---|---|---|---|
| Application unit tests | Automated, every PR | `dotnet test tests/Modules/Knowledge.Tests/ --filter "Category=Application"` | None (mocked dependencies) |
| Infrastructure unit tests | Automated, every PR | `dotnet test tests/Modules/Knowledge.Tests/ --filter "Category=Infrastructure"` | None (mocked services) |
| Full test suite | Automated, every PR | `dotnet test tests/Modules/Knowledge.Tests/` | None |
| Hot scenarios | Semi-automated, nightly | Manual execution against host with database and search | Database, search index, seeded data |

## 12. Traceability

| Test area | Linked ADR | Linked journey stage | Linked code path |
|---|---|---|---|
| Source deletion | — | — | `src/Modules/Knowledge/Application/Commands/DeleteKnowledgeSourceCommand.cs` |
| Collection query | — | — | `src/Modules/Knowledge/Application/Queries/KnowledgeOwnerCollectionQuery.cs` |
| Collection projector | — | — | `src/Modules/Knowledge/Application/Projections/KnowledgeOwnerCollectionProjector.cs` |
| TechQA importer | — | — | `src/Modules/Knowledge/Infrastructure/Importers/TechQaImporter.cs` |
| TechQA evaluator | — | — | `src/Modules/Knowledge/Infrastructure/Evaluators/TechQaEvaluator.cs` |
