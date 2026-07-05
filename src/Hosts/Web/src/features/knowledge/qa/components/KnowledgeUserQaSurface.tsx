"use client";

import { useCallback, useMemo, useState } from "react";
import { useLocalizedText, type Translate } from "@/localization";
import { KnowledgeAnswerList } from "./KnowledgeAnswerList";
import { KnowledgeCorpusRail } from "./KnowledgeCorpusRail";
import { KnowledgeQuestionComposer } from "./KnowledgeQuestionComposer";
import { useKnowledgeUserAsk } from "../hooks/useKnowledgeUserAsk";
import { useKnowledgeUserWorkspace } from "../hooks/useKnowledgeUserWorkspace";
import type {
  KnowledgeCorpusAnswerResult,
  KnowledgeQuestionScope,
  KnowledgeUserCorpus,
} from "../types";

const defaultTopK = 8;

export function KnowledgeUserQaSurface() {
  const { t } = useLocalizedText();
  const workspace = useKnowledgeUserWorkspace();
  const ask = useKnowledgeUserAsk();

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

  const currentScopeLabel = selectedCorpus?.name ?? t("knowledge.qa.allCorpora");
  const answerScopeLabel = getScopeLabel(lastScope, corpora, t);

  const submitQuestion = useCallback(
    async (nextQuestion: string, scopeOverride?: KnowledgeQuestionScope) => {
      const normalizedQuestion = nextQuestion.trim();
      if (!normalizedQuestion) {
        setStatusMessage(t("knowledge.qa.questionRequired"));
        return;
      }

      const scope = scopeOverride ?? toScope(selectedCorpus);
      const scopeCorpora = resolveScopeCorpora(scope, corpora);
      if (scopeCorpora.length === 0) {
        setStatusMessage(t("knowledge.qa.noSourcesForQuestion"));
        return;
      }

      setStatusMessage(null);
      setSubmittedQuestion(normalizedQuestion);
      setLastScope(scope);
      setQuestion(normalizedQuestion);
      setAnswerResults([]);

      const results = await ask.askAcrossCorpora({
        corpora: scopeCorpora,
        question: normalizedQuestion,
        topK: defaultTopK,
      });

      setAnswerResults(results);
    },
    [ask, corpora, selectedCorpus, t]
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
    <main className="p-6">
      <div className="flex flex-col gap-6 lg:flex-row">
        <KnowledgeCorpusRail
          corpora={corpora}
          selectedCorpusId={selectedCorpusId}
          isLoading={workspace.isLoading}
          onSelectAll={selectAll}
          onSelectCorpus={selectCorpus}
        />

        <section className="flex min-w-0 flex-1 flex-col gap-5">
          <KnowledgeQuestionComposer
            value={question}
            scopeLabel={currentScopeLabel}
            isPending={ask.isPending}
            hasAnswer={answerResults.length > 0}
            onChange={setQuestion}
            onSubmit={submitQuestion}
            onNewQuestion={startNewQuestion}
          />

          <KnowledgeAnswerList
            question={submittedQuestion}
            scopeKind={lastScope.kind}
            scopeLabel={answerScopeLabel}
            results={answerResults}
            isPending={ask.isPending}
            statusMessage={statusMessage ?? workspaceError(workspace.error, t) ?? ask.errorMessage}
            onAskAgain={askAgain}
            onNewQuestion={startNewQuestion}
            onContinueInCorpus={continueInCorpus}
          />
        </section>
      </div>
    </main>
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
  t: Translate
): string {
  if (scope.kind === "all") {
    return t("knowledge.qa.allCorpora");
  }

  return corpora.find((item) => item.corpusId === scope.corpusId)?.name ?? t("knowledge.qa.selectedCorpus");
}

function workspaceError(error: unknown, t: Translate): string | null {
  if (!error) {
    return null;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return t("knowledge.qa.loadCorporaFailed");
}
