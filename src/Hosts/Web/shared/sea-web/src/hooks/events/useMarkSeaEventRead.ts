import { useEffect, useRef } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { markSeaEventRead } from "../../api/events/seaEventsApi";
import { seaEventKeys } from "./seaEventKeys";

export function useMarkSeaEventRead() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => markSeaEventRead(id),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: seaEventKeys.all }),
  });
}

export function useMarkRenderedSeaEventsRead(
  renderedEventIds: readonly string[],
) {
  const receiptMutation = useMarkSeaEventRead();
  const attemptedEventIds = useRef(new Set<string>());

  useEffect(() => {
    for (const eventId of renderedEventIds) {
      if (!eventId || attemptedEventIds.current.has(eventId)) continue;

      attemptedEventIds.current.add(eventId);
      receiptMutation.mutate(eventId);
    }
  }, [receiptMutation.mutate, renderedEventIds]);
}
