"use client";

import { useCallback, useEffect, useRef, useState } from "react";

export interface ConversationRunResult<TResult> {
  conversationId: string;
  result: TResult;
}

export interface UseConversationControllerOptions<TResult> {
  ownerKey: string;
  isOpen: boolean;
  run: (message: string, conversationId: string | null) => Promise<ConversationRunResult<TResult>>;
  deleteConversation: (conversationId: string) => Promise<void>;
  isNotFound?: (error: unknown) => boolean;
}

export interface ConversationController<TResult> {
  isPending: boolean;
  isError: boolean;
  submit: (message: string) => Promise<TResult | undefined>;
  retry: () => Promise<TResult | undefined>;
  reset: () => void;
  deleteCurrent: () => Promise<void>;
}

export function useConversationController<TResult>({
  ownerKey,
  isOpen,
  run,
  deleteConversation,
  isNotFound = () => false,
}: UseConversationControllerOptions<TResult>): ConversationController<TResult> {
  const conversationId = useRef<string | null>(null);
  const lastMessage = useRef<string | null>(null);
  const instance = useRef(0);
  const wasOpen = useRef(false);
  const [isPending, setPending] = useState(false);
  const [isError, setError] = useState(false);

  const reset = useCallback(() => {
    instance.current += 1;
    conversationId.current = null;
    lastMessage.current = null;
    setPending(false);
    setError(false);
  }, []);

  useEffect(() => {
    reset();
  }, [ownerKey, reset]);

  useEffect(() => {
    if (isOpen !== wasOpen.current) reset();
    wasOpen.current = isOpen;
  }, [isOpen, reset]);

  const execute = useCallback(async (message: string) => {
    const submittedInstance = instance.current;
    const currentConversationId = conversationId.current;
    lastMessage.current = message;
    setPending(true);
    setError(false);
    try {
      let runResult: ConversationRunResult<TResult>;
      try {
        runResult = await run(message, currentConversationId);
      } catch (error) {
        if (!currentConversationId || !isNotFound(error)) throw error;
        conversationId.current = null;
        runResult = await run(message, null);
      }
      if (instance.current !== submittedInstance) return undefined;
      conversationId.current = runResult.conversationId;
      return runResult.result;
    } catch {
      if (instance.current === submittedInstance) setError(true);
      return undefined;
    } finally {
      if (instance.current === submittedInstance) setPending(false);
    }
  }, [isNotFound, run]);

  const retry = useCallback(
    () => lastMessage.current ? execute(lastMessage.current) : Promise.resolve(undefined),
    [execute],
  );

  const deleteCurrent = useCallback(async () => {
    const current = conversationId.current;
    if (!current) {
      reset();
      return;
    }
    await deleteConversation(current);
    reset();
  }, [deleteConversation, reset]);

  return { isPending, isError, submit: execute, retry, reset, deleteCurrent };
}
