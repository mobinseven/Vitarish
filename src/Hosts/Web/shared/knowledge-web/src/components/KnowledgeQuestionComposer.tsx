"use client";

import { PromptInput } from "@heroui-pro/react/prompt-input";
import { ChatPromptInput, type ChatTextDirection } from "@vitarish/web-ui";
import type { KnowledgeQaLabels } from "../types";

interface KnowledgeQuestionComposerProps {
  direction?: ChatTextDirection;
  hasAnswer: boolean;
  isPending: boolean;
  labels: KnowledgeQaLabels;
  onChange: (value: string) => void;
  onNewQuestion: () => void;
  onSubmit: (question: string) => void;
  scopeLabel: string;
  value: string;
}

export function KnowledgeQuestionComposer({
  direction,
  hasAnswer,
  isPending,
  labels,
  onChange,
  onNewQuestion,
  onSubmit,
  scopeLabel,
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
      helperText={labels.scopeLabel(scopeLabel)}
      helperIcon={<i aria-hidden="true" className="fa-solid fa-book-open size-4 shrink-0" />}
      onChange={onChange}
      onSubmit={onSubmit}
      actions={
        hasAnswer ? (
          <PromptInput.Action
            aria-label={labels.newQuestionAction}
            tooltip={labels.newQuestionAction}
            onPress={onNewQuestion}
          >
            <i aria-hidden="true" className="fa-solid fa-plus size-4" />
          </PromptInput.Action>
        ) : null
      }
    />
  );
}
