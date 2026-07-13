"use client";

import { useCallback, useState } from "react";
import { ApiError } from "@vitarish/web-api-client";
import { lookupPatientByIdentificationCode } from "../../api/patients/patientApi";
import type { IdentificationLookupState } from "../../models/patients/patientSearchModels";

const initialState: IdentificationLookupState = {
  status: "idle",
  patient: null,
  error: null,
};

export function usePatientIdentificationLookup() {
  const [state, setState] = useState<IdentificationLookupState>(initialState);

  const lookup = useCallback(async (identificationCode: string) => {
    if (!identificationCode.trim()) {
      setState(initialState);
      return;
    }

    setState({ status: "searching", patient: null, error: null });

    try {
      const patient = await lookupPatientByIdentificationCode({
        identificationCode: identificationCode.trim(),
      });
      setState({ status: "found", patient, error: null });
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) {
        setState({ status: "not-found", patient: null, error: null });
      } else {
        setState({ status: "error", patient: null, error: "lookup-failed" });
      }
    }
  }, []);

  const reset = useCallback(() => {
    setState(initialState);
  }, []);

  return { state, lookup, reset };
}
