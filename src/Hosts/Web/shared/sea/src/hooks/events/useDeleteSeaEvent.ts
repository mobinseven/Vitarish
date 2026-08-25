import { useMutation, useQueryClient } from "@tanstack/react-query";
import { seaEventKeys } from "./seaEventKeys";
import { deleteSeaEvent } from "../../api/events/seaEventsApi";
import { seaProcedureKeys } from "../procedures/seaProcedureKeys";

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
