"use client";

import { Button } from "@heroui/react";
import {
  KnowledgeChatBody,
  KnowledgeChatCallout,
  KnowledgeChatMessage,
  KnowledgeChatSource,
  KnowledgeChatSourceList,
} from "./chat";
import type { KnowledgeCorpusAnswerResult } from "../types";

interface KnowledgeAnswerCardProps {
  result: KnowledgeCorpusAnswerResult;
  isSingleCorpusMode: boolean;
  onAskAgain: () => void;
  onContinueInCorpus: (corpusId: string) => void;
}

export function KnowledgeAnswerCard({
  result,
  isSingleCorpusMode,
  onAskAgain,
  onContinueInCorpus,
}: KnowledgeAnswerCardProps) {
  const { corpus, answer, errorMessage } = result;
  const citations = answer?.citations ?? [];
  const retrievedContext = answer?.retrievedContext ?? [];

  return (
    <KnowledgeChatMessage
      kind="answer"
      title={corpus.name}
      subtitle={corpus.description ?? undefined}
      status={answerStatusLabel(answer?.isAnswered, !!errorMessage)}
      tone={answerStatusTone(answer?.isAnswered, !!errorMessage)}
      actions={
        <>
          <Button type="button" onPress={onAskAgain}>
            Ask again
          </Button>
          {!isSingleCorpusMode && (
            <Button
              type="button"
              variant="ghost"
              onPress={() => onContinueInCorpus(corpus.corpusId)}
            >
              Ask from this corpus
            </Button>
          )}
        </>
      }
    >
      {errorMessage && <KnowledgeChatCallout tone="danger">{errorMessage}</KnowledgeChatCallout>}

      {!errorMessage && answer && (
        <>
          <KnowledgeChatBody>
            <p className="whitespace-pre-wrap">
              {answer.isAnswered ? answer.answer : answer.missingInformation ?? answer.answer}
            </p>
          </KnowledgeChatBody>

          {answer.missingInformation && answer.isAnswered && (
            <KnowledgeChatCallout tone="warning">
              <span className="font-medium">Missing information: </span>
              {answer.missingInformation}
            </KnowledgeChatCallout>
          )}

          {citations.length > 0 && (
            <KnowledgeChatSourceList title="Citations">
              {citations.map((citation) => (
                <KnowledgeChatSource
                  key={`${citation.chunkId}-${citation.sourceId}`}
                  title={citation.title ?? "Referenced chunk"}
                  body={citation.quote ? `“${citation.quote}”` : null}
                />
              ))}
            </KnowledgeChatSourceList>
          )}

          {retrievedContext.length > 0 && (
            <details className="ds-inline-panel">
              <summary className="cursor-pointer text-sm font-semibold">
                Retrieved context ({retrievedContext.length})
              </summary>
              <div className="mt-3 grid gap-3">
                {retrievedContext.map((hit) => (
                  <KnowledgeChatSource
                    key={hit.chunkId}
                    title={hit.title ?? "Context section"}
                    meta={`Score ${hit.score.toFixed(2)}`}
                    body={hit.text}
                  />
                ))}
              </div>
            </details>
          )}
        </>
      )}
    </KnowledgeChatMessage>
  );
}

function answerStatusLabel(isAnswered: boolean | undefined, hasError: boolean): string {
  if (hasError) {
    return "Unavailable";
  }

  return isAnswered ? "Answered" : "Needs more context";
}

function answerStatusTone(
  isAnswered: boolean | undefined,
  hasError: boolean
): "success" | "warning" | "danger" {
  if (hasError) {
    return "danger";
  }

  return isAnswered ? "success" : "warning";
}
