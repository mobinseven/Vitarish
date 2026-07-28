import { useMemo } from "react";
import { useQueries } from "@tanstack/react-query";
import { getPatientDetails } from "../../../api/patients/patientApi";
import type { ReservationReadDto } from "../../../api/appointments/appointmentsApi";
import { patientKeys } from "../../patients/patientKeys";

export function useReservationPartyNames(
  reservations: ReservationReadDto[],
): Map<string, string> {
  const partyIds = [...new Set(
    reservations.flatMap((reservation) => reservation.reservingPartyId ? [reservation.reservingPartyId] : []),
  )];
  const queries = useQueries({
    queries: partyIds.map((patientId) => ({
      queryKey: patientKeys.detail(patientId),
      queryFn: () => getPatientDetails(patientId),
      staleTime: 5 * 60_000,
    })),
  });

  return useMemo(() => {
    const names = new Map<string, string>();
    for (const [index, query] of queries.entries()) {
      const patient = query.data;
      const patientId = partyIds[index];
      if (!patient || !patientId) continue;

      const name = patient.name ?? [patient.firstName, patient.lastName]
        .filter(Boolean)
        .join(" ");
      if (name) names.set(patientId, name);
    }
    return names;
  }, [partyIds, queries]);
}
