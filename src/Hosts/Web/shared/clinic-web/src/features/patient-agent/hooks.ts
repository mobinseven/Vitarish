"use client";

import { useMutation, useQuery } from "@tanstack/react-query";
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

export function usePatientAgent() {
  return useMutation({
    mutationFn: ({ patientId, message, conversationId }: { patientId: string; message: string; conversationId: string | null }) =>
      clinicRunPatientAgent(patientId, { message, conversationId }),
  });
}

export function usePatientAgentConversation(patientId: string) {
  const run = usePatientAgent();
  return {
    ...run,
    submit: (message: string, conversationId: string | null) =>
      run.mutateAsync({ patientId, message, conversationId }),
  };
}

export function usePatientAgentAction(patientId: string, actionId: string) {
  return useQuery({
    queryKey: ["clinic", "patient-agent", patientId, "actions", actionId],
    queryFn: () => clinicGetPatientAgentAction(patientId, actionId),
    enabled: Boolean(patientId && actionId),
    staleTime: 0,
  });
}

export function useConfirmPatientAgentAction(patientId: string) {
  return useMutation({
    mutationFn: (actionId: string) =>
      clinicConfirmPatientAgentAction(patientId, actionId, { actionId }),
  });
}

export function useRejectPatientAgentAction(patientId: string) {
  return useMutation({
    mutationFn: (actionId: string) =>
      clinicRejectPatientAgentAction(patientId, actionId, { actionId }),
  });
}

export type { PatientAgentActionResponse, RunPatientAgentResponse };
