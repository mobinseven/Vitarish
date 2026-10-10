"use client";

import { Surface } from "@heroui/react";
import {
  EmptyStateSurface as ClinicEmptyState,
  LoadingStateSurface as ClinicLoadingState,
  HeaderIcon,
} from "@vitarish/ui";
import { PatientRow } from "./PatientRow";
import type { PatientViewDto } from "../../../models/patients/patientSearchModels";
import { useLocalizedText } from "../../../localization/index";

interface AdvancedSearchResultsProps {
  results: PatientViewDto[];
  isLoading: boolean;
  onSelectPatient: (patientId: string) => void;
}

export function AdvancedSearchResults({
  results,
  isLoading,
  onSelectPatient,
}: AdvancedSearchResultsProps) {
  const { t } = useLocalizedText();

  if (isLoading) {
    return (
      <ClinicLoadingState
        label={t("clinic.patient.advancedSearch.searching")}
      />
    );
  }

  if (results.length === 0) {
    return (
      <ClinicEmptyState title={t("clinic.patient.advancedSearch.noResults")} />
    );
  }

  return (
    <Surface className="flex flex-col gap-6 rounded-3xl p-6">
      <div className="flex flex-col items-start gap-1">
        <div className="flex items-center gap-3">
          <HeaderIcon icon="list" />
          <div className="min-w-0">
            <h2 className="text-lg font-semibold text-foreground">
              {t("clinic.patient.advancedSearch.resultsCount", {
                count: String(results.length),
              })}
            </h2>
          </div>
        </div>
      </div>
      <div className="flex flex-col gap-1">
        {results.map((patient) => (
          <PatientRow
            key={patient.id}
            patient={patient}
            onSelect={onSelectPatient}
          />
        ))}
      </div>
    </Surface>
  );
}
