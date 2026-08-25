"use client";

import { ChatSource as HeroChatSource } from "@heroui-pro/react/chat-source";

export interface ChatSourceProps {
  body?: string | null;
  meta?: string;
  title: string;
}

export function ChatSource({ body, meta, title }: ChatSourceProps) {
  const description = [meta, body].filter(Boolean).join("\n\n") || undefined;

  return (
    <HeroChatSource
      description={description}
      sourceType="document"
      title={title}
    />
  );
}
