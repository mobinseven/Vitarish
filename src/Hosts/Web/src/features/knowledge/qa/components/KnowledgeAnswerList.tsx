"use client";

import { Button, Card } from "@heroui/react";
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
  if (isPending) {
    return (
      <Card className="glass-card">
        <Card.Content className="flex flex-col gap-3 p-6">
          <p className="text-xs uppercase tracking-[0.25em] text-muted">Thinking</p>
          <h2 className="text-xl font-semibold">Searching grounded context...</h2>
          <p className="text-sm text-muted">
            Checking {scopeLabel} for the best answer and citations.
          </p>
        </Card.Content>
      </Card>
    );
  }

  if (statusMessage) {
    return (
      <Card className="glass-card">
        <Card.Content className="p-6 text-sm text-danger-700">
          {statusMessage}
        </Card.Content>
      </Card>
    );
  }

  if (!question || results.length === 0) {
    return (
      <Card className="glass-card">
        <Card.Content className="flex flex-col gap-3 p-6">
          <p className="text-xs uppercase tracking-[0.25em] text-muted">Start</p>
          <h2 className="text-xl font-semibold">Choose a corpus or ask globally.</h2>
          <p className="text-sm text-muted">
            Ask without selecting a corpus to compare answers across every available corpus, or select one corpus for a focused answer.
          </p>
        </Card.Content>
      </Card>
    );
  }

  return (
    <section className="flex flex-col gap-4">
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
    </section>
  );
}
