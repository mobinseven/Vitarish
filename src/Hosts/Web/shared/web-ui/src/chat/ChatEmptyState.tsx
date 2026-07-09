"use client";

import { EmptyState } from "@heroui-pro/react/empty-state";

export interface ChatEmptyStateProps {
  title: string;
  description: string;
}

export function ChatEmptyState({ description, title }: ChatEmptyStateProps) {
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
