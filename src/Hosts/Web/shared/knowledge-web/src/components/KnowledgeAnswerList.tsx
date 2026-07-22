"use client";

import { Button } from "@heroui/react";
import {
  ChatCallout,
  ChatConversation,
  ChatEmptyState,
  ChatLoadingMessage,
  ChatMessage,
  FaIcon,
} from "@vitarish/web-ui";
import { KnowledgeAnswerCard } from "./KnowledgeAnswerCard";
import type {
  KnowledgeCorpusAnswerResult,
  KnowledgeQaLabels,
  KnowledgeUserScopeKind,
} from "../types";

interface KnowledgeAnswerListProps {
  formatScore: (score: number) => string;
  isPending: boolean;
  labels: KnowledgeQaLabels;
  onAskAgain: () => void;
  onContinueInCorpus: (corpusId: string) => void;
  onNewQuestion: () => void;
  question: string;
  results: KnowledgeCorpusAnswerResult[];
  scopeKind: KnowledgeUserScopeKind;
  scopeLabel: string;
  statusMessage?: string | null;
}

export function KnowledgeAnswerList({
  formatScore,
  isPending,
  labels,
  onAskAgain,
  onContinueInCorpus,
  onNewQuestion,
  question,
  results,
  scopeKind,
  scopeLabel,
  statusMessage,
}: KnowledgeAnswerListProps) {
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
          avatarAlt={labels.assistantAvatarAlt}
          avatarFallback={labels.assistantAvatarFallback}
          loadingLabel={labels.thinking}
          message={labels.checkingScope(scopeLabel)}
          title={labels.searchingContext}
        />
      )}

      {statusMessage && (
        <ChatMessage
          avatarAlt={labels.assistantAvatarAlt}
          avatarFallback={labels.assistantAvatarFallback}
          kind="answer"
          title={labels.title}
          status={labels.unavailableStatus}
          tone="danger"
          actions={
            <Button
              isIconOnly
              aria-label={labels.newQuestionAction}
              type="button"
              variant="ghost"
              onPress={onNewQuestion}
            >
              <FaIcon icon="plus" aria-hidden="true" className="size-4" />
            </Button>
          }
        >
          <ChatCallout tone="danger">{statusMessage}</ChatCallout>
        </ChatMessage>
      )}

      {!hasQuestion && !isPending && !statusMessage && (
        <ChatEmptyState
          title={labels.emptyTitle}
          description={labels.emptyDescription}
        />
      )}

      {hasQuestion && !isPending && !statusMessage && results.length === 0 && (
        <ChatMessage
          avatarAlt={labels.assistantAvatarAlt}
          avatarFallback={labels.assistantAvatarFallback}
          kind="answer"
          title={scopeKind === "all" ? labels.allCorpusAnswer : labels.selectedCorpusAnswer}
          status={labels.needsContextStatus}
          tone="warning"
        >
          <ChatCallout tone="warning">
            {labels.noSourcesForQuestion}
          </ChatCallout>
        </ChatMessage>
      )}

      {showAnswers && results.map((result) => (
        <KnowledgeAnswerCard
          key={result.corpus.corpusId}
          result={result}
          formatScore={formatScore}
          isSingleCorpusMode={scopeKind === "corpus"}
          labels={labels}
          onAskAgain={onAskAgain}
          onContinueInCorpus={onContinueInCorpus}
        />
      ))}
    </ChatConversation>
  );
}
