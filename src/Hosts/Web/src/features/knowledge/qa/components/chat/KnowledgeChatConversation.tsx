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
    <ChatConversation className={className ?? "min-h-0 flex-1"}>
      <ChatConversation.Content className="flex flex-col">
        <div className="mx-auto flex w-full max-w-[714px] flex-col gap-8 px-4 pb-6 pt-10">
          {children}
        </div>
        <ChatConversation.ScrollAnchor />
      </ChatConversation.Content>
    </ChatConversation>
  );
}
