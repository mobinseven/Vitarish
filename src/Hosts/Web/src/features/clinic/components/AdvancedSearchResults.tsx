"use client";

import { Card, Spinner } from "@heroui/react";
import { PatientRow } from "./PatientRow";
import type { PatientViewDto } from "../types";

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
  if (isLoading) {
    return (
      <Card className="glass-card p-4">
        <div className="flex items-center justify-center gap-2">
          <Spinner size="sm" />
          <span className="text-default-500">Searching...</span>
        </div>
      </Card>
    );
  }

  if (results.length === 0) {
    return (
      <Card className="glass-card p-4">
        <p className="text-center text-default-500">No patients found</p>
      </Card>
    );
  }

  return (
    <Card className="glass-card">
      <Card.Header>
        <h3 className="text-lg font-semibold">
          Results ({results.length})
        </h3>
      </Card.Header>
      <Card.Content className="flex flex-col gap-1">
        {results.map((patient) => (
          <PatientRow
            key={patient.id}
            patient={patient}
            onSelect={onSelectPatient}
          />
        ))}
      </Card.Content>
    </Card>
  );
}
