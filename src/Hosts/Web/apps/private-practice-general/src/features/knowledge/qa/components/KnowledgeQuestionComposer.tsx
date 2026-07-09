"use client";

import { PromptInput } from "@heroui-pro/react";
import { ChatPromptInput } from "@vitarish/web-ui";
import { useLocalizedText } from "@/localization";

interface KnowledgeQuestionComposerProps {
  value: string;
  scopeLabel: string;
  isPending: boolean;
  hasAnswer: boolean;
  onChange: (value: string) => void;
  onSubmit: (question: string) => void;
  onNewQuestion: () => void;
}

export function KnowledgeQuestionComposer({
  value,
  scopeLabel,
  isPending,
  hasAnswer,
  onChange,
  onSubmit,
  onNewQuestion,
}: KnowledgeQuestionComposerProps) {
  const { direction, t } = useLocalizedText();

  return (
    <ChatPromptInput
      value={value}
      label={t("knowledge.qa.questionLabel")}
      placeholder={t("knowledge.qa.questionPlaceholder")}
      isPending={isPending}
      submitLabel={t("knowledge.qa.askAction")}
      direction={direction}
      footer={t("knowledge.qa.composerFooter")}
      helperText={t("knowledge.qa.scopeLabel", { scope: scopeLabel })}
      helperIcon={<i aria-hidden="true" className="fa-solid fa-book-open size-4 shrink-0" />}
      onChange={onChange}
      onSubmit={onSubmit}
      actions={
        hasAnswer ? (
          <PromptInput.Action
            aria-label={t("knowledge.qa.newQuestionAction")}
            tooltip={t("knowledge.qa.newQuestionAction")}
            onPress={onNewQuestion}
          >
            <i aria-hidden="true" className="fa-solid fa-plus size-4" />
          </PromptInput.Action>
        ) : null
      }
    />
  );
}
