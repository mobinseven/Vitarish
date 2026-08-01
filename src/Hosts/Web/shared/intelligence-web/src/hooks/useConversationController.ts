"use client";

import { useCallback, useEffect, useRef, useState } from "react";

export interface ConversationRunResult<TResult> {
  conversationId: string;
  result: TResult;
}

export interface UseConversationControllerOptions<TResult> {
  ownerKey: string;
  isOpen: boolean;
  run: (message: string, conversationId: string | null, signal: AbortSignal) => Promise<ConversationRunResult<TResult>>;
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
  const activeRequest = useRef<AbortController | null>(null);
  const [isPending, setPending] = useState(false);
  const [isError, setError] = useState(false);

  const reset = useCallback(() => {
    activeRequest.current?.abort();
    activeRequest.current = null;
    const current = conversationId.current;
    instance.current += 1;
    conversationId.current = null;
    lastMessage.current = null;
    setPending(false);
    setError(false);
    if (current) void deleteConversation(current).catch(() => undefined);
  }, [deleteConversation]);

  useEffect(() => {
    reset();
  }, [ownerKey, reset]);

  const unmountTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  useEffect(() => {
    if (unmountTimer.current) clearTimeout(unmountTimer.current);
    return () => {
      unmountTimer.current = setTimeout(() => {
        activeRequest.current?.abort();
        instance.current += 1;
      }, 0);
    };
  }, []);

  useEffect(() => {
    if (isOpen !== wasOpen.current) reset();
    wasOpen.current = isOpen;
  }, [isOpen, reset]);

  const execute = useCallback(async (message: string) => {
    const submittedInstance = instance.current;
    const currentConversationId = conversationId.current;
    const controller = new AbortController();
    activeRequest.current = controller;
    lastMessage.current = message;
    setPending(true);
    setError(false);
    try {
      let runResult: ConversationRunResult<TResult>;
      try {
        runResult = await run(message, currentConversationId, controller.signal);
      } catch (error) {
        if (!currentConversationId || !isNotFound(error)) throw error;
        conversationId.current = null;
        runResult = await run(message, null, controller.signal);
      }
      if (instance.current !== submittedInstance) {
        void deleteConversation(runResult.conversationId).catch(() => undefined);
        return undefined;
      }
      conversationId.current = runResult.conversationId;
      return runResult.result;
    } catch {
      if (instance.current === submittedInstance) setError(true);
      return undefined;
    } finally {
      if (instance.current === submittedInstance) setPending(false);
      if (activeRequest.current === controller) activeRequest.current = null;
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
