"use client";

import { ChatLoader, ChatMessage, TextShimmer } from "@heroui-pro/react";
import { useLocalizedText } from "@/localization";

export function KnowledgeChatLoader({ message }: { message: string }) {
  const { t } = useLocalizedText();

  return (
    <ChatMessage.Assistant>
      <ChatMessage.Avatar
        alt={t("knowledge.qa.assistantAvatarAlt")}
        fallback={t("knowledge.qa.assistantAvatarFallback")}
        show
      />
      <ChatMessage.Body>
        <TextShimmer>{t("knowledge.qa.searchingContext")}</TextShimmer>
        <ChatLoader.Dots label={t("knowledge.qa.thinking")} />
        <ChatMessage.Content>
          <p className="text-sm text-muted">{message}</p>
        </ChatMessage.Content>
      </ChatMessage.Body>
    </ChatMessage.Assistant>
  );
}
