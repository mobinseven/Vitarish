import { useMutation, useQueryClient } from "@tanstack/react-query";
import { seaProcedureKeys } from "./seaProcedureKeys";
import { createSeaProcedure, type SeaProcedureWriteDto } from "../../api/procedures/seaProceduresApi";
import { seaProjectKeys } from "../projects/seaProjectKeys";

export function useCreateSeaProcedure() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: SeaProcedureWriteDto) => createSeaProcedure(data),
    onSuccess: (_result, variables) => {
      qc.invalidateQueries({ queryKey: seaProcedureKeys.all });
      if (variables.projectId) {
        qc.invalidateQueries({ queryKey: seaProjectKeys.detail(variables.projectId) });
      }
    },
  });
}
