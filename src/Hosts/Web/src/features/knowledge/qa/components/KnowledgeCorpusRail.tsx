"use client";

import { Button, Card } from "@heroui/react";
import { SurfaceCard } from "@/design-system";
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
  return (
    <aside className="w-full lg:w-72 shrink-0">
      <SurfaceCard className="sticky top-6">
        <Card.Header>
          <div>
            <p className="text-xs uppercase tracking-[0.25em] text-muted">Corpora</p>
            <h2 className="text-lg font-semibold">Knowledge sources</h2>
          </div>
        </Card.Header>
        <Card.Content className="flex flex-col gap-3">
          <Button
            type="button"
            onPress={onSelectAll}
            isDisabled={isLoading}
            className={railButtonClass(selectedCorpusId === null)}
          >
            <span className="flex w-full flex-col items-start gap-1 text-left">
              <span className="font-medium">All corpora</span>
              <span className="text-xs text-muted">Ask across every available corpus</span>
            </span>
          </Button>

          {isLoading && <p className="text-sm text-muted">Loading corpora...</p>}

          {!isLoading && corpora.length === 0 && (
            <p className="ds-muted-panel text-sm">
              No knowledge sources are available yet.
            </p>
          )}

          {corpora.map((corpus) => (
            <Button
              key={corpus.corpusId}
              type="button"
              onPress={() => onSelectCorpus(corpus.corpusId)}
              className={railButtonClass(selectedCorpusId === corpus.corpusId)}
            >
              <span className="flex w-full flex-col items-start gap-1 text-left">
                <span className="font-medium">{corpus.name}</span>
                {corpus.description && (
                  <span className="line-clamp-2 text-xs text-muted">
                    {corpus.description}
                  </span>
                )}
                <span className="text-xs text-muted">
                  {corpus.sourceCount} source{corpus.sourceCount === 1 ? "" : "s"}
                  {corpus.chunkCount > 0 ? ` · ${corpus.chunkCount} chunks` : ""}
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
    "h-auto w-full justify-start rounded-2xl px-4 py-3 text-left transition-colors",
    isSelected
      ? "bg-[color-mix(in_oklch,var(--accent)_16%,transparent)] text-[var(--accent)]"
      : "bg-transparent hover:bg-[var(--accent-soft)]",
  ].join(" ");
}
