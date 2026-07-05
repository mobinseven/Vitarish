"use client";

import type { ReactNode } from "react";
import { ChatMessage, Markdown } from "@heroui-pro/react";

export function KnowledgeChatBody({ children }: { children: ReactNode }) {
  return (
    <ChatMessage.Content>
      {typeof children === "string" ? <Markdown>{children}</Markdown> : children}
    </ChatMessage.Content>
  );
}
