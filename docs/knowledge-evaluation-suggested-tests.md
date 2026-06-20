# Knowledge Evaluation Suggested Tests

This document lists recommended coverage for the knowledge evaluation changes in this commit.

## Import and gold-context mapping

- Import a JSON row with `expected_chunk_ids` containing valid chunk GUIDs and verify `KnowledgeQuestion.ExpectedChunkIds` preserves every explicit identifier.
- Import a JSON row with `chunk_ids`, `chunk_id`, `evidence_chunk_ids`, and object-shaped evidence IDs to verify equivalent dataset columns are accepted.
- Import a row without explicit IDs and verify text-based matching maps the expected context to matching chunks.
- Import context text that differs from chunk text only by whitespace, line breaks, casing, or low-value markdown characters and verify the expected chunk is mapped.
- Import a row whose expected context cannot be matched and verify the evaluation item reports `MissingGoldContext`.

## Retrieval and multiple valid evidence chunks

- Evaluate a question with multiple valid expected chunks and verify retrieving any one of them marks `ExpectedContextWasRetrieved` correctly.
- Evaluate a question with multiple valid expected chunks and verify retrieving any one of them within ranks 1-5 marks `ExpectedContextFoundInTop5` correctly.
- Evaluate a question with multiple valid expected chunks and verify retrieving any one of them within ranks 1-10 marks `ExpectedContextFoundInTop10` correctly.
- Evaluate a question with mapped gold context that is absent from retrieved hits and verify `GoldContextNotRetrieved`.

## Answer status reporting

- Evaluate a question where the answerer returns an answer without retrieving gold context and verify `AnsweredWithoutGoldContext`.
- Evaluate a question where gold context is retrieved but the answerer abstains and verify `AbstainedWithGoldContext`.
- Evaluate a question where gold context is retrieved and an answer is produced without expected-answer scoring and verify `SupportedAnswerCandidate`.
- Evaluate a question with retrieved gold context and a mismatched answer and verify `UnsupportedAnswer`.
- Evaluate a question with retrieved gold context and a matched answer and verify `CorrectAnswer`.

## Answer correctness scoring

- Verify matched expected/generated answers populate `GeneratedAnswerMatchedExpectedAnswer = true`.
- Verify mismatched expected/generated answers populate `GeneratedAnswerMatchedExpectedAnswer = false`.
- Verify abstained answers leave `GeneratedAnswerMatchedExpectedAnswer = null`.
- Verify scorer-unavailable cases leave nullable correctness and include a rationale that no scorer is configured.

## Output row boundaries

- Run evaluation with N selected questions and verify `KnowledgeEvaluationRun.Items.Count == N`.
- Render or export one evaluation run and verify exactly N rows are emitted.
- Render or export repeated runs and verify rows are grouped or keyed by `KnowledgeEvaluationRun.Id` rather than concatenated without a run boundary.
