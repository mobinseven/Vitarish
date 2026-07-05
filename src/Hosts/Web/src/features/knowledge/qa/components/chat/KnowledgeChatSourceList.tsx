"use client";

import type { ReactNode } from "react";
import { ChatSources } from "@heroui-pro/react";

export function KnowledgeChatSourceList({
  title,
  children,
}: {
  title: string;
  children: ReactNode;
}) {
  return (
    <ChatSources defaultExpanded={false}>
      <ChatSources.Trigger>{title}</ChatSources.Trigger>
      <ChatSources.Content>
        <ChatSources.List>{children}</ChatSources.List>
      </ChatSources.Content>
    </ChatSources>
  );
}
