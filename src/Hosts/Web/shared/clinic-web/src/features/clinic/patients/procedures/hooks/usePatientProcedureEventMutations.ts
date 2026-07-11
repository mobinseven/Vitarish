import { useMutation, useQueryClient } from "@tanstack/react-query";
import { patientProcedureKeys } from "./patientProcedureKeys";
import {
  createEvent,
  updateEvent,
  deleteEvent,
  moveEventUp,
  moveEventDown,
  commitEvent,
  uncommitEvent,
} from "../api/proceduresApi";

export function useCreatePatientProcedureEvent() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: { projectId?: string; procedureId?: string; title?: string; description?: string; previousEventId?: string }) => createEvent(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: patientProcedureKeys.all }),
  });
}

export function useUpdatePatientProcedureEvent() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: { projectId?: string; procedureId?: string; title?: string; description?: string; id?: string }) => updateEvent(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: patientProcedureKeys.all }),
  });
}

export function useDeletePatientProcedureEvent() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteEvent(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: patientProcedureKeys.all }),
  });
}

export function useMovePatientProcedureEventUp() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => moveEventUp(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: patientProcedureKeys.all }),
  });
}

export function useMovePatientProcedureEventDown() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => moveEventDown(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: patientProcedureKeys.all }),
  });
}

export function useCommitPatientProcedureEvent() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => commitEvent(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: patientProcedureKeys.all }),
  });
}

export function useUncommitPatientProcedureEvent() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => uncommitEvent(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: patientProcedureKeys.all }),
  });
}
