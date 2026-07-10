"use client";

import type { ReactNode } from "react";
import { ChatSources } from "@heroui-pro/react/chat-source";

export interface ChatSourceListProps {
  children: ReactNode;
  title: string;
}

export function ChatSourceList({ children, title }: ChatSourceListProps) {
  return (
    <ChatSources defaultExpanded={false}>
      <ChatSources.Trigger>{title}</ChatSources.Trigger>
      <ChatSources.Content>
        <ChatSources.List>{children}</ChatSources.List>
      </ChatSources.Content>
    </ChatSources>
  );
}
