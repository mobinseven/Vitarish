import { useCallback, useState } from "react";
import { lookupPatientByIdentificationCode } from "../api/patientApi";
import type { IdentificationLookupState } from "../types";

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
      const message =
        error instanceof Error ? error.message : "Lookup failed";
      if (message.includes("404") || message.includes("Not Found")) {
        setState({ status: "not-found", patient: null, error: null });
      } else {
        setState({ status: "error", patient: null, error: message });
      }
    }
  }, []);

  const reset = useCallback(() => {
    setState(initialState);
  }, []);

  return { state, lookup, reset };
}
