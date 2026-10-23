"use client";

import { useState, type ReactNode } from "react";
import { Chip } from "@heroui/react";
import { useLocalizedText } from "@vitarish/localization";
import { ChatMessage } from "@vitarish/ui";
import type { InteractiveReference, InteractiveReply } from "../interaction/interactiveReply";
import { InteractiveOptionPicker } from "./InteractiveOptionPicker";
import { RememberedNotes } from "./UserMemory";

export interface InteractiveReferenceChipsProps {
  items: readonly InteractiveReference[];
}

/** The items a reply refers to, each with its status as a soft chip. */
export function InteractiveReferenceChips({ items }: InteractiveReferenceChipsProps) {
  const { t } = useLocalizedText();
  if (items.length === 0) return null;

  return (
    <ul aria-label={t("intelligence.interactive.references")} className="flex flex-wrap gap-2">
      {items.map((item) => (
        <li key={item.key} className="flex items-center gap-2 border border-default px-3 py-1.5">
          <span>{item.label}</span>
          {item.statusLabel ? (
            <Chip color={item.tone ?? "default"} size="sm" variant="soft">{item.statusLabel}</Chip>
          ) : null}
        </li>
      ))}
    </ul>
  );
}

export interface InteractiveReplyMessageProps {
  reply: InteractiveReply;
  avatarAlt: string;
  avatarFallback: string;
  title?: string;
  /** The app's resolution of `reply.references`; unknown references are simply left out. */
  references?: readonly InteractiveReference[];
  /** The app's control for `reply.nextAction`, if it offers one. */
  action?: ReactNode;
  /** True while the app cannot take an answer, for example while another reply is on its way. */
  isPending?: boolean;
  /** Returns false when the app did not take the answer, so the question stays open. */
  onAnswer: (keys: string[]) => boolean;
}

/** One assistant reply: its text, an optional tappable question, its references and one next step. */
export function InteractiveReplyMessage({
  reply,
  avatarAlt,
  avatarFallback,
  title,
  references = [],
  action,
  isPending = false,
  onAnswer,
}: InteractiveReplyMessageProps) {
  const [answered, setAnswered] = useState<string[]>([]);

  return (
    <ChatMessage avatarAlt={avatarAlt} avatarFallback={avatarFallback} kind="answer" title={title}>
      <div className="flex flex-col gap-4">
        <p className="whitespace-pre-line">{reply.text}</p>
        {reply.question ? (
          <InteractiveOptionPicker
            answered={answered}
            isDisabled={isPending}
            question={reply.question}
            onAnswer={(keys) => {
              if (onAnswer(keys)) setAnswered(keys);
            }}
          />
        ) : null}
        <InteractiveReferenceChips items={references} />
        <RememberedNotes items={reply.remembered} />
        {action ? <div className="flex flex-wrap gap-3">{action}</div> : null}
      </div>
    </ChatMessage>
  );
}
