import { useMutation } from "@tanstack/react-query";
import { searchPatients } from "../api/patientApi";
import type { AdvancedSearchCriteria } from "../types";

export function usePatientSearch() {
  return useMutation({
    mutationFn: (criteria: AdvancedSearchCriteria) =>
      searchPatients({
        search: [criteria.name, criteria.identificationCode, criteria.dateOfBirth, criteria.phone, criteria.email]
          .filter(Boolean)
          .join(" ") || null,
      }),
  });
}
