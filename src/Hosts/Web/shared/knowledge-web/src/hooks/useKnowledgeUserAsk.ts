import { useCallback, useState } from "react";
import type {
  KnowledgeAskAcrossCorporaRequest,
  KnowledgeCorpusAnswerResult,
  KnowledgeQaClient,
  KnowledgeQaErrorMessageResolver,
} from "../types";

interface UseKnowledgeUserAskOptions {
  client: KnowledgeQaClient;
  defaultErrorMessage: string;
  resolveErrorMessage?: KnowledgeQaErrorMessageResolver;
  saveTrace?: boolean;
  traceKind?: string | null;
}

export function useKnowledgeUserAsk({
  client,
  defaultErrorMessage,
  resolveErrorMessage,
  saveTrace = false,
  traceKind = "normal-user-ui",
}: UseKnowledgeUserAskOptions) {
  const [isPending, setIsPending] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const toErrorMessage = useCallback(
    (error: unknown): string => {
      if (resolveErrorMessage) {
        return resolveErrorMessage(error);
      }

      if (error instanceof Error) {
        return error.message;
      }

      return defaultErrorMessage;
    },
    [defaultErrorMessage, resolveErrorMessage]
  );

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
              const answer = await client.askCorpus({
                corpusId: corpus.corpusId,
                question: normalizedQuestion,
                topK,
                saveTrace,
                traceKind,
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
    [client, saveTrace, toErrorMessage, traceKind]
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
