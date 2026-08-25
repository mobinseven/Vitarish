import { useInfiniteQuery } from "@tanstack/react-query";
import { listPatients } from "../../api/patients/patientApi";
import { patientKeys } from "./patientKeys";
import type { PatientSearchCriteria } from "../../models/patients/patientSearchModels";

export function usePatientList(criteria: PatientSearchCriteria) {
  const pageSize = 100;

  return useInfiniteQuery({
    queryKey: patientKeys.list(criteria),
    initialPageParam: 0,
    queryFn: ({ pageParam }) =>
      listPatients({
        search: criteria.search,
        skip: pageParam,
        take: pageSize,
        sortBy: criteria.sortBy ?? undefined,
        descending: criteria.descending ?? false,
      }),
    getNextPageParam: (lastPage, pages) =>
      lastPage.length === pageSize ? pages.length * pageSize : undefined,
  });
}
