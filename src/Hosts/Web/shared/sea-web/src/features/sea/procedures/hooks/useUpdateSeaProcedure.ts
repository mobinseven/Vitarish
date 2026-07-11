import { useMutation, useQueryClient } from "@tanstack/react-query";
import { seaProcedureKeys } from "./seaProcedureKeys";
import { updateSeaProcedure, type SeaProcedureWriteDto } from "../api/seaProceduresApi";

export function useUpdateSeaProcedure() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: SeaProcedureWriteDto) => updateSeaProcedure(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: seaProcedureKeys.all }),
  });
}
