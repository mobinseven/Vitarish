"use client";

import type { FormEvent, ReactNode } from "react";
import { PromptInput } from "@heroui-pro/react";
import { useLocalizedText } from "@/localization";

export function KnowledgePromptInput({
  value,
  label,
  placeholder,
  isPending = false,
  submitLabel,
  helperText,
  actions,
  onChange,
  onSubmit,
}: {
  value: string;
  label?: string;
  placeholder?: string;
  isPending?: boolean;
  submitLabel?: string;
  helperText?: string;
  actions?: ReactNode;
  onChange: (value: string) => void;
  onSubmit: (value: string) => void;
}) {
  const { t } = useLocalizedText();
  const resolvedLabel = label ?? t("knowledge.qa.questionLabel");
  const resolvedPlaceholder = placeholder ?? t("knowledge.qa.composerTitle");
  const resolvedSubmitLabel = submitLabel ?? t("knowledge.qa.askAction");

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    onSubmit(value);
  };

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      <PromptInput
        value={value}
        onValueChange={onChange}
        onSubmit={() => onSubmit(value)}
        isDisabled={isPending}
        status={isPending ? "streaming" : "ready"}
      >
        <PromptInput.Shell>
          <PromptInput.Content>
            <PromptInput.TextArea
              placeholder={resolvedPlaceholder}
              aria-label={resolvedLabel}
            />
          </PromptInput.Content>
          <PromptInput.Toolbar>
            <PromptInput.ToolbarStart>
              {helperText && (
                <PromptInput.Footer>{helperText}</PromptInput.Footer>
              )}
            </PromptInput.ToolbarStart>
            <PromptInput.ToolbarEnd>
              {actions}
              <PromptInput.Send>{resolvedSubmitLabel}</PromptInput.Send>
            </PromptInput.ToolbarEnd>
          </PromptInput.Toolbar>
        </PromptInput.Shell>
      </PromptInput>
    </form>
  );
}
