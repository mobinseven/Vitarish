"use client";

import type { ReactNode } from "react";
import { ChatMessage } from "@heroui-pro/react";
import { useLocalizedText } from "@/localization";
import type { KnowledgeChatItemKind, KnowledgeChatTone } from "./types";

function KnowledgeChatStatus({ label, tone }: { label: string; tone: KnowledgeChatTone }) {
  const toneClass = {
    default: "text-muted",
    success: "text-success-600",
    warning: "text-warning-600",
    danger: "text-danger-600",
  }[tone];

  return <span className={`text-xs font-medium ${toneClass}`}>{label}</span>;
}

export function KnowledgeChatMessage({
  kind,
  title,
  subtitle,
  status,
  tone = "default",
  actions,
  children,
}: {
  kind: KnowledgeChatItemKind;
  title?: string;
  subtitle?: string;
  status?: string;
  tone?: KnowledgeChatTone;
  actions?: ReactNode;
  children: ReactNode;
}) {
  const { t } = useLocalizedText();

  if (kind === "question") {
    return (
      <ChatMessage.User>
        <ChatMessage.Bubble>
          <ChatMessage.Content>{children}</ChatMessage.Content>
        </ChatMessage.Bubble>
      </ChatMessage.User>
    );
  }

  return (
    <ChatMessage.Assistant>
      <ChatMessage.Avatar
        alt={t("knowledge.qa.assistantAvatarAlt")}
        fallback={t("knowledge.qa.assistantAvatarFallback")}
        show
      />

      <ChatMessage.Body>
        {title || subtitle || status ? (
          <div className="flex flex-col gap-1">
            <div className="flex flex-wrap items-center gap-2">
              {title && <span className="text-sm font-medium text-foreground">{title}</span>}
              {status && <KnowledgeChatStatus label={status} tone={tone} />}
            </div>
            {subtitle && <p className="text-sm text-muted">{subtitle}</p>}
          </div>
        ) : null}

        {children}

        {actions && <ChatMessage.Actions>{actions}</ChatMessage.Actions>}
      </ChatMessage.Body>
    </ChatMessage.Assistant>
  );
}
