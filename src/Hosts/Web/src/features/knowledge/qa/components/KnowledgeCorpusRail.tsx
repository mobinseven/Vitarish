"use client";

import { Button, Card } from "@heroui/react";
import { SurfaceCard } from "@/design-system";
import { useLocalizedText } from "@/localization";
import type { KnowledgeUserCorpus } from "../types";

interface KnowledgeCorpusRailProps {
  corpora: KnowledgeUserCorpus[];
  selectedCorpusId: string | null;
  isLoading?: boolean;
  onSelectAll: () => void;
  onSelectCorpus: (corpusId: string) => void;
}

export function KnowledgeCorpusRail({
  corpora,
  selectedCorpusId,
  isLoading = false,
  onSelectAll,
  onSelectCorpus,
}: KnowledgeCorpusRailProps) {
  const { t } = useLocalizedText();

  return (
    <aside className="w-full lg:w-72 shrink-0">
      <SurfaceCard className="sticky top-6">
        <Card.Header>
          <div>
            <p className="text-xs uppercase tracking-[0.25em] text-muted">{t("knowledge.qa.railEyebrow")}</p>
            <h2 className="text-lg font-semibold">{t("knowledge.qa.railTitle")}</h2>
          </div>
        </Card.Header>
        <Card.Content className="flex flex-col gap-4">
          <Button
            type="button"
            onPress={onSelectAll}
            isDisabled={isLoading}
            className={railButtonClass(selectedCorpusId === null)}
          >
            <span className="flex w-full flex-col items-start gap-1 text-start">
              <span className="font-medium">{t("knowledge.qa.allCorpora")}</span>
              <span className="text-xs text-muted">{t("knowledge.qa.allCorporaDescription")}</span>
            </span>
          </Button>

          {isLoading && <p className="text-sm text-muted">{t("knowledge.qa.loadingCorpora")}</p>}

          {!isLoading && corpora.length === 0 && (
            <p className="ds-muted-panel text-sm">
              {t("knowledge.qa.noCorpora")}
            </p>
          )}

          {corpora.map((corpus) => (
            <Button
              key={corpus.corpusId}
              type="button"
              onPress={() => onSelectCorpus(corpus.corpusId)}
              className={railButtonClass(selectedCorpusId === corpus.corpusId)}
            >
              <span className="flex w-full flex-col items-start gap-1 text-start">
                <span className="font-medium">{corpus.name}</span>
                {corpus.description && (
                  <span className="line-clamp-2 text-xs text-muted">
                    {corpus.description}
                  </span>
                )}
                <span className="text-xs text-muted">
                  {formatCorpusCounts(corpus.sourceCount, corpus.chunkCount, t)}
                </span>
              </span>
            </Button>
          ))}
        </Card.Content>
      </SurfaceCard>
    </aside>
  );
}

function railButtonClass(isSelected: boolean): string {
  return [
    "h-auto w-full justify-start rounded-2xl px-4 py-3 text-start transition-colors",
    isSelected
      ? "bg-[color-mix(in_oklch,var(--accent)_16%,transparent)] text-[var(--accent)]"
      : "bg-transparent hover:bg-[var(--accent-soft)]",
  ].join(" ");
}


function formatCorpusCounts(sourceCount: number, chunkCount: number, t: ReturnType<typeof useLocalizedText>["t"]): string {
  const sourceKey = sourceCount === 1 ? "knowledge.qa.sourceCount.one" : "knowledge.qa.sourceCount.other";
  const sourceLabel = t(sourceKey, { count: sourceCount });

  if (chunkCount <= 0) {
    return sourceLabel;
  }

  const chunkKey = chunkCount === 1 ? "knowledge.qa.chunkCount.one" : "knowledge.qa.chunkCount.other";
  return `${sourceLabel} · ${t(chunkKey, { count: chunkCount })}`;
}
