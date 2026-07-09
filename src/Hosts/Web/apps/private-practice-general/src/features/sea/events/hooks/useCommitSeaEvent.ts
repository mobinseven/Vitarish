import { useMutation, useQueryClient } from "@tanstack/react-query";
import { seaEventKeys } from "./seaEventKeys";
import { commitSeaEvent } from "../api/seaEventsApi";

export function useCommitSeaEvent() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => commitSeaEvent(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: seaEventKeys.all }),
  });
}
