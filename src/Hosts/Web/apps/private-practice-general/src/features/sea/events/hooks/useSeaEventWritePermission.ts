import { useQuery } from "@tanstack/react-query";
import { seaEventKeys } from "./seaEventKeys";
import { getSeaEventWritePermission } from "../api/seaEventsApi";

export function useSeaEventWritePermission(eventId: string) {
  return useQuery({
    queryKey: seaEventKeys.writePermission(eventId),
    queryFn: () => getSeaEventWritePermission(eventId),
    enabled: !!eventId,
  });
}
