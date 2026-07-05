"use client";

import type { ReactNode } from "react";
import { ChatMessage } from "@heroui-pro/react";

export function KnowledgeChatBody({ children }: { children: ReactNode }) {
  return <ChatMessage.Body>{children}</ChatMessage.Body>;
}
