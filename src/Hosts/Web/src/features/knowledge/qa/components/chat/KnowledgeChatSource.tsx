"use client";

import { ChatSource } from "@heroui-pro/react";

export function KnowledgeChatSource({
  title,
  body,
  meta,
}: {
  title: string;
  body?: string | null;
  meta?: string;
}) {
  const description = [meta, body].filter(Boolean).join("\n\n") || undefined;

  return (
    <ChatSource
      description={description}
      sourceType="document"
      title={title}
    />
  );
}
