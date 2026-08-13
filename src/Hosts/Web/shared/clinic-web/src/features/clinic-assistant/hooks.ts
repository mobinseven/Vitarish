"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useCallback, useEffect, useRef, useState } from "react";
import {
  intelligenceRunAgentConversation,
  useConversationController,
  type ConversationController,
} from "@vitarish/intelligence-web";
import {
  clinicConfirmAssistantAction,
  clinicConfirmPatientAssistantAction,
  clinicDeletePatientAssistantConversation,
  clinicGetAssistantAction,
  clinicGetPatientAssistantAction,
  clinicRejectAssistantAction,
  clinicRejectPatientAssistantAction,
  clinicDeleteOperationsAssistantConversation,
} from "../../api/generated/clinic/clinic";
import type {
  ClinicAssistantCandidateResponse,
  ClinicAssistantActionDetailsResponse,
  ClinicAssistantActionResponse,
  ClinicAssistantMessageResponse,
  RunClinicAssistantResponse,
} from "../../api/generated/clinic/model";

export type AssistantAction = ClinicAssistantActionDetailsResponse;
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
      return {
        conversationId: response.conversationId,
        result: response.result as unknown as RunClinicAssistantResponse,
      };
    },
    [],
  );
  return useConversationController({
    ownerKey: "clinic.assistant.operations",
    isOpen,
    run: runTurn,
    deleteConversation: clinicDeleteOperationsAssistantConversation,
    isNotFound: (error) =>
      typeof error === "object" &&
      error !== null &&
      "status" in error &&
      error.status === 404,
  });
}

export function useClinicAssistantConversationSurface(
  conversation: ConversationController<RunClinicAssistantResponse>,
  scopeKey: string,
) {
  const [messages, setMessages] = useState<ClinicAssistantChatMessage[]>([]);
  const [actionIds, setActionIds] = useState<string[]>([]);
  const [lastMessage, setLastMessage] = useState("");
  const [candidatePresentation, setCandidatePresentation] = useState<CandidatePresentation | null>(null);
  const activeScope = useRef(scopeKey);
  const requestSequence = useRef(0);

  useEffect(() => {
    activeScope.current = scopeKey;
    requestSequence.current += 1;
    setMessages([]);
    setActionIds([]);
    setLastMessage("");
    setCandidatePresentation(null);
  }, [scopeKey]);

  const appendResult = useCallback(
    (result: RunClinicAssistantResponse | undefined, submittedScope: string, submittedMessage: string, requestId: number) => {
      if (!result || activeScope.current !== submittedScope || requestSequence.current !== requestId) return;
      const hasFailure = result.messages.some(({ kind }) => kind === "Failure");
      setMessages((current) => [
        ...current,
        ...(hasFailure
          ? result.messages.filter(({ kind }) => kind === "Failure")
          : result.messages),
      ]);
      if (!hasFailure) {
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
  const deleteCurrent = useCallback(async () => {
    requestSequence.current += 1;
    await conversation.deleteCurrent();
    setMessages([]);
    setActionIds([]);
    setLastMessage("");
    setCandidatePresentation(null);
  }, [conversation]);

  return {
    messages,
    actionIds,
    lastMessage,
    candidates: candidatePresentation?.candidates ?? [],
    candidateRunId: candidatePresentation?.runId ?? null,
    candidateMessage: candidatePresentation?.message ?? null,
    submit,
    retry,
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
    onSettled: (_data, _error, actionId) =>
      queryClient.invalidateQueries({ queryKey: operationsAssistantActionKey(actionId) }),
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
