import { useQuery } from "@tanstack/react-query";
import { listPatients } from "../../api/patients/patientApi";
import { patientKeys } from "./patientKeys";
import type { PatientSearchCriteria } from "../../models/patients/patientSearchModels";

export function usePatientList(criteria: PatientSearchCriteria) {
  return useQuery({
    queryKey: patientKeys.list(criteria),
    queryFn: () =>
      listPatients({
        search: criteria.search,
        skip: criteria.skip ?? 0,
        take: criteria.take ?? 20,
        sortBy: criteria.sortBy ?? undefined,
        descending: criteria.descending ?? false,
      }),
  });
}
