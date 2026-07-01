"use client";

import { Card } from "@heroui/react";
import { PatientRow } from "./PatientRow";
import type { PatientViewDto, PatientSortBy } from "../types";

interface SortHeader {
  field: PatientSortBy;
  label: string;
}

const sortHeaders: SortHeader[] = [
  { field: "Name", label: "Name" },
  { field: "IdentificationCode", label: "ID Code" },
  { field: "DateOfBirth", label: "DOB" },
  { field: "CreatedAt", label: "Created" },
];

interface PatientsGridProps {
  patients: PatientViewDto[];
  sortBy?: PatientSortBy;
  descending: boolean;
  onSort: (field: PatientSortBy) => void;
  onSelectPatient: (patientId: string) => void;
}

export function PatientsGrid({
  patients,
  sortBy,
  descending,
  onSort,
  onSelectPatient,
}: PatientsGridProps) {
  if (patients.length === 0) {
    return (
      <Card className="glass-card p-8">
        <p className="text-center text-default-500">
          No patients found. Try adjusting your search.
        </p>
      </Card>
    );
  }

  return (
    <Card className="glass-card">
      <Card.Header className="border-b border-default-200">
        <div className="grid grid-cols-4 gap-4 px-3 py-2 text-sm font-medium text-default-500">
          {sortHeaders.map((header) => (
            <button
              key={header.field}
              className="text-start hover:text-default-700 transition-colors"
              onClick={() => onSort(header.field)}
            >
              {header.label}
              {sortBy === header.field && (
                <span className="ms-1">{descending ? " ↓" : " ↑"}</span>
              )}
            </button>
          ))}
        </div>
      </Card.Header>
      <Card.Content className="divide-y divide-default-200">
        {patients.map((patient) => (
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
