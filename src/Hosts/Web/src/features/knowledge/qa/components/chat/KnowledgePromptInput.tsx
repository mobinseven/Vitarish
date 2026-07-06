"use client";

import type { ReactNode } from "react";
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
  const { direction, t } = useLocalizedText();
  const resolvedLabel = label ?? t("knowledge.qa.questionLabel");
  const resolvedPlaceholder = placeholder ?? t("knowledge.qa.composerTitle");
  const resolvedSubmitLabel = submitLabel ?? t("knowledge.qa.askAction");
  const canSend = value.trim().length > 0;

  const handleSubmit = () => {
    onSubmit(value);
  };

  return (
    <PromptInput
      status={isPending ? "streaming" : "ready"}
      value={value}
      variant="primary"
      onSubmit={handleSubmit}
      onValueChange={onChange}
    >
      <PromptInput.Shell>
        <PromptInput.Content>
          <PromptInput.TextArea
            aria-label={resolvedLabel}
            disabled={isPending}
            placeholder={resolvedPlaceholder}
          />
        </PromptInput.Content>
        <PromptInput.Toolbar>
          <PromptInput.ToolbarStart>
            {helperText && (
              <span className="flex min-w-0 items-center gap-2 text-xs text-muted">
                <i aria-hidden="true" className="fa-solid fa-book-open size-4 shrink-0" />
                <span className="truncate">{helperText}</span>
              </span>
            )}
          </PromptInput.ToolbarStart>
          <PromptInput.ToolbarEnd
            style={direction === "rtl" ? { marginInlineStart: "auto", marginLeft: 0 } : undefined}
          >
            {actions}
            <PromptInput.Send
              aria-label={resolvedSubmitLabel}
              isDisabled={isPending || !canSend}
            />
          </PromptInput.ToolbarEnd>
        </PromptInput.Toolbar>
      </PromptInput.Shell>
      <PromptInput.Footer>{t("knowledge.qa.composerFooter")}</PromptInput.Footer>
    </PromptInput>
  );
}
