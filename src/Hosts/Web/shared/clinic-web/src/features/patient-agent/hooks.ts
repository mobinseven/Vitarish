"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useCallback, useEffect, useRef, useState } from "react";
import {
  intelligenceRunAgentConversation,
  useConversationController,
  type ConversationController,
} from "@vitarish/intelligence-web";
import {
  clinicConfirmAgentAction,
  clinicConfirmPatientAgentAction,
  clinicDeletePatientAgentConversation,
  clinicGetAgentAction,
  clinicGetPatientAgentAction,
  clinicRejectAgentAction,
  clinicRejectPatientAgentAction,
  clinicDeleteOperationsAgentConversation,
} from "../../api/generated/clinic/clinic";
import type {
  PatientAgentActionDetailsResponse,
  PatientAgentActionResponse,
  PatientAgentMessageResponse,
  RunPatientAgentResponse,
} from "../../api/generated/clinic/model";

export type PatientAgentAction = PatientAgentActionDetailsResponse;
export type PatientAgentMessageResult = PatientAgentMessageResponse;
export type ClinicAgentChatMessage =
  | PatientAgentMessageResult
  | { kind: "UserMessage"; content: string };

const patientAgentActionKey = (patientId: string, actionId: string) =>
  ["clinic", "patient-agent", patientId, "actions", actionId] as const;
const operationsAgentActionKey = (actionId: string) =>
  ["clinic", "operations-agent", "actions", actionId] as const;

export function usePatientAgentConversation(patientId: string, isOpen: boolean) {
  const runTurn = useCallback(
    async (message: string, conversationId: string | null, signal: AbortSignal) => {
      const response = await intelligenceRunAgentConversation({
        agentKey: "clinic.patient",
        subjectKind: "patient",
        subjectId: patientId,
        message,
        conversationId,
      }, undefined, signal);
      const result = response.result as unknown as RunPatientAgentResponse;
      return { conversationId: response.conversationId, result };
    },
    [patientId],
  );
  const deleteConversation = useCallback(
    (conversationId: string) =>
      clinicDeletePatientAgentConversation(patientId, conversationId),
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

export function useOperationsAgentConversation(isOpen: boolean) {
  const runTurn = useCallback(
    async (
      message: string,
      conversationId: string | null,
      signal: AbortSignal,
    ) => {
      const response = await intelligenceRunAgentConversation(
        {
          agentKey: "clinic.operations",
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
        result: response.result as unknown as RunPatientAgentResponse,
      };
    },
    [],
  );
  return useConversationController({
    ownerKey: "clinic.operations",
    isOpen,
    run: runTurn,
    deleteConversation: clinicDeleteOperationsAgentConversation,
    isNotFound: (error) =>
      typeof error === "object" &&
      error !== null &&
      "status" in error &&
      error.status === 404,
  });
}

export function useClinicAgentConversationSurface(
  conversation: ConversationController<RunPatientAgentResponse>,
  scopeKey: string,
) {
  const [messages, setMessages] = useState<ClinicAgentChatMessage[]>([]);
  const [actionIds, setActionIds] = useState<string[]>([]);
  const [lastMessage, setLastMessage] = useState("");
  const activeScope = useRef(scopeKey);

  useEffect(() => {
    activeScope.current = scopeKey;
    setMessages([]);
    setActionIds([]);
    setLastMessage("");
  }, [scopeKey]);

  const appendResult = useCallback(
    (result: RunPatientAgentResponse | undefined, submittedScope: string) => {
      if (!result || activeScope.current !== submittedScope) return;
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
      }
    },
    [],
  );

  const submit = useCallback(
    async (message: string) => {
      const submittedScope = activeScope.current;
      setLastMessage(message);
      setMessages((current) => [
        ...current,
        { kind: "UserMessage", content: message },
      ]);
      appendResult(await conversation.submit(message), submittedScope);
    },
    [appendResult, conversation],
  );

  const retry = useCallback(
    async () =>
      appendResult(await conversation.retry(), activeScope.current),
    [appendResult, conversation],
  );
  const deleteCurrent = useCallback(async () => {
    await conversation.deleteCurrent();
    setMessages([]);
    setActionIds([]);
    setLastMessage("");
  }, [conversation]);

  return { messages, actionIds, lastMessage, submit, retry, deleteCurrent };
}

export function usePatientAgentAction(patientId: string, actionId: string) {
  return useQuery({
    queryKey: patientAgentActionKey(patientId, actionId),
    queryFn: ({ signal }) => clinicGetPatientAgentAction(patientId, actionId, undefined, signal),
    enabled: Boolean(patientId && actionId),
    staleTime: 0,
  });
}

export function useOperationsAgentAction(actionId: string) {
  return useQuery({
    queryKey: operationsAgentActionKey(actionId),
    queryFn: ({ signal }) => clinicGetAgentAction(actionId, undefined, signal),
    enabled: Boolean(actionId),
    staleTime: 0,
  });
}

export function useConfirmPatientAgentAction(patientId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (actionId: string) =>
      clinicConfirmPatientAgentAction(patientId, actionId, { actionId }),
    onSettled: (_data, _error, actionId) =>
      queryClient.invalidateQueries({ queryKey: patientAgentActionKey(patientId, actionId) }),
  });
}

export function useRejectPatientAgentAction(patientId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (actionId: string) =>
      clinicRejectPatientAgentAction(patientId, actionId, { actionId }),
    onSettled: (_data, _error, actionId) =>
      queryClient.invalidateQueries({ queryKey: patientAgentActionKey(patientId, actionId) }),
  });
}

export function useConfirmOperationsAgentAction() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (actionId: string) => clinicConfirmAgentAction(actionId),
    onSettled: (_data, _error, actionId) =>
      queryClient.invalidateQueries({ queryKey: operationsAgentActionKey(actionId) }),
  });
}

export function useRejectOperationsAgentAction() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (actionId: string) => clinicRejectAgentAction(actionId, { actionId }),
    onSettled: (_data, _error, actionId) =>
      queryClient.invalidateQueries({ queryKey: operationsAgentActionKey(actionId) }),
  });
}

export type { PatientAgentActionResponse, RunPatientAgentResponse };
