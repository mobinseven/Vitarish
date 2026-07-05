"use client";

import { useCallback, useEffect, useState } from "react";
import { TextField, Label, Input, Spinner } from "@heroui/react";
import { usePatientIdentificationLookup } from "../hooks/usePatientIdentificationLookup";
import { IdentificationLookupResult } from "./IdentificationLookupResult";
import { useLocalizedText } from "@/localization";

interface IdentificationLookupProps {
  onSelectPatient: (patientId: string) => void;
}

export function IdentificationLookup({
  onSelectPatient,
}: IdentificationLookupProps) {
  const { state, lookup, reset } = usePatientIdentificationLookup();
  const { t } = useLocalizedText();
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
        <Label>{t("clinic.patient.identification.label")}</Label>
        <Input
          aria-label={t("clinic.patient.identification.label")}
          placeholder={t("clinic.patient.identification.placeholder")}
          value={code}
          onChange={(e) => setCode(e.target.value)}
        />
      </TextField>

      {state.status === "searching" && (
        <div className="flex items-center gap-2 text-muted">
          <Spinner size="sm" />
          <span>{t("clinic.patient.identification.lookingUp")}</span>
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
