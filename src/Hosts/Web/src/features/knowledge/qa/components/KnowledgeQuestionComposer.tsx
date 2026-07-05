"use client";

import { PromptInput } from "@heroui-pro/react";
import { useLocalizedText } from "@/localization";
import { KnowledgePromptInput } from "./chat";

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
  const { t } = useLocalizedText();

  return (
    <KnowledgePromptInput
      value={value}
      label={t("knowledge.qa.questionLabel")}
      placeholder={t("knowledge.qa.questionPlaceholder")}
      isPending={isPending}
      submitLabel={t("knowledge.qa.askAction")}
      helperText={t("knowledge.qa.scopeLabel", { scope: scopeLabel })}
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
