import { useQuery } from "@tanstack/react-query";
import { seaEventKeys } from "./seaEventKeys";
import { listSeaEvents } from "../../api/events/seaEventsApi";

export function useSeaEvents(procedureId: string) {
  return useQuery({
    queryKey: seaEventKeys.list(procedureId),
    queryFn: () => listSeaEvents(procedureId),
    enabled: !!procedureId,
  });
}
