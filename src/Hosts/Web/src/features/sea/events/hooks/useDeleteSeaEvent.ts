import { useMutation, useQueryClient } from "@tanstack/react-query";
import { seaEventKeys } from "./seaEventKeys";
import { deleteSeaEvent } from "../api/seaEventsApi";
import { seaProcedureKeys } from "../../procedures/hooks/seaProcedureKeys";

export function useDeleteSeaEvent() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteSeaEvent(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: seaEventKeys.all });
      qc.invalidateQueries({ queryKey: seaProcedureKeys.all });
    },
  });
}
