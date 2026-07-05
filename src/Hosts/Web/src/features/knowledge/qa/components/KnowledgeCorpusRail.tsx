"use client";

import { useCallback } from "react";
import { ChatListView } from "@heroui-pro/react";
import { useLocalizedText } from "@/localization";
import type { KnowledgeUserCorpus } from "../types";

const ALL_KEY = "__all__";

interface KnowledgeCorpusRailProps {
  corpora: KnowledgeUserCorpus[];
  selectedCorpusId: string | null;
  isLoading?: boolean;
  onSelectAll: () => void;
  onSelectCorpus: (corpusId: string) => void;
}

interface CorpusItem {
  corpusId: string;
  name: string;
  description: string | null;
  sourceCount: number;
  chunkCount: number;
}

export function KnowledgeCorpusRail({
  corpora,
  selectedCorpusId,
  isLoading = false,
  onSelectAll,
  onSelectCorpus,
}: KnowledgeCorpusRailProps) {
  const { t } = useLocalizedText();

  const items: CorpusItem[] = corpora.map((c) => ({
    corpusId: c.corpusId,
    name: c.name,
    description: c.description ?? null,
    sourceCount: c.sourceCount,
    chunkCount: c.chunkCount,
  }));

  const selectedKeys = selectedCorpusId === null
    ? new Set([ALL_KEY])
    : new Set([selectedCorpusId]);

  const handleSelectionChange = useCallback(
    (keys: string | Set<string | number>) => {
      if (typeof keys === "string") {
        if (keys === ALL_KEY) {
          onSelectAll();
        } else {
          onSelectCorpus(keys);
        }
        return;
      }
      const key = Array.from(keys)[0];
      if (key === ALL_KEY || key == null) {
        onSelectAll();
      } else {
        onSelectCorpus(String(key));
      }
    },
    [onSelectAll, onSelectCorpus]
  );

  return (
    <aside className="w-full lg:w-72 shrink-0">
      <div className="sticky top-6 flex flex-col gap-3">
        <div>
          <p className="text-xs uppercase tracking-[0.25em] text-muted">{t("knowledge.qa.railEyebrow")}</p>
          <h2 className="text-lg font-semibold">{t("knowledge.qa.railTitle")}</h2>
        </div>

        {isLoading && <p className="text-sm text-muted">{t("knowledge.qa.loadingCorpora")}</p>}

        {!isLoading && corpora.length === 0 && (
          <p className="text-sm text-muted">{t("knowledge.qa.noCorpora")}</p>
        )}

        {!isLoading && corpora.length > 0 && (
          <ChatListView.Root
            aria-label={t("knowledge.qa.railTitle")}
            selectionMode="single"
            selectedKeys={selectedKeys}
            onSelectionChange={handleSelectionChange}
          >
            <ChatListView.Item id={ALL_KEY} textValue={t("knowledge.qa.allCorpora")}>
              <ChatListView.ItemContent>
                <ChatListView.Text>
                  <ChatListView.Title>{t("knowledge.qa.allCorpora")}</ChatListView.Title>
                  <ChatListView.Preview>{t("knowledge.qa.allCorporaDescription")}</ChatListView.Preview>
                </ChatListView.Text>
              </ChatListView.ItemContent>
            </ChatListView.Item>

            {items.map((corpus) => (
              <ChatListView.Item key={corpus.corpusId} id={corpus.corpusId} textValue={corpus.name}>
                <ChatListView.ItemContent>
                  <ChatListView.Text>
                    <ChatListView.Title>{corpus.name}</ChatListView.Title>
                    {corpus.description && (
                      <ChatListView.Preview className="line-clamp-2">{corpus.description}</ChatListView.Preview>
                    )}
                    <ChatListView.Preview>{formatCorpusCounts(corpus.sourceCount, corpus.chunkCount, t)}</ChatListView.Preview>
                  </ChatListView.Text>
                </ChatListView.ItemContent>
              </ChatListView.Item>
            ))}
          </ChatListView.Root>
        )}
      </div>
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
