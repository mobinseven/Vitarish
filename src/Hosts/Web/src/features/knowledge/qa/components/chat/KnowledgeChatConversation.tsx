"use client";

import type { ReactNode } from "react";
import { ChatConversation } from "@heroui-pro/react";

export function KnowledgeChatConversation({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}) {
  return (
    <ChatConversation.Root className={className}>
      <ChatConversation.Content>{children}</ChatConversation.Content>
    </ChatConversation.Root>
  );
}
