"use client";

import { useCallback, useState } from "react";
import { useConversationController } from "@vitarish/intelligence";
import {
  deleteLandingConversation,
  LandingConversationError,
  type LandingConversationOwner,
  submitLandingMessage,
} from "./landing-conversation";

export type LandingMessage = {
  id: number;
  kind: "question" | "answer";
  text: string;
};

export function useLandingConversation(ownerKey: LandingConversationOwner) {
  const [messages, setMessages] = useState<LandingMessage[]>([]);
  const [isResetError, setResetError] = useState(false);
  const run = useCallback(
    async (
      message: string,
      conversationId: string | null,
      signal: AbortSignal,
    ) => {
      const reply = await submitLandingMessage(
        ownerKey,
        message,
        conversationId,
        signal,
      );
      return { conversationId: reply.conversationId, result: reply.message };
    },
    [ownerKey],
  );
  const remove = useCallback(
    (conversationId: string) =>
      deleteLandingConversation(ownerKey, conversationId),
    [ownerKey],
  );
  const controller = useConversationController({
    ownerKey,
    isOpen: true,
    run,
    deleteConversation: remove,
    isNotFound: (error) =>
      error instanceof LandingConversationError && error.code === "not_found",
  });

  const submit = useCallback(
    async (text: string) => {
      const trimmed = text.trim();
      if (!trimmed || controller.isPending) return;

      setResetError(false);
      const id = Date.now();
      setMessages((current) => [
        ...current,
        { id, kind: "question", text: trimmed },
      ]);
      const answer = await controller.submit(trimmed);
      if (answer) {
        setMessages((current) => [
          ...current,
          { id: id + 1, kind: "answer", text: answer },
        ]);
      }
    },
    [controller],
  );

  const retry = useCallback(async () => {
    setResetError(false);
    const answer = await controller.retry();
    if (answer) {
      setMessages((current) => [
        ...current,
        { id: Date.now(), kind: "answer", text: answer },
      ]);
    }
  }, [controller]);

  const reset = useCallback(async () => {
    setResetError(false);
    try {
      await controller.deleteCurrent();
      setMessages([]);
    } catch {
      setResetError(true);
    }
  }, [controller]);

  const cancel = useCallback(() => {
    controller.reset();
    setMessages([]);
    setResetError(false);
  }, [controller]);

  return {
    ...controller,
    isError: controller.isError || isResetError,
    messages,
    submit,
    retry,
    reset,
    cancel,
  };
}
