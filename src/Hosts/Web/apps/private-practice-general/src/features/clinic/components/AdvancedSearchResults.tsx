"use client";

import { Widget } from "@heroui-pro/react";
import { ClinicEmptyState, ClinicLoadingState } from "./ClinicStateSurface";
import { PatientRow } from "./PatientRow";
import type { PatientViewDto } from "../types";
import { useLocalizedText } from "@/localization";

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
    return <ClinicLoadingState label={t("clinic.patient.advancedSearch.searching")} />;
  }

  if (results.length === 0) {
    return <ClinicEmptyState title={t("clinic.patient.advancedSearch.noResults")} />;
  }

  return (
    <Widget>
      <Widget.Header>
        <Widget.Title>
          {t("clinic.patient.advancedSearch.resultsCount", { count: String(results.length) })}
        </Widget.Title>
      </Widget.Header>
      <Widget.Content className="flex flex-col gap-1">
        {results.map((patient) => (
          <PatientRow
            key={patient.id}
            patient={patient}
            onSelect={onSelectPatient}
          />
        ))}
      </Widget.Content>
    </Widget>
  );
}
