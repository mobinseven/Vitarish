"use client";

import { Button } from "@heroui/react";
import {
  KnowledgeChatBody,
  KnowledgeChatCallout,
  KnowledgeChatMessage,
  KnowledgeChatSource,
  KnowledgeChatSourceList,
} from "./chat";
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
    <KnowledgeChatMessage
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
      {errorMessage && <KnowledgeChatCallout tone="danger">{errorMessage}</KnowledgeChatCallout>}

      {!errorMessage && answer && (
        <>
          {answerText && <KnowledgeChatBody>{answerText}</KnowledgeChatBody>}

          {answer.missingInformation && answer.isAnswered && (
            <KnowledgeChatCallout tone="warning">
              <span className="font-medium">{t("knowledge.qa.missingInformationPrefix")} </span>
              {answer.missingInformation}
            </KnowledgeChatCallout>
          )}

          {citations.length > 0 && (
            <KnowledgeChatSourceList title={t("knowledge.qa.citations")}>
              {citations.map((citation) => (
                <KnowledgeChatSource
                  key={`${citation.chunkId}-${citation.sourceId}`}
                  title={citation.title ?? t("knowledge.qa.referencedChunk")}
                  body={citation.quote ? `"${citation.quote}"` : null}
                />
              ))}
            </KnowledgeChatSourceList>
          )}

          {retrievedContext.length > 0 && (
            <KnowledgeChatSourceList title={t("knowledge.qa.retrievedContext", { count: retrievedContext.length })}>
              {retrievedContext.map((hit) => (
                <KnowledgeChatSource
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
            </KnowledgeChatSourceList>
          )}
        </>
      )}
    </KnowledgeChatMessage>
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
