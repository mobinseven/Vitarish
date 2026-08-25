"use client";

import {
  EmptyStateSurface as ClinicEmptyState,
  LoadingStateSurface as ClinicLoadingState,
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
    <section className="rounded-medium border border-divider p-4">
      <header className="mb-3">
        <h2>
          {t("clinic.patient.advancedSearch.resultsCount", {
            count: String(results.length),
          })}
        </h2>
      </header>
      <div className="flex flex-col gap-1">
        {results.map((patient) => (
          <PatientRow
            key={patient.id}
            patient={patient}
            onSelect={onSelectPatient}
          />
        ))}
      </div>
    </section>
  );
}
