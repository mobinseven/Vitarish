"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useCallback, useEffect, useRef, useState } from "react";
import {
  emptyGuidedSurface,
  intelligenceRunAgentConversation,
  toGuidedSurface,
  useConversationController,
  type ConversationController,
  type GuidedSurface,
  type GuidedSurfaceActionGroup,
} from "@vitarish/intelligence";
import {
  clinicConfirmAssistantAction,
  clinicConfirmPatientAssistantAction,
  clinicDeletePatientAssistantConversation,
  clinicGetAssistantAction,
  clinicGetPatientAssistantAction,
  clinicRejectAssistantAction,
  clinicRejectPatientAssistantAction,
  clinicDeleteOperationsAssistantConversation,
  clinicReviseAssistantAction,
  getClinicGetSetupProgressQueryKey,
} from "../../api/generated/clinic/clinic";
import type {
  ClinicAssistantCandidateResponse,
  ClinicAssistantActionDetailsResponse,
  ClinicAssistantActionResponse,
  ClinicAssistantMessageResponse,
  RunClinicAssistantResponse,
} from "../../api/generated/clinic/model";

export type AssistantAction = ClinicAssistantActionDetailsResponse;
/** A Clinic assistant reply with its guided-surface blocks (only the operations assistant sends them). */
export type ClinicAssistantReply = RunClinicAssistantResponse & { guidedSurface?: GuidedSurface };
export type AssistantMessageResult = ClinicAssistantMessageResponse;
export type ClinicAssistantChatMessage =
  | AssistantMessageResult
  | { kind: "UserMessage"; content: string };

type CandidatePresentation = {
  candidates: ClinicAssistantCandidateResponse[];
  runId: string;
  message: string;
};

const clinicAssistantActionKey = (patientId: string, actionId: string) =>
  ["clinic", "clinic-assistant", patientId, "actions", actionId] as const;
const operationsAssistantActionKey = (actionId: string) =>
  ["clinic", "operations-agent", "actions", actionId] as const;
/** The query key one operations action is cached under, shared by single and grouped reviews. */
export const operationsAssistantActionQueryKey = operationsAssistantActionKey;

export function useAssistantConversation(patientId: string, isOpen: boolean) {
  const runTurn = useCallback(
    async (message: string, conversationId: string | null, signal: AbortSignal) => {
      const response = await intelligenceRunAgentConversation({
        agentKey: "clinic.assistant.patient",
        subjectKind: "patient",
        subjectId: patientId,
        message,
        conversationId,
      }, undefined, signal);
      const result = response.result as unknown as RunClinicAssistantResponse;
      return { conversationId: response.conversationId, result };
    },
    [patientId],
  );
  const deleteConversation = useCallback(
    (conversationId: string) =>
      clinicDeletePatientAssistantConversation(patientId, conversationId),
    [patientId],
  );
  return useConversationController({
    ownerKey: patientId,
    isOpen,
    run: runTurn,
    deleteConversation,
    isNotFound: (error) =>
      typeof error === "object" &&
      error !== null &&
      "status" in error &&
      error.status === 404,
  });
}

export function useOperationsAssistantConversation(isOpen: boolean) {
  const runTurn = useCallback(
    async (
      message: string,
      conversationId: string | null,
      signal: AbortSignal,
    ) => {
      const response = await intelligenceRunAgentConversation(
        {
          agentKey: "clinic.assistant.operations",
          subjectKind: "practice",
          subjectId: "practice",
          message,
          conversationId,
        },
        undefined,
        signal,
      );
      const result: ClinicAssistantReply = {
        ...(response.result as unknown as RunClinicAssistantResponse),
        guidedSurface: toGuidedSurface(response.blocks),
      };
      return { conversationId: response.conversationId, result };
    },
    [],
  );
  const [initialConversationId] = useState(readOperationsConversationId);
  return useConversationController({
    ownerKey: "clinic.assistant.operations",
    isOpen,
    run: runTurn,
    deleteConversation: clinicDeleteOperationsAssistantConversation,
    // One operations conversation per browser session, shared by the setup home and the side panel; closing a
    // panel keeps it, and "start a new conversation" deletes it everywhere.
    resetOnClose: false,
    initialConversationId,
    onConversationIdChange: writeOperationsConversationId,
    isNotFound: (error) =>
      typeof error === "object" &&
      error !== null &&
      "status" in error &&
      error.status === 404,
  });
}

/** Where the browser session remembers its operations conversation; the server still owns who may continue it. */
const operationsConversationStorageKey = "vitarish.clinic.operationsAssistant.conversationId";

function readOperationsConversationId(): string | null {
  try {
    return typeof window === "undefined" ? null : window.sessionStorage.getItem(operationsConversationStorageKey);
  } catch {
    return null;
  }
}

function writeOperationsConversationId(conversationId: string | null) {
  try {
    if (typeof window === "undefined") return;
    if (conversationId) window.sessionStorage.setItem(operationsConversationStorageKey, conversationId);
    else window.sessionStorage.removeItem(operationsConversationStorageKey);
  } catch {
    // Without storage, each surface keeps its own conversation, as before.
  }
}

export function useClinicAssistantConversationSurface(
  conversation: ConversationController<ClinicAssistantReply>,
  scopeKey: string,
) {
  const [messages, setMessages] = useState<ClinicAssistantChatMessage[]>([]);
  const [actionIds, setActionIds] = useState<string[]>([]);
  const [actionGroups, setActionGroups] = useState<GuidedSurfaceActionGroup[]>([]);
  const [surface, setSurface] = useState<GuidedSurface>(emptyGuidedSurface);
  const [lastMessage, setLastMessage] = useState("");
  const [candidatePresentation, setCandidatePresentation] = useState<CandidatePresentation | null>(null);
  const activeScope = useRef(scopeKey);
  const requestSequence = useRef(0);

  useEffect(() => {
    activeScope.current = scopeKey;
    requestSequence.current += 1;
    setMessages([]);
    setActionIds([]);
    setActionGroups([]);
    setSurface(emptyGuidedSurface);
    setLastMessage("");
    setCandidatePresentation(null);
  }, [scopeKey]);

  const appendResult = useCallback(
    (result: ClinicAssistantReply | undefined, submittedScope: string, submittedMessage: string, requestId: number) => {
      if (!result || activeScope.current !== submittedScope || requestSequence.current !== requestId) return;
      const hasFailure = result.messages.some(({ kind }) => kind === "Failure");
      setMessages((current) => [
        ...current,
        ...(hasFailure
          ? result.messages.filter(({ kind }) => kind === "Failure")
          : result.messages),
      ]);
      setSurface(!hasFailure && result.guidedSurface ? result.guidedSurface : emptyGuidedSurface);
      if (!hasFailure) {
        // Actions grouped by the reply's blocks; any action a block does not group is shown on its own.
        const grouped = new Set(result.guidedSurface?.actionGroups.flatMap(({ actionIds: ids }) => ids) ?? []);
        setActionGroups((current) => [
          ...current,
          ...(result.guidedSurface?.actionGroups ?? []),
          ...result.actions
            .filter(({ id }) => !grouped.has(id))
            .map(({ id }) => ({ agentRunId: result.agentRunId, actionIds: [id] })),
        ]);
        setActionIds((current) => [
          ...current,
          ...result.actions
            .map(({ id }) => id)
            .filter((id) => !current.includes(id)),
        ]);
        setCandidatePresentation(result.candidates?.length
          ? { candidates: result.candidates, runId: result.agentRunId, message: submittedMessage }
          : null);
      }
    },
    [],
  );

  const submit = useCallback(
    async (message: string) => {
      const submittedScope = activeScope.current;
      const requestId = requestSequence.current + 1;
      requestSequence.current = requestId;
      setCandidatePresentation(null);
      setLastMessage(message);
      setMessages((current) => [
        ...current,
        { kind: "UserMessage", content: message },
      ]);
      appendResult(await conversation.submit(message), submittedScope, message, requestId);
    },
    [appendResult, conversation],
  );

  const retry = useCallback(async () => {
    const submittedScope = activeScope.current;
    const message = lastMessage;
    const requestId = requestSequence.current + 1;
    requestSequence.current = requestId;
    setCandidatePresentation(null);
    appendResult(await conversation.retry(), submittedScope, message, requestId);
  }, [appendResult, conversation, lastMessage]);
  /** A revised proposal takes the original's place in its group. */
  const replaceAction = useCallback((originalId: string, replacementId: string) => {
    setActionGroups((current) => current.map((group) => ({
      ...group,
      actionIds: group.actionIds.map((id) => (id === originalId ? replacementId : id)),
    })));
    setActionIds((current) => current.map((id) => (id === originalId ? replacementId : id)));
  }, []);
  const deleteCurrent = useCallback(async () => {
    requestSequence.current += 1;
    await conversation.deleteCurrent();
    setMessages([]);
    setActionIds([]);
    setActionGroups([]);
    setSurface(emptyGuidedSurface);
    setLastMessage("");
    setCandidatePresentation(null);
  }, [conversation]);

  return {
    messages,
    actionIds,
    actionGroups,
    question: surface.question,
    nextAction: surface.nextAction,
    lastMessage,
    candidates: candidatePresentation?.candidates ?? [],
    candidateRunId: candidatePresentation?.runId ?? null,
    candidateMessage: candidatePresentation?.message ?? null,
    submit,
    retry,
    replaceAction,
    deleteCurrent,
  };
}

export function useAssistantAction(patientId: string, actionId: string) {
  return useQuery({
    queryKey: clinicAssistantActionKey(patientId, actionId),
    queryFn: ({ signal }) => clinicGetPatientAssistantAction(patientId, actionId, undefined, signal),
    enabled: Boolean(patientId && actionId),
    staleTime: 0,
  });
}

export function useOperationsAssistantAction(actionId: string) {
  return useQuery({
    queryKey: operationsAssistantActionKey(actionId),
    queryFn: ({ signal }) => clinicGetAssistantAction(actionId, undefined, signal),
    enabled: Boolean(actionId),
    staleTime: 0,
  });
}

export function useConfirmAssistantAction(patientId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (actionId: string) =>
      clinicConfirmPatientAssistantAction(patientId, actionId, { actionId }),
    onSettled: (_data, _error, actionId) =>
      queryClient.invalidateQueries({ queryKey: clinicAssistantActionKey(patientId, actionId) }),
  });
}

export function useRejectAssistantAction(patientId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (actionId: string) =>
      clinicRejectPatientAssistantAction(patientId, actionId, { actionId }),
    onSettled: (_data, _error, actionId) =>
      queryClient.invalidateQueries({ queryKey: clinicAssistantActionKey(patientId, actionId) }),
  });
}

export function useConfirmOperationsAssistantAction() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (actionId: string) => clinicConfirmAssistantAction(actionId),
    onSettled: (_data, _error, actionId) => Promise.all([
      queryClient.invalidateQueries({ queryKey: operationsAssistantActionKey(actionId) }),
      // A confirmed step can complete a setup goal.
      queryClient.invalidateQueries({ queryKey: getClinicGetSetupProgressQueryKey() }),
    ]),
  });
}

/** Replaces a proposal with the owner's edit; resolves with the replacement action. */
export function useReviseOperationsAssistantAction() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ actionId, argumentsJson }: { actionId: string; argumentsJson: Record<string, unknown> }) =>
      clinicReviseAssistantAction(actionId, { actionId, arguments: argumentsJson }),
    onSuccess: (replacement, { actionId }) => {
      queryClient.setQueryData(operationsAssistantActionKey(replacement.actionId), replacement);
      return queryClient.invalidateQueries({ queryKey: operationsAssistantActionKey(actionId) });
    },
  });
}

export function useRejectOperationsAssistantAction() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (actionId: string) => clinicRejectAssistantAction(actionId, { actionId }),
    onSettled: (_data, _error, actionId) =>
      queryClient.invalidateQueries({ queryKey: operationsAssistantActionKey(actionId) }),
  });
}

export type { ClinicAssistantActionResponse, RunClinicAssistantResponse };
