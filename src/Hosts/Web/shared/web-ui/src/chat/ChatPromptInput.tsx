"use client";

import type { CSSProperties, ReactNode } from "react";
import { PromptInput } from "@heroui-pro/react/prompt-input";
import type { ChatTextDirection } from "./types";

export interface ChatPromptInputProps {
  actions?: ReactNode;
  direction?: ChatTextDirection;
  footer?: ReactNode;
  helperIcon?: ReactNode;
  helperText?: ReactNode;
  isPending?: boolean;
  label: string;
  onChange: (value: string) => void;
  onSubmit: (value: string) => void;
  placeholder: string;
  submitLabel: string;
  value: string;
}

export function ChatPromptInput({
  actions,
  direction,
  footer,
  helperIcon,
  helperText,
  isPending = false,
  label,
  onChange,
  onSubmit,
  placeholder,
  submitLabel,
  value,
}: ChatPromptInputProps) {
  const canSend = value.trim().length > 0;
  const toolbarEndStyle: CSSProperties | undefined =
    direction === "rtl"
      ? { marginInlineStart: "auto", marginLeft: 0 }
      : undefined;

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
            aria-label={label}
            disabled={isPending}
            placeholder={placeholder}
          />
        </PromptInput.Content>
        <PromptInput.Toolbar>
          <PromptInput.ToolbarStart>
            {helperText && (
              <span className="flex min-w-0 items-center gap-2 text-xs text-muted">
                {helperIcon}
                <span className="truncate">{helperText}</span>
              </span>
            )}
          </PromptInput.ToolbarStart>
          <PromptInput.ToolbarEnd style={toolbarEndStyle}>
            {actions}
            <PromptInput.Send
              aria-label={submitLabel}
              isDisabled={isPending || !canSend}
            />
          </PromptInput.ToolbarEnd>
        </PromptInput.Toolbar>
      </PromptInput.Shell>
      {footer && <PromptInput.Footer>{footer}</PromptInput.Footer>}
    </PromptInput>
  );
}
