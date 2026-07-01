"use client";

import { Button, Card, Description, Label, ListBox } from "@heroui/react";
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
      <Card className="sticky top-6">
        <Card.Header>
          <div>
            <p className="text-xs uppercase tracking-[0.25em] text-default-500">{t("knowledge.qa.railEyebrow")}</p>
            <h2 className="text-lg font-semibold">{t("knowledge.qa.railTitle")}</h2>
          </div>
        </Card.Header>
        <Card.Content>
          <Button
            type="button"
            onPress={onSelectAll}
            isDisabled={isLoading || selectedCorpusId === null}
            className="w-full justify-start"
          >
            <span className="flex w-full flex-col items-start gap-1 text-start">
              <Label>{t("knowledge.qa.allCorpora")}</Label>
              <Description>{t("knowledge.qa.allCorporaDescription")}</Description>
            </span>
          </Button>

          {isLoading && <p className="mt-4 text-sm text-default-500">{t("knowledge.qa.loadingCorpora")}</p>}

          {!isLoading && corpora.length === 0 && (
            <p className="mt-4 text-sm text-default-500">
              {t("knowledge.qa.noCorpora")}
            </p>
          )}

          {!isLoading && corpora.length > 0 && (
            <ListBox
              aria-label={t("knowledge.qa.railTitle")}
              className="mt-4"
              selectionMode="single"
              selectedKeys={selectedCorpusId ? new Set([selectedCorpusId]) : new Set()}
              onSelectionChange={(keys) => {
                const key = Array.from(keys)[0];
                if (key != null) onSelectCorpus(String(key));
              }}
              disabledKeys={selectedCorpusId ? [selectedCorpusId] : []}
            >
              {corpora.map((corpus) => (
                <ListBox.Item key={corpus.corpusId} id={corpus.corpusId} textValue={corpus.name}>
                  <div className="flex flex-col">
                    <Label>{corpus.name}</Label>
                    {corpus.description && (
                      <Description className="line-clamp-2">{corpus.description}</Description>
                    )}
                    <Description>{formatCorpusCounts(corpus.sourceCount, corpus.chunkCount, t)}</Description>
                  </div>
                </ListBox.Item>
              ))}
            </ListBox>
          )}
        </Card.Content>
      </Card>
    </aside>
  );
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
