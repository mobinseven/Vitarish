"use client";

import { useCallback, useMemo } from "react";
import {
  KnowledgeUserQaSurface,
  type KnowledgeQaLabels,
} from "@vitarish/knowledge-web";
import {
  getKnowledgeQaErrorMessage,
  createKnowledgeQaClient,
} from "@vitarish/knowledge-web";
import { apiFetch } from "@vitarish/web-api-client";

const knowledgeQaClient = createKnowledgeQaClient(
  apiFetch as <T>(url: string, options?: RequestInit) => Promise<T>,
);
import { formatNumber, useLocalizedText } from "@/localization";

export function KnowledgeQaPageSurface() {
  const { direction, locale, t } = useLocalizedText();
  const labels = useMemo(() => createKnowledgeQaLabels(t), [t]);
  const formatScore = useCallback(
    (score: number) =>
      formatNumber(score, locale, {
        maximumFractionDigits: 2,
        minimumFractionDigits: 2,
      }),
    [locale],
  );
  const resolveErrorMessage = useCallback(
    (error: unknown) =>
      getKnowledgeQaErrorMessage(error, labels.unavailableStatus),
    [labels.unavailableStatus],
  );

  return (
    <KnowledgeUserQaSurface
      client={knowledgeQaClient}
      direction={direction}
      formatScore={formatScore}
      labels={labels}
      resolveErrorMessage={resolveErrorMessage}
    />
  );
}

function createKnowledgeQaLabels(
  t: ReturnType<typeof useLocalizedText>["t"],
): KnowledgeQaLabels {
  return {
    allCorpora: t("knowledge.qa.allCorpora"),
    allCorporaDescription: t("knowledge.qa.allCorporaDescription"),
    allCorpusAnswer: t("knowledge.qa.allCorpusAnswer"),
    answeredStatus: t("knowledge.qa.answeredStatus"),
    assistantAvatarAlt: t("knowledge.qa.assistantAvatarAlt"),
    assistantAvatarFallback: t("knowledge.qa.assistantAvatarFallback"),
    askAction: t("knowledge.qa.askAction"),
    askAgainAction: t("knowledge.qa.askAgainAction"),
    askFromCorpusAction: t("knowledge.qa.askFromCorpusAction"),
    checkingScope: (scope) => t("knowledge.qa.checkingScope", { scope }),
    chunkCount: (count) =>
      t(
        count === 1
          ? "knowledge.qa.chunkCount.one"
          : "knowledge.qa.chunkCount.other",
        { count },
      ),
    citations: t("knowledge.qa.citations"),
    composerFooter: t("knowledge.qa.composerFooter"),
    contextSection: t("knowledge.qa.contextSection"),
    emptyDescription: t("knowledge.qa.emptyDescription"),
    emptyTitle: t("knowledge.qa.emptyTitle"),
    loadCorporaFailed: t("knowledge.qa.loadCorporaFailed"),
    loadingCorpora: t("knowledge.qa.loadingCorpora"),
    missingInformationPrefix: t("knowledge.qa.missingInformationPrefix"),
    needsContextStatus: t("knowledge.qa.needsContextStatus"),
    newQuestionAction: t("knowledge.qa.newQuestionAction"),
    noCorpora: t("knowledge.qa.noCorpora"),
    noSourcesForQuestion: t("knowledge.qa.noSourcesForQuestion"),
    questionLabel: t("knowledge.qa.questionLabel"),
    questionPlaceholder: t("knowledge.qa.questionPlaceholder"),
    questionRequired: t("knowledge.qa.questionRequired"),
    railEyebrow: t("knowledge.qa.railEyebrow"),
    railTitle: t("knowledge.qa.railTitle"),
    referencedChunk: t("knowledge.qa.referencedChunk"),
    retrievedContext: (count) => t("knowledge.qa.retrievedContext", { count }),
    scopeLabel: (scope) => t("knowledge.qa.scopeLabel", { scope }),
    score: (score) => t("knowledge.qa.score", { score }),
    searchingContext: t("knowledge.qa.searchingContext"),
    selectedCorpus: t("knowledge.qa.selectedCorpus"),
    selectedCorpusAnswer: t("knowledge.qa.selectedCorpusAnswer"),
    sourceCount: (count) =>
      t(
        count === 1
          ? "knowledge.qa.sourceCount.one"
          : "knowledge.qa.sourceCount.other",
        { count },
      ),
    thinking: t("knowledge.qa.thinking"),
    title: t("knowledge.qa.title"),
    unavailableStatus: t("knowledge.qa.unavailableStatus"),
  };
}
