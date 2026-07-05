"use client";

import { Button } from "@heroui/react";
import { useLocalizedText } from "@/localization";
import { KnowledgeAnswerCard } from "./KnowledgeAnswerCard";
import {
  KnowledgeChatCallout,
  KnowledgeChatConversation,
  KnowledgeChatEmptyState,
  KnowledgeChatLoader,
  KnowledgeChatMessage,
} from "./chat";
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
    <KnowledgeChatConversation>
      {hasQuestion && (
        <KnowledgeChatMessage kind="question">
          {question}
        </KnowledgeChatMessage>
      )}

      {isPending && (
        <KnowledgeChatLoader message={t("knowledge.qa.checkingScope", { scope: scopeLabel })} />
      )}

      {statusMessage && (
        <KnowledgeChatMessage
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
          <KnowledgeChatCallout tone="danger">{statusMessage}</KnowledgeChatCallout>
        </KnowledgeChatMessage>
      )}

      {!hasQuestion && !isPending && !statusMessage && (
        <KnowledgeChatEmptyState
          title={t("knowledge.qa.emptyTitle")}
          description={t("knowledge.qa.emptyDescription")}
        />
      )}

      {hasQuestion && !isPending && !statusMessage && results.length === 0 && (
        <KnowledgeChatMessage
          kind="answer"
          title={scopeKind === "all" ? t("knowledge.qa.allCorpusAnswer") : t("knowledge.qa.selectedCorpusAnswer")}
          status={t("knowledge.qa.needsContextStatus")}
          tone="warning"
        >
          <KnowledgeChatCallout tone="warning">
            {t("knowledge.qa.noSourcesForQuestion")}
          </KnowledgeChatCallout>
        </KnowledgeChatMessage>
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
    </KnowledgeChatConversation>
  );
}
