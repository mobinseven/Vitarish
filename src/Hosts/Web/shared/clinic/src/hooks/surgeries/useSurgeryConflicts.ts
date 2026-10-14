import { useEffect, useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { clinicFindSurgeryConflicts } from "../../api/generated/clinic/clinic";
import { mapSurgeryConflict } from "../../models/surgeries/surgeryMappers";
import type { SurgeryConflictProbe } from "../../models/surgeries/surgeryModels";
import { surgeryKeys } from "./surgeryKeys";

const debounceMs = 400;

export function useSurgeryConflicts(probe: SurgeryConflictProbe) {
  const latestProbe = useRef(probe);
  const probeKey = JSON.stringify(probe);
  const [debouncedProbe, setDebouncedProbe] = useState(probe);

  useEffect(() => {
    latestProbe.current = probe;
  });

  useEffect(() => {
    const handle = window.setTimeout(() => {
      setDebouncedProbe(latestProbe.current);
    }, debounceMs);
    return () => window.clearTimeout(handle);
  }, [probeKey]);

  const scheduledAt = debouncedProbe.scheduledAt;

  return useQuery({
    queryKey: surgeryKeys.conflicts(debouncedProbe),
    queryFn: async () => {
      if (!scheduledAt) return [];
      const conflicts = await clinicFindSurgeryConflicts({
        scheduledAt,
        durationMinutes: debouncedProbe.durationMinutes,
        location: debouncedProbe.location,
        teamUserIds: debouncedProbe.teamUserIds,
        excludeSurgeryId: debouncedProbe.excludeSurgeryId,
      });
      return conflicts.map(mapSurgeryConflict);
    },
    enabled: Boolean(scheduledAt),
  });
}
