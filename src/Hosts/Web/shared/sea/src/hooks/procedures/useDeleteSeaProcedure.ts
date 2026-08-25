import { useMutation, useQueryClient } from "@tanstack/react-query";
import { seaProcedureKeys } from "./seaProcedureKeys";
import { deleteSeaProcedure } from "../../api/procedures/seaProceduresApi";
import { seaProjectKeys } from "../projects/seaProjectKeys";

export function useDeleteSeaProcedure() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteSeaProcedure(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: seaProcedureKeys.all });
      qc.invalidateQueries({ queryKey: seaProjectKeys.all });
    },
  });
}
