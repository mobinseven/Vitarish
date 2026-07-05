"use client";

import type { ReactNode } from "react";
import { ChatMessage } from "@heroui-pro/react";
import type { KnowledgeChatTone } from "./types";

export function KnowledgeChatCallout({
  tone = "default",
  children,
}: {
  tone?: KnowledgeChatTone;
  children: ReactNode;
}) {
  const toneClass = {
    default: "text-muted",
    success: "text-success-600",
    warning: "text-warning-600",
    danger: "text-danger-600",
  }[tone];

  return (
    <ChatMessage.Content>
      <p className={`text-sm ${toneClass}`}>{children}</p>
    </ChatMessage.Content>
  );
}
