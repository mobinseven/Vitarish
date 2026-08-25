"use client";

import { useRef, type ReactNode } from "react";
import {
  appendConfirmedTranscript,
  useLiveDictation,
} from "../hooks/useLiveDictation";
import { LiveDictation } from "./LiveDictation";
import { ChatPromptInput, type ChatTextDirection } from "@vitarish/ui";

export interface AgentConversationComposerProps {
  actions?: ReactNode;
  direction?: ChatTextDirection;
  enabled?: boolean;
  footer?: ReactNode;
  helperIcon?: ReactNode;
  helperText?: ReactNode;
  isPending?: boolean;
  label: string;
  onChange: (value: string) => void;
  onSubmit: (value: string) => void | Promise<void>;
  placeholder: string;
  preferredLanguage: string;
  sessionKey: string;
  submitLabel: string;
  toolbarStart?: ReactNode;
  value: string;
}

export function AgentConversationComposer({
  actions,
  direction,
  enabled = true,
  footer,
  helperIcon,
  helperText,
  isPending = false,
  label,
  onChange,
  onSubmit,
  placeholder,
  preferredLanguage,
  sessionKey,
  submitLabel,
  toolbarStart,
  value,
}: AgentConversationComposerProps) {
  const valueRef = useRef(value);
  valueRef.current = value;
  const dictation = useLiveDictation({
    enabled: enabled && !isPending,
    onSegment: (segment) => {
      const nextValue = appendConfirmedTranscript(valueRef.current, segment);
      valueRef.current = nextValue;
      onChange(nextValue);
    },
    preferredLanguage,
    sessionKey,
  });

  return (
    <ChatPromptInput
      actions={
        <>
          <LiveDictation controller={dictation} />
          {actions}
        </>
      }
      direction={direction}
      footer={footer}
      helperIcon={helperIcon}
      helperText={helperText}
      isPending={isPending}
      isSendDisabled={dictation.isActive}
      label={label}
      placeholder={placeholder}
      submitLabel={submitLabel}
      toolbarStart={toolbarStart}
      value={value}
      onChange={onChange}
      onSubmit={onSubmit}
    />
  );
}
