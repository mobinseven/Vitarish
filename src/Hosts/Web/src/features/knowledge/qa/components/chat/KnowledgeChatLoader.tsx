"use client";

import { ChatLoader } from "@heroui-pro/react";
import { useLocalizedText } from "@/localization";

export function KnowledgeChatLoader({ message }: { message: string }) {
  const { t } = useLocalizedText();

  return (
    <div className="flex flex-col gap-4 p-6">
      <ChatLoader.Dots label={t("knowledge.qa.thinking")} />
      <h2 className="text-xl font-semibold">{t("knowledge.qa.searchingContext")}</h2>
      <p className="text-sm text-muted">{message}</p>
    </div>
  );
}
