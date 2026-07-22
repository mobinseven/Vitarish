"use client";

import { PromptInput } from "@heroui-pro/react/prompt-input";
import {
  ChatPromptInput,
  FaIcon,
  type ChatTextDirection,
} from "@vitarish/web-ui";
import { KnowledgeCorpusSelect } from "./KnowledgeCorpusSelect";
import type { KnowledgeQaLabels, KnowledgeUserCorpus } from "../types";

interface KnowledgeQuestionComposerProps {
  corpora: KnowledgeUserCorpus[];
  direction?: ChatTextDirection;
  hasAnswer: boolean;
  isPending: boolean;
  isWorkspaceLoading: boolean;
  labels: KnowledgeQaLabels;
  onChange: (value: string) => void;
  onNewQuestion: () => void;
  onSelectAll: () => void;
  onSelectCorpus: (corpusId: string) => void;
  onSubmit: (question: string) => void;
  selectedCorpusId: string | null;
  value: string;
}

export function KnowledgeQuestionComposer({
  corpora,
  direction,
  hasAnswer,
  isPending,
  isWorkspaceLoading,
  labels,
  onChange,
  onNewQuestion,
  onSelectAll,
  onSelectCorpus,
  onSubmit,
  selectedCorpusId,
  value,
}: KnowledgeQuestionComposerProps) {
  return (
    <ChatPromptInput
      value={value}
      label={labels.questionLabel}
      placeholder={labels.questionPlaceholder}
      isPending={isPending}
      submitLabel={labels.askAction}
      direction={direction}
      footer={labels.composerFooter}
      helperText={
        <KnowledgeCorpusSelect
          className="w-44 sm:w-56"
          corpora={corpora}
          isLoading={isWorkspaceLoading}
          labels={labels}
          selectedCorpusId={selectedCorpusId}
          onSelectAll={onSelectAll}
          onSelectCorpus={onSelectCorpus}
        />
      }
      onChange={onChange}
      onSubmit={onSubmit}
      actions={
        hasAnswer ? (
          <PromptInput.Action
            aria-label={labels.newQuestionAction}
            tooltip={labels.newQuestionAction}
            onPress={onNewQuestion}
          >
            <FaIcon icon="plus" aria-hidden="true" className="size-4" />
          </PromptInput.Action>
        ) : null
      }
    />
  );
}
