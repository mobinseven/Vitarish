import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { patientProcedureKeys } from "./patientProcedureKeys";
import { listEventReadRoles, listEventWriteRoles, assignEventRole, removeEventRole, listAllRoles } from "@vitarish/sea-web";
import type { AccessSummary } from "../models/PatientProcedureAccessModels";

export function usePatientProcedureEventAccess(eventId: string) {
  return useQuery({
    queryKey: patientProcedureKeys.access(eventId),
    queryFn: async (): Promise<AccessSummary> => {
      const [readRoles, writeRoles] = await Promise.all([
        listEventReadRoles(eventId),
        listEventWriteRoles(eventId),
      ]);
      return { eventId, readRoles, writeRoles };
    },
    enabled: !!eventId,
  });
}

export function usePatientProcedureAllRoles() {
  return useQuery({
    queryKey: [...patientProcedureKeys.all, "allRoles"],
    queryFn: listAllRoles,
  });
}

export function useAssignPatientProcedureEventRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: { eventId: string; roleId: string; type: string }) => assignEventRole(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: patientProcedureKeys.all }),
  });
}

export function useRemovePatientProcedureEventRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => removeEventRole(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: patientProcedureKeys.all }),
  });
}
