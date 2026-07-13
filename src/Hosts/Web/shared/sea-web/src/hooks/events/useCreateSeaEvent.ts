import { useMutation, useQueryClient } from "@tanstack/react-query";
import { seaEventKeys } from "./seaEventKeys";
import { createSeaEventAfter, type SeaEventWriteDto } from "../../api/events/seaEventsApi";
import { seaProcedureKeys } from "../procedures/seaProcedureKeys";

export function useCreateSeaEvent() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ previousEventId, data }: { previousEventId?: string; data: SeaEventWriteDto }) =>
      createSeaEventAfter(previousEventId, data),
    onSuccess: (_result, variables) => {
      qc.invalidateQueries({ queryKey: seaEventKeys.all });
      if (variables.data.procedureId) {
        qc.invalidateQueries({ queryKey: seaProcedureKeys.detail(variables.data.procedureId) });
      }
    },
  });
}
