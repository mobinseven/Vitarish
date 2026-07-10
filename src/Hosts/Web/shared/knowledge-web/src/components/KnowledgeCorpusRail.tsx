"use client";

import { useCallback } from "react";
import { ChatListView } from "@heroui-pro/react/chat-list-view";
import type { KnowledgeQaLabels, KnowledgeUserCorpus } from "../types";

const ALL_KEY = "__all__";

interface KnowledgeCorpusRailProps {
  corpora: KnowledgeUserCorpus[];
  labels: KnowledgeQaLabels;
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
  labels,
  selectedCorpusId,
  isLoading = false,
  onSelectAll,
  onSelectCorpus,
}: KnowledgeCorpusRailProps) {
  const items: CorpusItem[] = corpora.map((corpus) => ({
    corpusId: corpus.corpusId,
    name: corpus.name,
    description: corpus.description ?? null,
    sourceCount: corpus.sourceCount,
    chunkCount: corpus.chunkCount,
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
    <aside className="flex max-h-56 w-full shrink-0 flex-col border-b border-separator bg-background px-3 py-3 lg:max-h-none lg:w-72 lg:border-b-0 lg:border-e">
      <div className="flex min-h-0 flex-col gap-3">
        <div className="px-2 py-1">
          <p className="text-xs uppercase tracking-[0.25em] text-muted">{labels.railEyebrow}</p>
          <h2 className="text-sm font-medium text-foreground">{labels.railTitle}</h2>
        </div>

        {isLoading && <p className="px-2 text-sm text-muted">{labels.loadingCorpora}</p>}

        {!isLoading && corpora.length === 0 && (
          <p className="px-2 text-sm text-muted">{labels.noCorpora}</p>
        )}

        {!isLoading && corpora.length > 0 && (
          <div className="min-h-0 overflow-y-auto">
            <ChatListView.Root
              aria-label={labels.railTitle}
              selectionMode="single"
              selectedKeys={selectedKeys}
              onSelectionChange={handleSelectionChange}
            >
              <ChatListView.Item id={ALL_KEY} textValue={labels.allCorpora}>
                <ChatListView.ItemContent>
                  <ChatListView.Text>
                    <ChatListView.Title>{labels.allCorpora}</ChatListView.Title>
                    <ChatListView.Preview>{labels.allCorporaDescription}</ChatListView.Preview>
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
                      <ChatListView.Preview>
                        {formatCorpusCounts(corpus.sourceCount, corpus.chunkCount, labels)}
                      </ChatListView.Preview>
                    </ChatListView.Text>
                  </ChatListView.ItemContent>
                </ChatListView.Item>
              ))}
            </ChatListView.Root>
          </div>
        )}
      </div>
    </aside>
  );
}

function formatCorpusCounts(sourceCount: number, chunkCount: number, labels: KnowledgeQaLabels): string {
  const sourceLabel = labels.sourceCount(sourceCount);

  if (chunkCount <= 0) {
    return sourceLabel;
  }

  return `${sourceLabel} / ${labels.chunkCount(chunkCount)}`;
}
