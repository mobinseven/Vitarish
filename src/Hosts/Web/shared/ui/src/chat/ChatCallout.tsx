"use client";

import type { ReactNode } from "react";
import { ChatMessage } from "@heroui-pro/react/chat-message";
import { getChatToneTextClassName } from "./chatTone";
import type { ChatTone } from "./types";

export interface ChatCalloutProps {
  children: ReactNode;
  tone?: ChatTone;
}

export function ChatCallout({ children, tone = "default" }: ChatCalloutProps) {
  return (
    <ChatMessage.Content>
      <p className={`text-sm ${getChatToneTextClassName(tone)}`}>{children}</p>
    </ChatMessage.Content>
  );
}
