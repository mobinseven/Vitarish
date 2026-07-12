"use client";

import { ListBox, Select } from "@heroui/react";
import type { KnowledgeQaLabels, KnowledgeUserCorpus } from "../types";

const ALL_KEY = "__all__";

interface KnowledgeCorpusSelectProps {
  className?: string;
  corpora: KnowledgeUserCorpus[];
  labels: KnowledgeQaLabels;
  selectedCorpusId: string | null;
  isLoading?: boolean;
  onSelectAll: () => void;
  onSelectCorpus: (corpusId: string) => void;
}

export function KnowledgeCorpusSelect({
  className,
  corpora,
  labels,
  selectedCorpusId,
  isLoading = false,
  onSelectAll,
  onSelectCorpus,
}: KnowledgeCorpusSelectProps) {
  const handleSelectionChange = (key: string | number | null) => {
    if (key === ALL_KEY || key === null) {
      onSelectAll();
      return;
    }

    onSelectCorpus(String(key));
  };

  return (
    <Select
      aria-label={labels.railTitle}
      className={className}
      isDisabled={isLoading}
      placeholder={isLoading ? labels.loadingCorpora : labels.noCorpora}
      value={selectedCorpusId ?? ALL_KEY}
      variant="secondary"
      onChange={handleSelectionChange}
    >
      <Select.Trigger>
        <i aria-hidden="true" className="fa-solid fa-book size-4 shrink-0 text-muted" />
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
  );
}
