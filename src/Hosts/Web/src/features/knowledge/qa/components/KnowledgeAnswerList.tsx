"use client";

import { Button, Card } from "@heroui/react";
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
  if (isPending) {
    return <KnowledgeChatLoader message={`Checking ${scopeLabel} for the best answer and citations.`} />;
  }

  if (statusMessage) {
    return <KnowledgeChatCallout tone="danger">{statusMessage}</KnowledgeChatCallout>;
  }

  if (!question || results.length === 0) {
    return (
      <KnowledgeChatEmptyState
        title="Choose a corpus or ask globally."
        description="Ask without selecting a corpus to compare answers across every available corpus, or select one corpus for a focused answer."
      />
    );
  }

  return (
    <KnowledgeChatConversation>
      <Card className="glass-card">
        <Card.Content className="flex flex-col gap-3 p-5 md:flex-row md:items-center md:justify-between">
          <div>
            <p className="text-xs uppercase tracking-[0.25em] text-muted">
              {scopeKind === "all" ? "All-corpus answer" : "Selected-corpus answer"}
            </p>
            <h2 className="text-lg font-semibold">{question}</h2>
            <p className="text-sm text-muted">Scope: {scopeLabel}</p>
          </div>
          <div className="flex flex-wrap gap-2">
            <Button type="button" onPress={onAskAgain}>
              Ask again
            </Button>
            <Button type="button" onPress={onNewQuestion} className="bg-transparent">
              New question
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
