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
  const [searchResults, setSearchResults] = useState<PatientViewDto[] | null>(null);
  const [lookupError, setLookupError] = useState<string | null>(null);
  const [createIdentificationCode, setCreateIdentificationCode] = useState("");
  const [isLooking, setIsLooking] = useState(false);

  const advancedSearch = usePatientSearch();
  const createPatient = useCreatePatientProfile();

  const handleLookup = useCallback(async (identificationCode: string): Promise<PatientViewDto | null> => {
    const normalizedCode = identificationCode.trim();
    if (!normalizedCode) return null;
    setIsLooking(true);
    setLookupError(null);
    try {
      const patient = await lookupPatientByIdentificationCode({
        identificationCode: normalizedCode,
      });
      setStep("selected");
      return patient;
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) {
        setCreateIdentificationCode(normalizedCode);
        setStep("create");
      } else {
        setLookupError("error");
      }
      return null;
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
    setSearchResults(null);
    setLookupError(null);
    setCreateIdentificationCode("");
  }, []);

  return {
    step,
    searchResults,
    lookupError,
    createIdentificationCode,
    isLooking,
    handleLookup,
    handleAdvancedSearch,
    handleSelectPatient,
    handleGoToLookup,
    createPatient,
    isPending: advancedSearch.isPending || createPatient.isPending,
  };
}
