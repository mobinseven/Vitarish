import { useCallback, useState } from "react";
import { usePatientIdentificationLookup } from "@/features/clinic/hooks/usePatientIdentificationLookup";
import { usePatientSearch } from "@/features/clinic/hooks/usePatientSearch";
import { useCreatePatientProfile } from "@/features/clinic/patients/profile/hooks/useCreatePatientProfile";
import type { AdvancedSearchCriteria } from "@/features/clinic/types";

export type FindOrCreateStep = "lookup" | "search" | "create" | "selected";

interface FindOrCreateState {
  step: FindOrCreateStep;
  lookupResult: ReturnType<typeof usePatientIdentificationLookup>["state"];
  searchResults: unknown[] | null;
  createdPatient: { id: string } | null;
}

export function usePatientFindOrCreate() {
  const [state, setState] = useState<FindOrCreateState>({
    step: "lookup",
    lookupResult: { status: "idle", patient: null, error: null },
    searchResults: null,
    createdPatient: null,
  });

  const identificationLookup = usePatientIdentificationLookup();
  const advancedSearch = usePatientSearch();
  const createPatient = useCreatePatientProfile();

  const handleLookup = useCallback(
    async (identificationCode: string) => {
      await identificationLookup.lookup(identificationCode);
      if (identificationLookup.state.status === "found") {
        setState((prev) => ({
          ...prev,
          step: "selected",
          lookupResult: identificationLookup.state,
        }));
      }
    },
    [identificationLookup]
  );

  const handleAdvancedSearch = useCallback(
    async (criteria: AdvancedSearchCriteria) => {
      const result = await advancedSearch.mutateAsync(criteria);
      setState((prev) => ({
        ...prev,
        step: "search",
        searchResults: result as unknown[],
      }));
    },
    [advancedSearch]
  );

  const handleSelectPatient = useCallback((patientId: string) => {
    setState((prev) => ({ ...prev, step: "selected" }));
  }, []);

  const handleCreatePatient = useCallback(async () => {
    setState((prev) => ({ ...prev, step: "create" }));
  }, []);

  const handleGoToLookup = useCallback(() => {
    setState((prev) => ({
      ...prev,
      step: "lookup",
      lookupResult: { status: "idle", patient: null, error: null },
      searchResults: null,
    }));
  }, []);

  return {
    state,
    identificationLookup: identificationLookup.state,
    handleLookup,
    handleAdvancedSearch,
    handleSelectPatient,
    handleCreatePatient,
    handleGoToLookup,
    createPatient,
    isPending: advancedSearch.isPending || createPatient.isPending,
  };
}
