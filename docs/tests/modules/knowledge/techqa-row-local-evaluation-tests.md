# Suggested Tests: TechQA Row-Local Evaluation Import

## Importer tests

### ImportAsync_MapsTechQaQuestionToChunksFromSameJsonRow

Verify that two TechQA rows produce two questions and two row-local evidence documents, and that each question's `ExpectedChunkIds` contains only chunks created from its own row-local document.

### ImportAsync_UsesRowLocalDocumentWhenAnswerIsParaphrased

Verify that a TechQA row with a paraphrased answer still maps the question to chunks from the same row-local document.

### ImportAsync_ReadsContextArraysAsRowLocalGoldEvidence

Verify that `contexts`, `documents`, or `passages` arrays are joined or extracted into row-local evidence and mapped to the question's `ExpectedChunkIds`.

### ImportAsync_FallsBackToExpectedContextTextsWhenNoRowLocalDocumentExists

Verify that rows without row-local document/context evidence still use the existing `ExpectedContextTexts` fallback.

## Evaluator tests

### EvaluateAsync_TreatsAnyRowLocalGoldChunkAsExpectedContextHit

Verify that when a question has multiple expected chunk IDs from row-local evidence, retrieval of any one expected chunk counts as an expected context hit.

### EvaluateAsync_RetrievesAtLeastTop10WhenReportingTop10Metric

Verify that evaluation retrieves at least 10 hits internally when Top5/Top10 metrics are reported, even when the configured request `TopK` is lower than 10.
