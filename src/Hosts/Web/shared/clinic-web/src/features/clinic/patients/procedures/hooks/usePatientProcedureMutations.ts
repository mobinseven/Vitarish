import { useMutation, useQueryClient } from "@tanstack/react-query";
import { patientProcedureKeys } from "./patientProcedureKeys";
import { createProcedure, updateProcedure, deleteProcedure } from "../api/proceduresApi";

export function useCreatePatientProcedure() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: { projectId?: string; name?: string; description?: string; id?: string }) => createProcedure(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: patientProcedureKeys.all }),
  });
}

export function useUpdatePatientProcedure() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: { projectId?: string; name?: string; description?: string; id?: string }) => updateProcedure(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: patientProcedureKeys.all }),
  });
}

export function useDeletePatientProcedure() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteProcedure(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: patientProcedureKeys.all }),
  });
}
