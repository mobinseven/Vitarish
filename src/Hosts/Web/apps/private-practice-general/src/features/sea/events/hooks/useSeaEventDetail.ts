import { useQuery } from "@tanstack/react-query";
import { seaEventKeys } from "./seaEventKeys";
import { getSeaEvent } from "../api/seaEventsApi";

export function useSeaEventDetail(eventId: string) {
  return useQuery({
    queryKey: seaEventKeys.detail(eventId),
    queryFn: () => getSeaEvent(eventId),
    enabled: !!eventId,
  });
}
