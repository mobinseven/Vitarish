import { useCallback, useState } from "react";
import { ApiError } from "@vitarish/web-api-client";
import { usePatientSearch } from "../usePatientSearch";
import { useCreatePatientProfile } from "../profile/useCreatePatientProfile";
import { lookupPatientByIdentificationCode } from "../../../api/patients/patientApi";
import type { AdvancedSearchCriteria } from "../../../models/patients/patientSearchModels";
import type { PatientViewDto } from "../../../api/generated/clinic/model";

export type FindOrCreateStep = "lookup" | "search" | "create" | "selected";

export function usePatientFindOrCreate() {
  const [step, setStep] = useState<FindOrCreateStep>("lookup");
  const [foundPatient, setFoundPatient] = useState<PatientViewDto | null>(null);
  const [searchResults, setSearchResults] = useState<PatientViewDto[] | null>(null);
  const [lookupError, setLookupError] = useState<string | null>(null);
  const [isLooking, setIsLooking] = useState(false);

  const advancedSearch = usePatientSearch();
  const createPatient = useCreatePatientProfile();

  const handleLookup = useCallback(async (identificationCode: string) => {
    if (!identificationCode.trim()) return;
    setIsLooking(true);
    setLookupError(null);
    try {
      const patient = await lookupPatientByIdentificationCode({
        identificationCode: identificationCode.trim(),
      });
      setFoundPatient(patient);
      setStep("selected");
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) {
        setLookupError("not-found");
      } else {
        setLookupError("error");
      }
    } finally {
      setIsLooking(false);
    }
  }, []);

  const handleAdvancedSearch = useCallback(
    async (criteria: AdvancedSearchCriteria) => {
      const result = await advancedSearch.mutateAsync(criteria);
      setSearchResults(result as PatientViewDto[]);
      setStep("search");
    },
    [advancedSearch]
  );

  const handleSelectPatient = useCallback(() => {
    setStep("selected");
  }, []);

  const handleGoToLookup = useCallback(() => {
    setStep("lookup");
    setFoundPatient(null);
    setSearchResults(null);
    setLookupError(null);
  }, []);

  return {
    step,
    foundPatient,
    searchResults,
    lookupError,
    isLooking,
    handleLookup,
    handleAdvancedSearch,
    handleSelectPatient,
    handleGoToLookup,
    createPatient,
    isPending: advancedSearch.isPending || createPatient.isPending,
  };
}
