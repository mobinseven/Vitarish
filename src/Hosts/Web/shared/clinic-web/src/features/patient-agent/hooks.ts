"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  clinicConfirmPatientAgentAction,
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

export function usePatientAgent() {
  return useMutation({
    mutationFn: ({ patientId, message, conversationId, previousResponseId }: { patientId: string; message: string; conversationId: string | null; previousResponseId: string | null }) =>
      clinicRunPatientAgent(patientId, { message, conversationId, previousResponseId }),
  });
}

export function usePatientAgentConversation(patientId: string) {
  const run = usePatientAgent();
  return {
    ...run,
    submit: (message: string, conversationId: string | null, previousResponseId: string | null = null) =>
      run.mutateAsync({ patientId, message, conversationId, previousResponseId }),
  };
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
