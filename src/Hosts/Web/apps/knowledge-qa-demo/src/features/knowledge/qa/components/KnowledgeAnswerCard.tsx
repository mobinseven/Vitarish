"use client";

import { Button } from "@heroui/react";
import {
  ChatBody,
  ChatCallout,
  ChatMessage,
  ChatSource,
  ChatSourceList,
} from "@vitarish/web-ui";
import { formatNumber, useLocalizedText, type Translate } from "@/localization";
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
  const { t, locale } = useLocalizedText();
  const { corpus, answer, errorMessage } = result;
  const citations = answer?.citations ?? [];
  const retrievedContext = answer?.retrievedContext ?? [];
  const answerText = answer?.isAnswered ? answer.answer : answer?.missingInformation ?? answer?.answer;

  return (
    <ChatMessage
      avatarAlt={t("knowledge.qa.assistantAvatarAlt")}
      avatarFallback={t("knowledge.qa.assistantAvatarFallback")}
      kind="answer"
      title={corpus.name}
      subtitle={corpus.description ?? undefined}
      status={answerStatusLabel(answer?.isAnswered, !!errorMessage, t)}
      tone={answerStatusTone(answer?.isAnswered, !!errorMessage)}
      actions={
        <>
          <Button
            isIconOnly
            aria-label={t("knowledge.qa.askAgainAction")}
            type="button"
            variant="ghost"
            onPress={onAskAgain}
          >
            <i aria-hidden="true" className="fa-solid fa-rotate-right size-4" />
          </Button>
          {!isSingleCorpusMode && (
            <Button
              isIconOnly
              aria-label={t("knowledge.qa.askFromCorpusAction")}
              type="button"
              variant="ghost"
              onPress={() => onContinueInCorpus(corpus.corpusId)}
            >
              <i aria-hidden="true" className="fa-solid fa-filter size-4" />
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
              <span className="font-medium">{t("knowledge.qa.missingInformationPrefix")} </span>
              {answer.missingInformation}
            </ChatCallout>
          )}

          {citations.length > 0 && (
            <ChatSourceList title={t("knowledge.qa.citations")}>
              {citations.map((citation) => (
                <ChatSource
                  key={`${citation.chunkId}-${citation.sourceId}`}
                  title={citation.title ?? t("knowledge.qa.referencedChunk")}
                  body={citation.quote ? `"${citation.quote}"` : null}
                />
              ))}
            </ChatSourceList>
          )}

          {retrievedContext.length > 0 && (
            <ChatSourceList title={t("knowledge.qa.retrievedContext", { count: retrievedContext.length })}>
              {retrievedContext.map((hit) => (
                <ChatSource
                  key={hit.chunkId}
                  title={hit.title ?? t("knowledge.qa.contextSection")}
                  meta={t("knowledge.qa.score", {
                    score: formatNumber(hit.score, locale, {
                      maximumFractionDigits: 2,
                      minimumFractionDigits: 2,
                    }),
                  })}
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

function answerStatusLabel(isAnswered: boolean | undefined, hasError: boolean, t: Translate): string {
  if (hasError) {
    return t("knowledge.qa.unavailableStatus");
  }

  return isAnswered ? t("knowledge.qa.answeredStatus") : t("knowledge.qa.needsContextStatus");
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
