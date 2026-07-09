import { useCallback, useState } from "react";
import { ApiError } from "@/api/client/apiFetch";
import { askKnowledgeCorpus } from "../api/knowledgeUserApi";
import type {
  KnowledgeAskAcrossCorporaRequest,
  KnowledgeCorpusAnswerResult,
} from "../types";

export function useKnowledgeUserAsk() {
  const [isPending, setIsPending] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const askAcrossCorpora = useCallback(
    async ({
      corpora,
      question,
      topK = 8,
    }: KnowledgeAskAcrossCorporaRequest): Promise<KnowledgeCorpusAnswerResult[]> => {
      const normalizedQuestion = question.trim();
      setIsPending(true);
      setErrorMessage(null);

      try {
        const results = await Promise.all(
          corpora.map(async (corpus) => {
            try {
              const answer = await askKnowledgeCorpus({
                corpusId: corpus.corpusId,
                question: normalizedQuestion,
                topK,
                saveTrace: false,
                traceKind: "normal-user-ui",
              });

              return {
                corpus,
                question: normalizedQuestion,
                answer,
              } satisfies KnowledgeCorpusAnswerResult;
            } catch (error) {
              return {
                corpus,
                question: normalizedQuestion,
                errorMessage: toErrorMessage(error),
              } satisfies KnowledgeCorpusAnswerResult;
            }
          })
        );

        return results.sort(compareAnswerResults);
      } catch (error) {
        const message = toErrorMessage(error);
        setErrorMessage(message);
        return [];
      } finally {
        setIsPending(false);
      }
    },
    []
  );

  return {
    askAcrossCorpora,
    isPending,
    errorMessage,
  };
}

function compareAnswerResults(
  left: KnowledgeCorpusAnswerResult,
  right: KnowledgeCorpusAnswerResult
): number {
  const leftScore = scoreResult(left);
  const rightScore = scoreResult(right);

  if (leftScore !== rightScore) {
    return rightScore - leftScore;
  }

  return left.corpus.name.localeCompare(right.corpus.name);
}

function scoreResult(result: KnowledgeCorpusAnswerResult): number {
  if (result.errorMessage) {
    return 0;
  }

  if (result.answer?.isAnswered) {
    return 2;
  }

  return 1;
}

function toErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return "Knowledge answer is unavailable.";
}
