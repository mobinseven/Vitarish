import { useCallback, useRef, useState } from "react";
import { ApiError } from "@vitarish/web-api-client";
import { usePatientSearch } from "../usePatientSearch";
import { useCreatePatientProfile } from "../profile/useCreatePatientProfile";
import {
  lookupPatientByIdentificationCode,
  searchPatients,
} from "../../../api/patients/patientApi";
import type { AdvancedSearchCriteria } from "../../../models/patients/patientSearchModels";
import type { PatientViewDto } from "../../../api/generated/clinic/model";

export type FindOrCreateStep = "lookup" | "search" | "create" | "selected";

export function usePatientFindOrCreate() {
  const [step, setStep] = useState<FindOrCreateStep>("lookup");
  const [searchResults, setSearchResults] = useState<PatientViewDto[] | null>(null);
  const [lookupError, setLookupError] = useState<string | null>(null);
  const [createIdentificationCode, setCreateIdentificationCode] = useState("");
  const [isLooking, setIsLooking] = useState(false);
  const [autocompleteResults, setAutocompleteResults] = useState<PatientViewDto[]>([]);
  const [isAutocompleting, setIsAutocompleting] = useState(false);
  const autocompleteRequestId = useRef(0);

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

  const handleAutocomplete = useCallback(async (query: string) => {
    const requestId = ++autocompleteRequestId.current;
    const normalizedQuery = query.trim();

    if (!normalizedQuery) {
      setAutocompleteResults([]);
      setIsAutocompleting(false);
      return;
    }

    setIsAutocompleting(true);
    try {
      const results = await searchPatients({ search: normalizedQuery, take: 5 });
      if (requestId === autocompleteRequestId.current) {
        setAutocompleteResults(results as PatientViewDto[]);
      }
    } catch {
      if (requestId === autocompleteRequestId.current) {
        setAutocompleteResults([]);
      }
    } finally {
      if (requestId === autocompleteRequestId.current) {
        setIsAutocompleting(false);
      }
    }
  }, []);

  const handleSelectPatient = useCallback(() => {
    setStep("selected");
  }, []);

  const handleGoToLookup = useCallback(() => {
    setStep("lookup");
    setSearchResults(null);
    setLookupError(null);
    setCreateIdentificationCode("");
    setAutocompleteResults([]);
    setIsAutocompleting(false);
    autocompleteRequestId.current += 1;
  }, []);

  return {
    step,
    searchResults,
    lookupError,
    createIdentificationCode,
    isLooking,
    autocompleteResults,
    isAutocompleting,
    handleLookup,
    handleAutocomplete,
    handleAdvancedSearch,
    handleSelectPatient,
    handleGoToLookup,
    createPatient,
    isPending: advancedSearch.isPending || createPatient.isPending,
  };
}
