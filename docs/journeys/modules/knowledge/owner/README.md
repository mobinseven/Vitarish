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

## Implementation Tracking

See [implementation-matrix.md](./implementation-matrix.md) for current routes, UI identifiers, backend contracts, domain state, known gaps, and alignment notes.

## Boundary Rules

- Commands: save corpus, import source, index corpus, and run evaluation.
- Queries: list corpora, ask a question, inspect sources, inspect chunks, inspect traces, and inspect debug output.
- State source of truth: Knowledge domain records (`KnowledgeCorpus`, `KnowledgeSource`, `KnowledgeDocument`, `KnowledgeChunk`, `KnowledgeQuestion`, `KnowledgeAskTrace`, and `KnowledgeIndexRun`) remain the canonical state model.
