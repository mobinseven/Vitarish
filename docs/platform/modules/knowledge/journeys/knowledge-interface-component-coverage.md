# Knowledge Interface Component Coverage Matrix

## 1. Context

- App: Shared platform
- Module: Knowledge
- Capability: Knowledge owner management, source import, indexing, evaluation, diagnostics, owner QA testing, and normal-user QA
- Product / service: Vitarish Knowledge module interface components
- Journey type: Current-state component coverage audit
- Scenario: Ensure every `src/Modules/Knowledge/Interface/Components` artifact is covered by a canonical Knowledge journey or is explicitly classified as a support component that inherits journey coverage.
- Primary persona: Knowledge owner, support user, tester, and normal user depending on the mapped journey.
- Evidence sources: Static review of `src/Modules/Knowledge/Interface/Components/*.razor`, existing journey files in this folder, and the normal-user React implementation matrix.
- Confidence level: Medium; coverage is based on repository documentation and component inspection rather than user research.

## 2. Coverage Classification Rules

| Classification | Meaning | Coverage requirement |
| --- | --- | --- |
| Standalone journey-bearing component | Component owns a visible user step or command/query boundary. | Map directly to one canonical journey and describe the user-visible role in that journey. |
| Support component | Selector, collection item, status display, format label, payload model, or shared rendering primitive that does not own a complete journey by itself. | Map to the parent journey it supports and name the inherited coverage. |
| Cross-journey support component | Component is reused by more than one journey without owning state. | Map to each journey that depends on it and state the shared role. |

## 3. Owner Blazor Knowledge Interface Components

| Component | Classification | Canonical journey coverage | User-visible role | Coverage status |
| --- | --- | --- | --- | --- |
| `KnowledgeOwnerManagementSurface` | Standalone journey-bearing component | [Knowledge Corpus Management](./knowledge-corpus-management-journey.md), [Knowledge Source Import](./knowledge-source-import-journey.md), [Knowledge Indexing](./knowledge-indexing-journey.md) | Coordinates the owner workspace where corpus creation, file import, source deletion, and indexing commands are initiated, then reloads the collection and shows operation status. | Covered by owner management stages across corpus, import/deletion, and indexing journeys. |
| `KnowledgeOwnerCorpusCollection` | Cross-journey support component | Corpus management, source import, indexing | Lists corpus cards and hosts the create/import/index/delete entry points inherited from the owner management surface. | Support component with inherited coverage. |
| `KnowledgeOwnerCorpusCollectionItem` | Cross-journey support component | Corpus management, source import, indexing | Expands one corpus, exposes its source list, and hosts per-corpus file import. | Support component with inherited coverage. |
| `KnowledgeOwnerCorpusCollectionItem` source list area | Support component | Source import | Shows when a corpus has no imported sources and provides the visible source collection context after imports. | Support component with inherited coverage. |
| `KnowledgeOwnerSourceCollectionItem` | Support component | Source import, indexing | Displays one imported source, exposes source-specific index action, and hosts source deletion. | Support component with inherited coverage. |
| `KnowledgeOwnerSourceFileCollectionItem` | Support component | Source import | Displays an imported file item when source-file metadata is present in the owner collection model. | Support component with inherited coverage. |
| `KnowledgeOwnerStatus` | Support component | Corpus management, source import, indexing, evaluation, QA test | Shows the latest operation/query status without owning corpus, source, index, evaluation, or answer state. | Support component with inherited coverage. |
| `KnowledgeCorpusCreator` | Standalone journey-bearing component | [Knowledge Corpus Management](./knowledge-corpus-management-journey.md) | Opens the add-corpus dialog, collects name/description/active state, and raises the create command. | Covered by corpus creation stage. |
| `KnowledgeCorpusSelector` | Support component | Corpus management, source import, indexing, evaluation, QA test | Lets the user select the tenant-scoped corpus that downstream commands or queries inherit. | Support component with inherited coverage. |
| `KnowledgeCorpusIndexer` | Standalone journey-bearing component | [Knowledge Indexing](./knowledge-indexing-journey.md) | Runs indexing for the currently selected corpus and reports index command output. | Covered by indexing stage. |
| `KnowledgeCorpusEvaluator` | Standalone journey-bearing component | [Knowledge Evaluation](./knowledge-evaluation-journey.md) | Runs retrieval evaluation for the currently selected corpus and exposes summary metrics. | Covered by evaluation stage. |
| `KnowledgeSourceImporter` | Standalone journey-bearing component | [Knowledge Source Import](./knowledge-source-import-journey.md) | Imports a file-backed source into the selected corpus by delegating payload capture to the file input component. | Covered by source import stage. |
| `KnowledgeSourcePayloadFileInput` | Cross-journey support component | Source import, source replacement | Opens the file payload dialog, captures source name and format, uploads/selects the file payload, and emits a source payload for import or replacement. | Support component with inherited coverage. |
| `KnowledgeSourceReplacer` | Standalone journey-bearing component | [Knowledge Source Import](./knowledge-source-import-journey.md) | Selects the existing source to replace and submits a new file payload as an explicit replacement command. | Covered by source replacement stage. |
| `KnowledgeSourceSelector` | Support component | Source import | Retains and changes the focused source for replacement without owning source state. | Support component with inherited coverage. |
| `KnowledgeSourceDeleter` | Standalone journey-bearing component | [Knowledge Source Import](./knowledge-source-import-journey.md) | Confirms and requests deletion of one imported source from a corpus. | Covered by source deletion stage. |
| `KnowledgeSourcesPanel` | Standalone journey-bearing component | [Knowledge Diagnostics](./knowledge-diagnostics-journey.md) | Lists imported source diagnostics for a corpus. | Covered by diagnostics source-inspection stage. |
| `KnowledgeChunksPanel` | Standalone journey-bearing component | [Knowledge Diagnostics](./knowledge-diagnostics-journey.md) | Filters chunk diagnostics, opens one chunk, and displays stored chunk text. | Covered by diagnostics chunk-inspection stage. |
| `KnowledgeAskDebugPanel` | Standalone journey-bearing component | [Knowledge Diagnostics](./knowledge-diagnostics-journey.md) | Runs a debug ask query to inspect retrieval hits, packed context, answer, citations, and failure stage. | Covered by diagnostics debug-ask stage. |
| `KnowledgeAskTracesPanel` | Standalone journey-bearing component | [Knowledge Diagnostics](./knowledge-diagnostics-journey.md) | Filters ask traces and opens a trace for answer, citation, retrieved chunk, and missing-information details. | Covered by diagnostics trace-inspection stage. |
| `KnowledgeTestQaSurface` | Standalone journey-bearing component | [Knowledge QA Test](./knowledge-qa-test-journey.md) | Provides owner/tester corpus selection, chat submit, ask validation, answer query, and management navigation. | Covered by QA test stages. |
| `KnowledgeQuestionAnswer` | Support component | [Knowledge QA Test](./knowledge-qa-test-journey.md), [Knowledge Diagnostics](./knowledge-diagnostics-journey.md) | Renders answer status, answer text, missing information, retrieved context, and citations for a completed ask. | Support component with inherited QA answer-review coverage. |
| `KnowledgeSourceInputFormatLabels` | Support component | [Knowledge Source Import](./knowledge-source-import-journey.md) | Provides the single UI label mapping for supported Knowledge source input formats. | Support component with inherited source import coverage. |
| `_Imports` | Support artifact | All Blazor Knowledge interface journeys | Supplies component-level imports only; it has no user-visible journey role. | Support artifact with inherited coverage. |

## 4. Normal-User React Knowledge Interface Components

Normal-user React components live under `src/Hosts/Web/src/features/knowledge/qa/components/**`. They remain separate from owner/admin Blazor components and inherit coverage from [Knowledge Normal User QA](./knowledge-normal-user-qa-journey.md) and [Knowledge Normal User Journey Implementation Matrix](./normal-user-journey-implementation-matrix.md).

| Component | Classification | Canonical journey coverage | User-visible role | Coverage status |
| --- | --- | --- | --- | --- |
| `KnowledgeUserQaSurface` | Standalone journey-bearing component | [Knowledge Normal User QA](./knowledge-normal-user-qa-journey.md) | Owns the normal-user read-only QA page flow, corpus selection state, ask submission, answer grouping, retry, and new-question behavior. | Covered by normal-user QA stages. |
| `KnowledgeCorpusRail` | Support component | Knowledge Normal User QA | Shows active/user-visible corpora and lets the user choose all-corpus or selected-corpus scope. | Support component with inherited normal-user corpus-discovery and selection coverage. |
| `KnowledgeQuestionComposer` | Standalone journey-bearing component | Knowledge Normal User QA | Captures the user question, communicates current ask scope, and submits read-only ask queries. | Covered by normal-user ask stages. |
| `KnowledgeAnswerList` | Support component | Knowledge Normal User QA | Groups answer cards for all-corpus or selected-corpus result review. | Support component with inherited answer-review coverage. |
| `KnowledgeAnswerCard` | Support component | Knowledge Normal User QA | Displays one grounded answer with status, missing information, citations, context, ask-again, and continue-in-corpus controls. | Support component with inherited answer-review and follow-up coverage. |
| `KnowledgeChatConversation` | Support component | Knowledge Normal User QA; [HeroUI Pro Migration Seam](./normal-user-heroui-pro-migration.md) | Provides the chat conversation layout adapter. | Support component with inherited normal-user QA coverage. |
| `KnowledgeChatMessage` | Support component | Knowledge Normal User QA; HeroUI Pro Migration Seam | Renders an individual chat-style message/card. | Support component with inherited normal-user QA coverage. |
| `KnowledgePromptInput` | Support component | Knowledge Normal User QA; HeroUI Pro Migration Seam | Provides the chat prompt adapter for question entry. | Support component with inherited ask-composer coverage. |
| `KnowledgeChatSourceList` | Support component | Knowledge Normal User QA; HeroUI Pro Migration Seam | Groups source/citation entries for an answer. | Support component with inherited citation-review coverage. |
| `KnowledgeChatSource` | Support component | Knowledge Normal User QA; HeroUI Pro Migration Seam | Renders one citation or retrieved source row. | Support component with inherited citation-review coverage. |
| `KnowledgeChatLoader` | Support component | Knowledge Normal User QA; HeroUI Pro Migration Seam | Shows read-only ask loading state. | Support component with inherited ask-query coverage. |
| `KnowledgeChatEmptyState` | Support component | Knowledge Normal User QA; HeroUI Pro Migration Seam | Shows the no-question/no-answer starting state. | Support component with inherited start-page coverage. |
| `KnowledgeChatBody` | Support component | Knowledge Normal User QA; HeroUI Pro Migration Seam | Wraps answer body content without changing answer state. | Support component with inherited answer-review coverage. |
| `KnowledgeChatCallout` | Support component | Knowledge Normal User QA; HeroUI Pro Migration Seam | Displays error or missing-information callouts. | Support component with inherited failure-path coverage. |

## 5. Coverage Gaps Closed by This Matrix

- Corpus collection items, selectors, source file items, payload inputs, status displays, and format labels are explicitly classified as support components rather than independent journeys.
- Source replacement and deletion are explicitly tied to the source import journey because they are owner source lifecycle commands over canonical `KnowledgeSource` state.
- Source-specific indexing entry points are explicitly tied to the indexing journey while source content ownership remains in the source import journey.
- Evaluation, QA test, diagnostics, and normal-user chat rendering components are mapped to their read/query or command boundaries so answer display components do not become hidden source-of-truth artifacts.
