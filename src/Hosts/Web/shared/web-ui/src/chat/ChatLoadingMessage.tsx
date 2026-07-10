"use client";

import { ChatLoader } from "@heroui-pro/react/chat-loader";
import { ChatMessage } from "@heroui-pro/react/chat-message";
import { TextShimmer } from "@heroui-pro/react/text-shimmer";

export interface ChatLoadingMessageProps {
  avatarAlt: string;
  avatarFallback: string;
  loadingLabel: string;
  message: string;
  title: string;
}

export function ChatLoadingMessage({
  avatarAlt,
  avatarFallback,
  loadingLabel,
  message,
  title,
}: ChatLoadingMessageProps) {
  return (
    <ChatMessage.Assistant>
      <ChatMessage.Avatar alt={avatarAlt} fallback={avatarFallback} show />
      <ChatMessage.Body>
        <TextShimmer>{title}</TextShimmer>
        <ChatLoader.Dots label={loadingLabel} />
        <ChatMessage.Content>
          <p className="text-sm text-muted">{message}</p>
        </ChatMessage.Content>
      </ChatMessage.Body>
    </ChatMessage.Assistant>
  );
}
