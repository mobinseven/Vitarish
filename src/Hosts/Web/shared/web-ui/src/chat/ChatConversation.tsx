"use client";

import type { ReactNode } from "react";
import { ChatConversation as HeroChatConversation } from "@heroui-pro/react/chat-conversation";

export interface ChatConversationProps {
  children: ReactNode;
  className?: string;
  contentClassName?: string;
  innerClassName?: string;
}

export function ChatConversation({
  children,
  className,
  contentClassName,
  innerClassName,
}: ChatConversationProps) {
  return (
    <HeroChatConversation className={className ?? "min-h-0 flex-1"}>
      <HeroChatConversation.Content className={contentClassName ?? "flex flex-col"}>
        <div className={innerClassName ?? "mx-auto flex w-full max-w-[714px] flex-col gap-8 px-4 pb-6 pt-10"}>
          {children}
        </div>
        <HeroChatConversation.ScrollAnchor />
      </HeroChatConversation.Content>
    </HeroChatConversation>
  );
}
