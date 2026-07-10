"use client";

import type { ReactNode } from "react";
import { ChatMessage } from "@heroui-pro/react/chat-message";
import { Markdown } from "@heroui-pro/react/markdown";

export interface ChatBodyProps {
  children: ReactNode;
}

export function ChatBody({ children }: ChatBodyProps) {
  return (
    <ChatMessage.Content>
      {typeof children === "string" ? <Markdown>{children}</Markdown> : children}
    </ChatMessage.Content>
  );
}
