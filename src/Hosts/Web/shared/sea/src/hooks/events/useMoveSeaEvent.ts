import { useMutation, useQueryClient } from "@tanstack/react-query";
import { seaEventKeys } from "./seaEventKeys";
import { moveSeaEventUp, moveSeaEventDown } from "../../api/events/seaEventsApi";

export function useMoveSeaEventUp() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => moveSeaEventUp(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: seaEventKeys.all }),
  });
}

export function useMoveSeaEventDown() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => moveSeaEventDown(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: seaEventKeys.all }),
  });
}
