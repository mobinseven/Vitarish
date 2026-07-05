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
  return (
    <ChatSource.Root title={title}>
      <ChatSource.Trigger>
        <ChatSource.Title>{title}</ChatSource.Title>
      </ChatSource.Trigger>
      {meta && <span className="shrink-0 text-xs text-muted">{meta}</span>}
      {body && <p className="mt-1 text-muted text-sm">{body}</p>}
    </ChatSource.Root>
  );
}
