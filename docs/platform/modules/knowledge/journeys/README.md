# Knowledge Owner Journey Set

This directory contains the canonical owner journey documentation for the current Vitarish Knowledge module. The journeys are split by responsibility so command flows stay separate from read-only QA and diagnostic flows.

## Canonical Journeys

| Journey | Responsibility | Command / query boundary |
| --- | --- | --- |
| [Knowledge Corpus Management](./knowledge-corpus-management-journey.md) | Owner creates or updates one tenant-scoped corpus. | Command: save corpus. Query: refresh corpus list. |
| [Knowledge Source Import](./knowledge-source-import-journey.md) | Owner imports markdown or plain-text source content into one corpus. | Command: import or replace source content. |
| [Knowledge Indexing](./knowledge-indexing-journey.md) | Owner indexes corpus chunks for retrieval. | Command: index corpus or source-derived chunks. |
| [Knowledge QA Test](./knowledge-qa-test-journey.md) | Owner/tester asks a grounded question against one corpus. | Query: ask and inspect answer output. |
| [Knowledge Evaluation](./knowledge-evaluation-journey.md) | Owner runs retrieval evaluation and reviews recall metrics. | Command: run evaluation against canonical questions. |
| [Knowledge Diagnostics](./knowledge-diagnostics-journey.md) | Owner/support inspects sources, chunks, debug ask output, and ask traces. | Queries: source, chunk, debug, trace, pipeline, and index diagnostics. |
| [Knowledge Interface Component Coverage](./knowledge-interface-component-coverage.md) | Maps each Knowledge Interface component to its canonical journey or support-component inheritance. | Documentation-only coverage matrix; does not introduce commands or queries. |

## Implementation Tracking

See the [React interface coverage audit](../../../../apps/clinic-owner/tests/react-interface-coverage-audit.md) for current React route/component evidence and known gaps. Knowledge normal-user QA remains separate from Knowledge owner/admin journeys. See the [Knowledge Interface Component Coverage Matrix](./knowledge-interface-component-coverage.md) for the canonical mapping from Blazor owner/admin components and React normal-user components to these journeys.

## Boundary Rules

- Commands: save corpus, import source, index corpus, and run evaluation.
- Queries: list corpora, ask a question, inspect sources, inspect chunks, inspect traces, and inspect debug output.
- State source of truth: Knowledge domain records (`KnowledgeCorpus`, `KnowledgeSource`, `KnowledgeDocument`, `KnowledgeChunk`, `KnowledgeQuestion`, `KnowledgeAskTrace`, and `KnowledgeIndexRun`) remain the canonical state model.
