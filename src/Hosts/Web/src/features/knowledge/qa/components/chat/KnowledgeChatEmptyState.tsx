"use client";

import { EmptyState } from "@heroui-pro/react";

export function KnowledgeChatEmptyState({
  title,
  description,
}: {
  title: string;
  description: string;
}) {
  return (
    <div className="flex min-h-[360px] items-center justify-center">
      <EmptyState.Root className="max-w-md">
        <EmptyState.Header>
          <EmptyState.Title>{title}</EmptyState.Title>
          <EmptyState.Description>{description}</EmptyState.Description>
        </EmptyState.Header>
      </EmptyState.Root>
    </div>
  );
}
