"use client";

import { useCallback, useEffect, useState } from "react";
import { TextField, Label, Input, Spinner } from "@heroui/react";
import { usePatientIdentificationLookup } from "../hooks/usePatientIdentificationLookup";
import { IdentificationLookupResult } from "./IdentificationLookupResult";

interface IdentificationLookupProps {
  onSelectPatient: (patientId: string) => void;
}

export function IdentificationLookup({
  onSelectPatient,
}: IdentificationLookupProps) {
  const { state, lookup, reset } = usePatientIdentificationLookup();
  const [code, setCode] = useState("");

  useEffect(() => {
    if (!code.trim()) {
      reset();
      return;
    }

    const timer = setTimeout(() => {
      lookup(code);
    }, 500);

    return () => clearTimeout(timer);
  }, [code, lookup, reset]);

  const handleRetry = useCallback(() => {
    if (code.trim()) {
      lookup(code);
    }
  }, [code, lookup]);

  return (
    <div className="flex flex-col gap-4">
      <TextField>
        <Label>Identification Code</Label>
        <Input
          placeholder="Enter identification code..."
          value={code}
          onChange={(e) => setCode(e.target.value)}
        />
      </TextField>

      {state.status === "searching" && (
        <div className="flex items-center gap-2 text-default-500">
          <Spinner size="sm" />
          <span>Looking up patient...</span>
        </div>
      )}

      {(state.status === "found" ||
        state.status === "not-found" ||
        state.status === "error") && (
        <IdentificationLookupResult
          status={state.status}
          patient={state.patient}
          error={state.error}
          onSelect={onSelectPatient}
          onRetry={handleRetry}
        />
      )}
    </div>
  );
}
