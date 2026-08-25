import { useMutation, useQueryClient } from "@tanstack/react-query";
import { seaEventKeys } from "./seaEventKeys";
import { uncommitSeaEvent } from "../../api/events/seaEventsApi";

export function useUncommitSeaEvent() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => uncommitSeaEvent(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: seaEventKeys.all }),
  });
}
