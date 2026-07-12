"use client";

import { useCallback, useMemo, useState } from "react";
import { KnowledgeAnswerList } from "./KnowledgeAnswerList";
import { KnowledgeQuestionComposer } from "./KnowledgeQuestionComposer";
import { useKnowledgeUserAsk } from "../hooks/useKnowledgeUserAsk";
import { useKnowledgeUserWorkspace } from "../hooks/useKnowledgeUserWorkspace";
import type {
  KnowledgeCorpusAnswerResult,
  KnowledgeQaClient,
  KnowledgeQaErrorMessageResolver,
  KnowledgeQaLabels,
  KnowledgeQuestionScope,
  KnowledgeUserCorpus,
} from "../types";
import type { ChatTextDirection } from "@vitarish/web-ui";

const defaultTopK = 8;

export interface KnowledgeUserQaSurfaceProps {
  client: KnowledgeQaClient;
  defaultTopK?: number;
  direction?: ChatTextDirection;
  formatScore?: (score: number) => string;
  labels: KnowledgeQaLabels;
  resolveErrorMessage?: KnowledgeQaErrorMessageResolver;
  saveTrace?: boolean;
  traceKind?: string | null;
}

export function KnowledgeUserQaSurface({
  client,
  defaultTopK: topK = defaultTopK,
  direction,
  formatScore = defaultScoreFormatter,
  labels,
  resolveErrorMessage,
  saveTrace = false,
  traceKind = "normal-user-ui",
}: KnowledgeUserQaSurfaceProps) {
  const workspace = useKnowledgeUserWorkspace({ client });
  const ask = useKnowledgeUserAsk({
    client,
    defaultErrorMessage: labels.unavailableStatus,
    resolveErrorMessage,
    saveTrace,
    traceKind,
  });
  const { askAcrossCorpora } = ask;

  const corpora = useMemo(() => workspace.data?.corpora ?? [], [workspace.data?.corpora]);
  const [selectedCorpusId, setSelectedCorpusId] = useState<string | null>(null);
  const [question, setQuestion] = useState("");
  const [submittedQuestion, setSubmittedQuestion] = useState("");
  const [answerResults, setAnswerResults] = useState<KnowledgeCorpusAnswerResult[]>([]);
  const [lastScope, setLastScope] = useState<KnowledgeQuestionScope>({ kind: "all" });
  const [statusMessage, setStatusMessage] = useState<string | null>(null);

  const selectedCorpus = useMemo(
    () => corpora.find((corpus) => corpus.corpusId === selectedCorpusId) ?? null,
    [corpora, selectedCorpusId]
  );

  const answerScopeLabel = getScopeLabel(lastScope, corpora, labels);

  const submitQuestion = useCallback(
    async (nextQuestion: string, scopeOverride?: KnowledgeQuestionScope) => {
      const normalizedQuestion = nextQuestion.trim();
      if (!normalizedQuestion) {
        setStatusMessage(labels.questionRequired);
        return;
      }

      const scope = scopeOverride ?? toScope(selectedCorpus);
      const scopeCorpora = resolveScopeCorpora(scope, corpora);
      if (scopeCorpora.length === 0) {
        setStatusMessage(labels.noSourcesForQuestion);
        return;
      }

      setStatusMessage(null);
      setSubmittedQuestion(normalizedQuestion);
      setLastScope(scope);
      setQuestion(normalizedQuestion);
      setAnswerResults([]);

      const results = await askAcrossCorpora({
        corpora: scopeCorpora,
        question: normalizedQuestion,
        topK,
      });

      setAnswerResults(results);
    },
    [askAcrossCorpora, corpora, labels.noSourcesForQuestion, labels.questionRequired, selectedCorpus, topK]
  );

  const askAgain = useCallback(async () => {
    if (!submittedQuestion) {
      return;
    }

    await submitQuestion(submittedQuestion, lastScope);
  }, [lastScope, submittedQuestion, submitQuestion]);

  const startNewQuestion = useCallback(() => {
    setQuestion("");
    setSubmittedQuestion("");
    setAnswerResults([]);
    setStatusMessage(null);
  }, []);

  const selectAll = useCallback(() => {
    setSelectedCorpusId(null);
    setStatusMessage(null);
  }, []);

  const selectCorpus = useCallback((corpusId: string) => {
    setSelectedCorpusId(corpusId);
    setStatusMessage(null);
  }, []);

  const continueInCorpus = useCallback((corpusId: string) => {
    setSelectedCorpusId(corpusId);
    setLastScope({ kind: "corpus", corpusId });
    setStatusMessage(null);
  }, []);

  return (
    <div className="flex h-svh min-h-0 flex-col overflow-hidden bg-background">
      <header className="flex h-16 shrink-0 items-center gap-3 border-b border-separator px-4 sm:px-6">
        <div className="flex size-9 shrink-0 items-center justify-center rounded-xl bg-accent-soft text-accent">
          <i aria-hidden="true" className="fa-solid fa-book-open size-4" />
        </div>
        <div className="min-w-0">
          <h1 className="truncate text-sm font-semibold text-foreground sm:text-base">{labels.title}</h1>
        </div>
      </header>

      <main className="flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden">
        <KnowledgeAnswerList
          question={submittedQuestion}
          scopeKind={lastScope.kind}
          scopeLabel={answerScopeLabel}
          results={answerResults}
          isPending={ask.isPending}
          statusMessage={statusMessage ?? workspaceError(workspace.error, labels, resolveErrorMessage) ?? ask.errorMessage}
          formatScore={formatScore}
          labels={labels}
          onAskAgain={askAgain}
          onNewQuestion={startNewQuestion}
          onContinueInCorpus={continueInCorpus}
        />

        <div className="shrink-0 border-t border-separator/60 bg-background px-4 pb-4 pt-3">
          <div className="mx-auto w-full max-w-[714px]">
            <KnowledgeQuestionComposer
              corpora={corpora}
              value={question}
              isPending={ask.isPending}
              isWorkspaceLoading={workspace.isLoading}
              hasAnswer={answerResults.length > 0}
              direction={direction}
              labels={labels}
              onChange={setQuestion}
              onSelectAll={selectAll}
              onSelectCorpus={selectCorpus}
              onSubmit={submitQuestion}
              onNewQuestion={startNewQuestion}
              selectedCorpusId={selectedCorpusId}
            />
          </div>
        </div>
      </main>
    </div>
  );
}

function toScope(corpus: KnowledgeUserCorpus | null): KnowledgeQuestionScope {
  if (corpus) {
    return { kind: "corpus", corpusId: corpus.corpusId };
  }

  return { kind: "all" };
}

function resolveScopeCorpora(
  scope: KnowledgeQuestionScope,
  corpora: KnowledgeUserCorpus[]
): KnowledgeUserCorpus[] {
  if (scope.kind === "all") {
    return corpora;
  }

  const corpus = corpora.find((item) => item.corpusId === scope.corpusId);
  return corpus ? [corpus] : [];
}

function getScopeLabel(
  scope: KnowledgeQuestionScope,
  corpora: KnowledgeUserCorpus[],
  labels: KnowledgeQaLabels
): string {
  if (scope.kind === "all") {
    return labels.allCorpora;
  }

  return corpora.find((item) => item.corpusId === scope.corpusId)?.name ?? labels.selectedCorpus;
}

function workspaceError(
  error: unknown,
  labels: KnowledgeQaLabels,
  resolveErrorMessage?: KnowledgeQaErrorMessageResolver
): string | null {
  if (!error) {
    return null;
  }

  if (resolveErrorMessage) {
    return resolveErrorMessage(error);
  }

  if (error instanceof Error) {
    return error.message;
  }

  return labels.loadCorporaFailed;
}

function defaultScoreFormatter(score: number): string {
  return score.toLocaleString(undefined, {
    maximumFractionDigits: 2,
    minimumFractionDigits: 2,
  });
}
