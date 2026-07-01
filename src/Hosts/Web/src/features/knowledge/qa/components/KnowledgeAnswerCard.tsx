"use client";

import { Button } from "@heroui/react";
import {
  KnowledgeChatBody,
  KnowledgeChatCallout,
  KnowledgeChatMessage,
  KnowledgeChatSource,
  KnowledgeChatSourceList,
} from "./chat";
import { useLocalizedText, type Translate } from "@/localization";
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
  const { t } = useLocalizedText();
  const { corpus, answer, errorMessage } = result;
  const citations = answer?.citations ?? [];
  const retrievedContext = answer?.retrievedContext ?? [];

  return (
    <KnowledgeChatMessage
      kind="answer"
      title={corpus.name}
      subtitle={corpus.description ?? undefined}
      status={answerStatusLabel(answer?.isAnswered, !!errorMessage, t)}
      tone={answerStatusTone(answer?.isAnswered, !!errorMessage)}
      actions={
        <>
          <Button type="button" onPress={onAskAgain}>
            {t("knowledge.qa.askAgainAction")}
          </Button>
          {!isSingleCorpusMode && (
            <Button
              type="button"
              variant="ghost"
              onPress={() => onContinueInCorpus(corpus.corpusId)}
            >
              {t("knowledge.qa.askFromCorpusAction")}
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
                  body={citation.quote ? `“${citation.quote}”` : null}
                />
              ))}
            </KnowledgeChatSourceList>
          )}

          {retrievedContext.length > 0 && (
            <details className="border border-default-200 rounded-lg p-3">
              <summary className="cursor-pointer text-sm font-semibold">
                {t("knowledge.qa.retrievedContext", { count: retrievedContext.length })}
              </summary>
              <div className="mt-3 grid gap-3">
                {retrievedContext.map((hit) => (
                  <KnowledgeChatSource
                    key={hit.chunkId}
                    title={hit.title ?? t("knowledge.qa.contextSection")}
                    meta={t("knowledge.qa.score", { score: hit.score.toFixed(2) })}
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
