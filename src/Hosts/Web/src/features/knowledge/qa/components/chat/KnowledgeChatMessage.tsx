"use client";

import type { ReactNode } from "react";
import { ChatMessage } from "@heroui-pro/react";
import type { KnowledgeChatItemKind, KnowledgeChatTone } from "./types";

function KnowledgeChatStatus({ label, tone }: { label: string; tone: KnowledgeChatTone }) {
  const toneClass = {
    default: "bg-surface-secondary text-muted",
    success: "bg-success-100 text-success-600",
    warning: "bg-warning-100 text-warning-600",
    danger: "bg-danger-100 text-danger-600",
  }[tone];

  return <span className={`rounded-full px-3 py-1 text-xs font-medium ${toneClass}`}>{label}</span>;
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
  const Wrapper = kind === "question" ? ChatMessage.User : ChatMessage.Assistant;

  return (
    <Wrapper className={kind === "question" ? "ms-auto max-w-3xl" : ""}>
      <ChatMessage.Bubble>
        <div className="flex w-full flex-col gap-2 md:flex-row md:items-start md:justify-between">
          <div>
            <p className="text-xs uppercase tracking-[0.25em] text-muted">{kind}</p>
            {title && <h2 className="text-xl font-semibold">{title}</h2>}
            {subtitle && <p className="mt-1 text-sm text-muted">{subtitle}</p>}
          </div>
          {status && <KnowledgeChatStatus label={status} tone={tone} />}
        </div>
        <ChatMessage.Content className="flex flex-col gap-5">
          {children}
          {actions && <ChatMessage.Actions>{actions}</ChatMessage.Actions>}
        </ChatMessage.Content>
      </ChatMessage.Bubble>
    </Wrapper>
  );
}
