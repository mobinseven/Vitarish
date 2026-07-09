"use client";

import { Button } from "@heroui/react";
import {
  ChatCallout,
  ChatConversation,
  ChatEmptyState,
  ChatLoadingMessage,
  ChatMessage,
} from "@vitarish/web-ui";
import { useLocalizedText } from "@/localization";
import { KnowledgeAnswerCard } from "./KnowledgeAnswerCard";
import type {
  KnowledgeCorpusAnswerResult,
  KnowledgeUserScopeKind,
} from "../types";

interface KnowledgeAnswerListProps {
  question: string;
  scopeKind: KnowledgeUserScopeKind;
  scopeLabel: string;
  results: KnowledgeCorpusAnswerResult[];
  isPending: boolean;
  statusMessage?: string | null;
  onAskAgain: () => void;
  onNewQuestion: () => void;
  onContinueInCorpus: (corpusId: string) => void;
}

export function KnowledgeAnswerList({
  question,
  scopeKind,
  scopeLabel,
  results,
  isPending,
  statusMessage,
  onAskAgain,
  onNewQuestion,
  onContinueInCorpus,
}: KnowledgeAnswerListProps) {
  const { t } = useLocalizedText();
  const hasQuestion = !!question?.trim();
  const showAnswers = hasQuestion && !isPending && !statusMessage && results.length > 0;

  return (
    <ChatConversation>
      {hasQuestion && (
        <ChatMessage kind="question">
          {question}
        </ChatMessage>
      )}

      {isPending && (
        <ChatLoadingMessage
          avatarAlt={t("knowledge.qa.assistantAvatarAlt")}
          avatarFallback={t("knowledge.qa.assistantAvatarFallback")}
          loadingLabel={t("knowledge.qa.thinking")}
          message={t("knowledge.qa.checkingScope", { scope: scopeLabel })}
          title={t("knowledge.qa.searchingContext")}
        />
      )}

      {statusMessage && (
        <ChatMessage
          avatarAlt={t("knowledge.qa.assistantAvatarAlt")}
          avatarFallback={t("knowledge.qa.assistantAvatarFallback")}
          kind="answer"
          title={t("knowledge.qa.title")}
          status={t("knowledge.qa.unavailableStatus")}
          tone="danger"
          actions={
            <Button
              isIconOnly
              aria-label={t("knowledge.qa.newQuestionAction")}
              type="button"
              variant="ghost"
              onPress={onNewQuestion}
            >
              <i aria-hidden="true" className="fa-solid fa-plus size-4" />
            </Button>
          }
        >
          <ChatCallout tone="danger">{statusMessage}</ChatCallout>
        </ChatMessage>
      )}

      {!hasQuestion && !isPending && !statusMessage && (
        <ChatEmptyState
          title={t("knowledge.qa.emptyTitle")}
          description={t("knowledge.qa.emptyDescription")}
        />
      )}

      {hasQuestion && !isPending && !statusMessage && results.length === 0 && (
        <ChatMessage
          avatarAlt={t("knowledge.qa.assistantAvatarAlt")}
          avatarFallback={t("knowledge.qa.assistantAvatarFallback")}
          kind="answer"
          title={scopeKind === "all" ? t("knowledge.qa.allCorpusAnswer") : t("knowledge.qa.selectedCorpusAnswer")}
          status={t("knowledge.qa.needsContextStatus")}
          tone="warning"
        >
          <ChatCallout tone="warning">
            {t("knowledge.qa.noSourcesForQuestion")}
          </ChatCallout>
        </ChatMessage>
      )}

      {showAnswers && results.map((result) => (
        <KnowledgeAnswerCard
          key={result.corpus.corpusId}
          result={result}
          isSingleCorpusMode={scopeKind === "corpus"}
          onAskAgain={onAskAgain}
          onContinueInCorpus={onContinueInCorpus}
        />
      ))}
    </ChatConversation>
  );
}
