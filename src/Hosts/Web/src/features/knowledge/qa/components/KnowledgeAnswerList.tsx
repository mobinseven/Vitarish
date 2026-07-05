"use client";

import { Button, Card } from "@heroui/react";
import { useLocalizedText } from "@/localization";
import { KnowledgeAnswerCard } from "./KnowledgeAnswerCard";
import {
  KnowledgeChatCallout,
  KnowledgeChatConversation,
  KnowledgeChatEmptyState,
  KnowledgeChatLoader,
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

  if (isPending) {
    return <KnowledgeChatLoader message={t("knowledge.qa.checkingScope", { scope: scopeLabel })} />;
  }

  if (statusMessage) {
    return <KnowledgeChatCallout tone="danger">{statusMessage}</KnowledgeChatCallout>;
  }

  if (!question || results.length === 0) {
    return (
      <KnowledgeChatEmptyState
        title={t("knowledge.qa.emptyTitle")}
        description={t("knowledge.qa.emptyDescription")}
      />
    );
  }

  return (
    <KnowledgeChatConversation>
      <Card>
        <Card.Content className="flex flex-col gap-4 p-5 md:flex-row md:items-center md:justify-between">
          <div>
            <p className="text-xs uppercase tracking-[0.25em] text-muted">
              {scopeKind === "all" ? t("knowledge.qa.allCorpusAnswer") : t("knowledge.qa.selectedCorpusAnswer")}
            </p>
            <h2 className="text-lg font-semibold">{question}</h2>
            <p className="text-sm text-muted">{t("knowledge.qa.scopeLabel", { scope: scopeLabel })}</p>
          </div>
          <div className="flex flex-wrap gap-2">
            <Button type="button" onPress={onAskAgain}>
              {t("knowledge.qa.askAgainAction")}
            </Button>
            <Button type="button" variant="ghost" onPress={onNewQuestion}>
              {t("knowledge.qa.newQuestionAction")}
            </Button>
          </div>
        </Card.Content>
      </Card>

      {results.map((result) => (
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
