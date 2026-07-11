import { useMutation, useQueryClient } from "@tanstack/react-query";
import { seaEventKeys } from "./seaEventKeys";
import { updateSeaEvent, type SeaEventWriteDto } from "../api/seaEventsApi";

export function useUpdateSeaEvent() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: SeaEventWriteDto) => updateSeaEvent(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: seaEventKeys.all }),
  });
}
