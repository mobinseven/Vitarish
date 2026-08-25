import { useQuery } from "@tanstack/react-query";
import { seaEventKeys } from "./seaEventKeys";
import { getSeaEventCapabilities } from "../../api/events/seaEventsApi";
import type { SeaEventCapabilities } from "../../models/seaStatModels";

const defaultCapabilities: SeaEventCapabilities = { canRead: false, canWrite: false, isElevated: false };

export function useSeaEventCapabilities(eventId: string) {
  return useQuery({
    queryKey: seaEventKeys.capabilities(eventId),
    queryFn: () => getSeaEventCapabilities(eventId),
    enabled: !!eventId,
    select: (data) => data ?? defaultCapabilities,
  });
}
