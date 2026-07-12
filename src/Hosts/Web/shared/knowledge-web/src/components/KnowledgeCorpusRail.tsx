"use client";

import { ListBox, Select } from "@heroui/react";
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

export function KnowledgeCorpusRail({
  corpora,
  labels,
  selectedCorpusId,
  isLoading = false,
  onSelectAll,
  onSelectCorpus,
}: KnowledgeCorpusRailProps) {
  const handleSelectionChange = (key: string | number | null) => {
      if (key === ALL_KEY || key === null) {
        onSelectAll();
      } else {
        onSelectCorpus(String(key));
      }
  };

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
          <Select
            aria-label={labels.railTitle}
            value={selectedCorpusId ?? ALL_KEY}
            onChange={handleSelectionChange}
            variant="secondary"
          >
            <Select.Trigger>
              <Select.Value />
              <Select.Indicator />
            </Select.Trigger>
            <Select.Popover>
              <ListBox>
                <ListBox.Item id={ALL_KEY} textValue={labels.allCorpora}>
                  {labels.allCorpora}
                  <ListBox.ItemIndicator />
                </ListBox.Item>
                {corpora.map((corpus) => (
                  <ListBox.Item key={corpus.corpusId} id={corpus.corpusId} textValue={corpus.name}>
                    {corpus.name}
                    <ListBox.ItemIndicator />
                  </ListBox.Item>
                ))}
              </ListBox>
            </Select.Popover>
          </Select>
        )}
      </div>
    </aside>
  );
}
