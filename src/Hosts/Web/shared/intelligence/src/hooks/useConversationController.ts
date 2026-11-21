"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { UnknownTurnOutcomeError } from "../api/intelligence/agentConversationTurn";
import type { ProvisionalAnswer } from "../api/intelligence/agentConversationTurn";

/** How often provisional text reaches the screen, so a fast stream repaints in batches rather than per frame. */
const provisionalFlushMilliseconds = 80;

export interface ConversationRunResult<TResult> {
  conversationId: string;
  result: TResult;
}

/** The provisional answer of the turn that is running now; the terminal response replaces it. */
export interface ActiveTurnProvisionalAnswer extends ProvisionalAnswer {
  /** Identifies the turn that produced it, so text can never be shown under another turn. */
  turnId: number;
}

export interface UseConversationControllerOptions<TResult> {
  ownerKey: string;
  isOpen: boolean;
  run: (
    message: string,
    conversationId: string | null,
    signal: AbortSignal,
    /** Reports the provisional answer text of a streamed turn; ignored once the turn is no longer the active one. */
    onProvisionalAnswer: (answer: ProvisionalAnswer) => void,
  ) => Promise<ConversationRunResult<TResult>>;
  deleteConversation: (conversationId: string) => Promise<void>;
  /** Whether a failed delete means the conversation is already gone. It is also the stale-conversation test unless `isStaleConversation` is given. */
  isNotFound?: (error: unknown) => boolean;
  /**
   * Whether a failed run is an unambiguous signal, raised before anything ran, that the resumed conversation is stale.
   * Only then is the turn sent once more as a new conversation.
   */
  isStaleConversation?: (error: unknown) => boolean;
  resetOnClose?: boolean;
  /** A conversation to continue, for example one another surface of the same app started. */
  initialConversationId?: string | null;
  onConversationIdChange?: (conversationId: string | null) => void;
}

export interface ConversationController<TResult> {
  isPending: boolean;
  isError: boolean;
  /**
   * The last turn was accepted but its outcome never arrived (the stream ended early, broke, or was stopped), so its
   * answer may or may not be saved. `retry` does nothing in this state; the person checks the conversation.
   */
  isOutcomeUnknown: boolean;
  /** Text of the answer being written by the running streamed turn; absent otherwise and never authoritative. */
  provisional: ActiveTurnProvisionalAnswer | null;
  submit: (message: string) => Promise<TResult | undefined>;
  retry: () => Promise<TResult | undefined>;
  reset: () => void;
  deleteCurrent: () => Promise<void>;
  /** Stops waiting for the running turn and keeps the conversation; work the server already did stays done. */
  cancel: () => void;
}

export function useConversationController<TResult>({
  ownerKey,
  isOpen,
  run,
  deleteConversation,
  isNotFound = () => false,
  isStaleConversation = isNotFound,
  resetOnClose = true,
  initialConversationId = null,
  onConversationIdChange,
}: UseConversationControllerOptions<TResult>): ConversationController<TResult> {
  const conversationId = useRef<string | null>(initialConversationId);
  const lastMessage = useRef<string | null>(null);
  const instance = useRef(0);
  const wasOpen = useRef(isOpen);
  const activeRequest = useRef<AbortController | null>(null);
  const [isPending, setPending] = useState(false);
  const [isError, setError] = useState(false);
  const [isOutcomeUnknown, setOutcomeUnknownState] = useState(false);
  const outcomeUnknown = useRef(false);
  const [provisional, setProvisional] = useState<ActiveTurnProvisionalAnswer | null>(null);
  const turnSequence = useRef(0);
  const pendingProvisional = useRef<ActiveTurnProvisionalAnswer | null>(null);
  const provisionalTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const setOutcomeUnknown = useCallback((value: boolean) => {
    outcomeUnknown.current = value;
    setOutcomeUnknownState(value);
  }, []);

  const clearProvisional = useCallback(() => {
    if (provisionalTimer.current) clearTimeout(provisionalTimer.current);
    provisionalTimer.current = null;
    pendingProvisional.current = null;
    setProvisional(null);
  }, []);

  const reset = useCallback(() => {
    activeRequest.current?.abort();
    activeRequest.current = null;
    const current = conversationId.current;
    instance.current += 1;
    conversationId.current = null;
    onConversationIdChange?.(null);
    lastMessage.current = null;
    setPending(false);
    setError(false);
    setOutcomeUnknown(false);
    clearProvisional();
    if (current) void deleteConversation(current).catch(() => undefined);
  }, [clearProvisional, deleteConversation, onConversationIdChange, setOutcomeUnknown]);

  // A new owner starts a new conversation; mounting does not, so a continued conversation survives.
  const resetOwnerKey = useRef(ownerKey);
  useEffect(() => {
    if (resetOwnerKey.current === ownerKey) return;
    resetOwnerKey.current = ownerKey;
    reset();
  }, [ownerKey, reset]);

  useEffect(
    () => () => {
      activeRequest.current?.abort();
      instance.current += 1;
      if (provisionalTimer.current) clearTimeout(provisionalTimer.current);
    },
    [],
  );

  useEffect(() => {
    if (resetOnClose && isOpen !== wasOpen.current) reset();
    wasOpen.current = isOpen;
  }, [isOpen, reset, resetOnClose]);

  const execute = useCallback(
    async (message: string) => {
      if (activeRequest.current) return undefined;

      const submittedInstance = instance.current;
      const currentConversationId = conversationId.current;
      const turnId = (turnSequence.current += 1);
      const controller = new AbortController();
      activeRequest.current = controller;
      lastMessage.current = message;
      setPending(true);
      setError(false);
      setOutcomeUnknown(false);
      clearProvisional();
      const isActiveTurn = () => activeRequest.current === controller && instance.current === submittedInstance;
      const onProvisionalAnswer = (answer: ProvisionalAnswer) => {
        if (!isActiveTurn()) return;
        pendingProvisional.current = { ...answer, turnId };
        provisionalTimer.current ??= setTimeout(() => {
          provisionalTimer.current = null;
          if (isActiveTurn()) setProvisional(pendingProvisional.current);
        }, provisionalFlushMilliseconds);
      };
      try {
        let runResult: ConversationRunResult<TResult>;
        try {
          runResult = await run(
            message,
            currentConversationId,
            controller.signal,
            onProvisionalAnswer,
          );
        } catch (error) {
          if (!currentConversationId || !isStaleConversation(error)) throw error;
          conversationId.current = null;
          clearProvisional();
          runResult = await run(message, null, controller.signal, onProvisionalAnswer);
        }
        if (instance.current !== submittedInstance) {
          void deleteConversation(runResult.conversationId).catch(
            () => undefined,
          );
          return undefined;
        }
        conversationId.current = runResult.conversationId;
        onConversationIdChange?.(runResult.conversationId);
        return runResult.result;
      } catch (error) {
        if (instance.current === submittedInstance) {
          // An accepted turn with no outcome is never an ordinary failure: its answer may be saved already.
          if (error instanceof UnknownTurnOutcomeError) setOutcomeUnknown(true);
          else if (!controller.signal.aborted) setError(true);
        }
        return undefined;
      } finally {
        if (instance.current === submittedInstance) {
          setPending(false);
          clearProvisional();
        }
        if (activeRequest.current === controller) activeRequest.current = null;
      }
    },
    [clearProvisional, deleteConversation, isStaleConversation, onConversationIdChange, run, setOutcomeUnknown],
  );

  const retry = useCallback(
    () =>
      // A turn whose outcome is unknown may already be saved; replaying it could do its work twice.
      lastMessage.current && !outcomeUnknown.current
        ? execute(lastMessage.current)
        : Promise.resolve(undefined),
    [execute],
  );

  const deleteCurrent = useCallback(async () => {
    const current = conversationId.current;
    if (!current) {
      reset();
      return;
    }
    try {
      await deleteConversation(current);
    } catch (error) {
      // A conversation that is already gone (for example expired) counts as deleted.
      if (!isNotFound(error)) throw error;
    }
    conversationId.current = null;
    reset();
  }, [deleteConversation, isNotFound, reset]);

  const cancel = useCallback(() => {
    activeRequest.current?.abort();
  }, []);

  return { isPending, isError, isOutcomeUnknown, provisional, submit: execute, retry, reset, deleteCurrent, cancel };
}
