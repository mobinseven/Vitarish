"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useCallback } from "react";
import { useConversationController } from "@vitarish/intelligence-web";
import {
  clinicConfirmPatientAgentAction,
  clinicDeletePatientAgentConversation,
  clinicGetPatientAgentAction,
  clinicRejectPatientAgentAction,
  clinicRunPatientAgent,
} from "../../api/generated/clinic/clinic";
import type {
  PatientAgentActionDetailsResponse,
  PatientAgentActionResponse,
  PatientAgentMessageResponse,
  RunPatientAgentResponse,
} from "../../api/generated/clinic/model";

export type PatientAgentAction = PatientAgentActionDetailsResponse;
export type PatientAgentMessageResult = PatientAgentMessageResponse;

const patientAgentActionKey = (patientId: string, actionId: string) =>
  ["clinic", "patient-agent", patientId, "actions", actionId] as const;

export function usePatientAgentConversation(patientId: string, isOpen: boolean) {
  const runTurn = useCallback(
    async (message: string, conversationId: string | null, signal: AbortSignal) => {
      const result = await clinicRunPatientAgent(patientId, {
        message,
        conversationId,
        previousResponseId: null,
      }, undefined, signal);
      return { conversationId: result.conversationId, result };
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

export function usePatientAgentAction(patientId: string, actionId: string) {
  return useQuery({
    queryKey: patientAgentActionKey(patientId, actionId),
    queryFn: () => clinicGetPatientAgentAction(patientId, actionId),
    enabled: Boolean(patientId && actionId),
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

export type { PatientAgentActionResponse, RunPatientAgentResponse };
