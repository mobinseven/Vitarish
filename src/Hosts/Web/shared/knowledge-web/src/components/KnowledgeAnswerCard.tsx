"use client";

import { Button } from "@heroui/react";
import {
  ChatBody,
  ChatCallout,
  ChatMessage,
  ChatSource,
  ChatSourceList,
  FaIcon,
} from "@vitarish/web-ui";
import type { KnowledgeCorpusAnswerResult, KnowledgeQaLabels } from "../types";

interface KnowledgeAnswerCardProps {
  formatScore: (score: number) => string;
  isSingleCorpusMode: boolean;
  labels: KnowledgeQaLabels;
  onAskAgain: () => void;
  onContinueInCorpus: (corpusId: string) => void;
  result: KnowledgeCorpusAnswerResult;
}

export function KnowledgeAnswerCard({
  formatScore,
  isSingleCorpusMode,
  labels,
  onAskAgain,
  onContinueInCorpus,
  result,
}: KnowledgeAnswerCardProps) {
  const { corpus, answer, errorMessage } = result;
  const citations = answer?.citations ?? [];
  const retrievedContext = answer?.retrievedContext ?? [];
  const answerText = answer?.isAnswered
    ? answer.answer
    : (answer?.missingInformation ?? answer?.answer);

  return (
    <ChatMessage
      avatarAlt={labels.assistantAvatarAlt}
      avatarFallback={labels.assistantAvatarFallback}
      kind="answer"
      title={corpus.name}
      subtitle={corpus.description ?? undefined}
      status={answerStatusLabel(answer?.isAnswered, !!errorMessage, labels)}
      tone={answerStatusTone(answer?.isAnswered, !!errorMessage)}
      actions={
        <>
          <Button
            isIconOnly
            aria-label={labels.askAgainAction}
            type="button"
            variant="ghost"
            onPress={onAskAgain}
          >
            <FaIcon icon="rotate-right" aria-hidden="true" className="size-4" />
          </Button>
          {!isSingleCorpusMode && (
            <Button
              isIconOnly
              aria-label={labels.askFromCorpusAction}
              type="button"
              variant="ghost"
              onPress={() => onContinueInCorpus(corpus.corpusId)}
            >
              <FaIcon icon="filter" aria-hidden="true" className="size-4" />
            </Button>
          )}
        </>
      }
    >
      {errorMessage && <ChatCallout tone="danger">{errorMessage}</ChatCallout>}

      {!errorMessage && answer && (
        <>
          {answerText && <ChatBody>{answerText}</ChatBody>}

          {answer.missingInformation && answer.isAnswered && (
            <ChatCallout tone="warning">
              <span className="font-medium">
                {labels.missingInformationPrefix}{" "}
              </span>
              {answer.missingInformation}
            </ChatCallout>
          )}

          {citations.length > 0 && (
            <ChatSourceList title={labels.citations}>
              {citations.map((citation) => (
                <ChatSource
                  key={`${citation.chunkId}-${citation.sourceId}`}
                  title={citation.title ?? labels.referencedChunk}
                  body={citation.quote ? `"${citation.quote}"` : null}
                />
              ))}
            </ChatSourceList>
          )}

          {retrievedContext.length > 0 && (
            <ChatSourceList
              title={labels.retrievedContext(retrievedContext.length)}
            >
              {retrievedContext.map((hit) => (
                <ChatSource
                  key={hit.chunkId}
                  title={hit.title ?? labels.contextSection}
                  meta={labels.score(formatScore(hit.score))}
                  body={hit.text}
                />
              ))}
            </ChatSourceList>
          )}
        </>
      )}
    </ChatMessage>
  );
}

function answerStatusLabel(
  isAnswered: boolean | undefined,
  hasError: boolean,
  labels: KnowledgeQaLabels,
): string {
  if (hasError) {
    return labels.unavailableStatus;
  }

  return isAnswered ? labels.answeredStatus : labels.needsContextStatus;
}

function answerStatusTone(
  isAnswered: boolean | undefined,
  hasError: boolean,
): "success" | "warning" | "danger" {
  if (hasError) {
    return "danger";
  }

  return isAnswered ? "success" : "warning";
}
