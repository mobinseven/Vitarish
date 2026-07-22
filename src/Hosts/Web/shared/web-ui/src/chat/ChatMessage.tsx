"use client";

import type { ReactNode } from "react";
import { ChatMessage as HeroChatMessage } from "@heroui-pro/react/chat-message";
import { getChatToneTextClassName } from "./chatTone";
import type { ChatMessageKind, ChatTone } from "./types";

interface ChatMessageBaseProps {
  actions?: ReactNode;
  children: ReactNode;
  status?: string;
  subtitle?: string;
  title?: string;
  tone?: ChatTone;
}

interface ChatQuestionMessageProps extends ChatMessageBaseProps {
  kind: "question";
  avatarAlt?: never;
  avatarFallback?: never;
}

interface ChatAssistantMessageProps extends ChatMessageBaseProps {
  kind: Exclude<ChatMessageKind, "question">;
  avatarAlt: string;
  avatarFallback: string;
}

export type ChatMessageProps =
  ChatQuestionMessageProps | ChatAssistantMessageProps;

function ChatStatus({ label, tone }: { label: string; tone: ChatTone }) {
  return (
    <span className={`text-xs font-medium ${getChatToneTextClassName(tone)}`}>
      {label}
    </span>
  );
}

export function ChatMessage({
  actions,
  avatarAlt,
  avatarFallback,
  children,
  kind,
  status,
  subtitle,
  title,
  tone = "default",
}: ChatMessageProps) {
  if (kind === "question") {
    return (
      <HeroChatMessage.User>
        <HeroChatMessage.Bubble>
          <HeroChatMessage.Content>{children}</HeroChatMessage.Content>
        </HeroChatMessage.Bubble>
      </HeroChatMessage.User>
    );
  }

  return (
    <HeroChatMessage.Assistant>
      <HeroChatMessage.Avatar alt={avatarAlt} fallback={avatarFallback} show />

      <HeroChatMessage.Body>
        {title || subtitle || status ? (
          <div className="flex flex-col gap-1">
            <div className="flex flex-wrap items-center gap-2">
              {title && (
                <span className="text-sm font-medium text-foreground">
                  {title}
                </span>
              )}
              {status && <ChatStatus label={status} tone={tone} />}
            </div>
            {subtitle && <p className="text-sm text-muted">{subtitle}</p>}
          </div>
        ) : null}

        {children}

        {actions && (
          <HeroChatMessage.Actions>{actions}</HeroChatMessage.Actions>
        )}
      </HeroChatMessage.Body>
    </HeroChatMessage.Assistant>
  );
}
