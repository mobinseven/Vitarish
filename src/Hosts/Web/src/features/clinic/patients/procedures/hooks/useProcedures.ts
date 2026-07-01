import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { listProcedures, getProcedure, listProcedureEvents, getEvent, deleteEvent, type ProcedureReadDto, type EventReadDto } from "../api/proceduresApi";

const SEA_KEYS = ["clinic", "procedures"] as const;

export function usePatientProcedures(projectId: string) {
  return useQuery({
    queryKey: [...SEA_KEYS, "list", projectId],
    queryFn: () => listProcedures(projectId),
    enabled: !!projectId,
  });
}

export function usePatientProcedureDetail(procedureId: string) {
  return useQuery({
    queryKey: [...SEA_KEYS, "detail", procedureId],
    queryFn: () => getProcedure(procedureId),
    enabled: !!procedureId,
  });
}

export function usePatientProcedureEvents(procedureId: string) {
  return useQuery({
    queryKey: [...SEA_KEYS, "events", procedureId],
    queryFn: () => listProcedureEvents(procedureId),
    enabled: !!procedureId,
  });
}

export function useProcedureEventDetail(eventId: string) {
  return useQuery({
    queryKey: [...SEA_KEYS, "event", eventId],
    queryFn: () => getEvent(eventId),
    enabled: !!eventId,
  });
}

export function useDeleteProcedureEvent() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: deleteEvent,
    onSuccess: () => qc.invalidateQueries({ queryKey: SEA_KEYS }),
  });
}
