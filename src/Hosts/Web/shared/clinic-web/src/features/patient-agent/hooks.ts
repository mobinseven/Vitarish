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
  PatientAgentCandidateResponse,
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

type CandidatePresentation = {
  candidates: PatientAgentCandidateResponse[];
  runId: string;
  message: string;
};

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
    (result: RunPatientAgentResponse | undefined, submittedScope: string, submittedMessage: string, requestId: number) => {
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
