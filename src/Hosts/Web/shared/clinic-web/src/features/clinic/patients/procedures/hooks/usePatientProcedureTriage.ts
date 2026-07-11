import { useQuery } from "@tanstack/react-query";
import { patientProcedureKeys } from "./patientProcedureKeys";
import { listProcedures } from "../api/proceduresApi";
import type { TriageSummary } from "../models/PatientProcedureTriageModels";

export function usePatientProcedureTriage(patientId: string) {
  return useQuery({
    queryKey: patientProcedureKeys.triage(patientId),
    queryFn: async (): Promise<TriageSummary> => {
      const procedures = await listProcedures(patientId);
      let incompleteProcedures = 0;
      let totalEvents = 0;
      let committedEvents = 0;
      let newItems = 0;

      for (const p of procedures) {
        totalEvents += p.stats?.events ?? 0;
        committedEvents += p.stats?.committedEvents ?? 0;
        newItems += p.stats?.newItems ?? 0;
        if ((p.stats?.events ?? 0) > (p.stats?.committedEvents ?? 0)) {
          incompleteProcedures++;
        }
      }

      return {
        incompleteProcedures,
        pendingReminders: 0,
        newEvidence: newItems,
        totalProcedures: procedures.length,
        totalEvents,
        committedEvents,
      };
    },
    enabled: !!patientId,
  });
}
