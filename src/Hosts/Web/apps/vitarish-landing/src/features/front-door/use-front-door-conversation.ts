"use client";

import { useCallback } from "react";
import { toInteractiveReply, useConversationController, type InteractiveReply } from "@vitarish/intelligence";
import { ApiError, landingDeleteConversation, landingSubmitConversation } from "@vitarish/platform";

/** The landing API scopes conversations by page; the front door is one page, the home page. */
const ownerKey = "home";

/**
 * Sends the visitor's messages to the landing assistant and returns its interactive replies. The
 * shared controller owns the conversation id, cancellation, retry and server-side deletion.
 */
export function useFrontDoorConversation() {
  const run = useCallback(async (message: string, conversationId: string | null, signal: AbortSignal) => {
    const response = await landingSubmitConversation({ ownerKey, message, conversationId }, undefined, signal);
    const reply: InteractiveReply = response.reply ? toInteractiveReply(response.reply) : toInteractiveReply({ text: response.message });
    return { conversationId: response.conversationId, result: reply };
  }, []);

  const remove = useCallback(
    (conversationId: string) => landingDeleteConversation(conversationId, { ownerKey }),
    [],
  );

  return useConversationController<InteractiveReply>({
    ownerKey,
    isOpen: true,
    run,
    deleteConversation: remove,
    isNotFound: (error) => error instanceof ApiError && error.status === 404,
  });
}
