"use client";

import { useCallback, useMemo } from "react";
import { DataGrid, type DataGridColumn, type DataGridSortDescriptor } from "@heroui-pro/react";
import { EmptyStateSurface as ClinicEmptyState } from "@vitarish/web-ui";
import type { PatientViewDto, PatientSortBy } from "../../../types";
import { useLocalizedText, formatDate } from "../../../../../localization";

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
  const { t, locale } = useLocalizedText();

  const sortDescriptor: DataGridSortDescriptor | undefined = useMemo(
    () => (sortBy ? { column: sortBy, direction: descending ? "descending" : "ascending" } : undefined),
    [sortBy, descending]
  );

  const handleSortChange = useCallback(
    (descriptor: DataGridSortDescriptor | undefined) => {
      if (descriptor?.column) {
        onSort(descriptor.column as PatientSortBy);
      }
    },
    [onSort]
  );

  const handleRowAction = useCallback(
    (key: string | number) => onSelectPatient(String(key)),
    [onSelectPatient]
  );

  const formatPatientName = useCallback(
    (patient: PatientViewDto) =>
      patient.firstName || patient.lastName
        ? ((patient.firstName ?? "") + " " + (patient.lastName ?? "")).trim()
        : patient.name || t("clinic.patients.unnamedPatient"),
    [t]
  );

  const columns: DataGridColumn<PatientViewDto>[] = useMemo(
    () => [
      {
        id: "Name",
        header: t("clinic.patient.fields.firstName"),
        allowsSorting: true,
        isRowHeader: true,
        cell: (patient) => (
          <div className="flex flex-col gap-1">
            <span className="font-medium">{formatPatientName(patient)}</span>
            <span className="text-sm text-muted">
              {[
                patient.identificationCode
                  ? `${t("clinic.patient.fields.identificationCode")}: ${patient.identificationCode}`
                  : null,
                patient.phone,
              ].filter(Boolean).join(" / ")}
            </span>
          </div>
        ),
      },
      {
        id: "IdentificationCode",
        header: t("clinic.patient.fields.identificationCode"),
        accessorKey: "identificationCode",
        allowsSorting: true,
      },
      {
        id: "DateOfBirth",
        header: t("clinic.patient.fields.dateOfBirth"),
        allowsSorting: true,
        cell: (patient) =>
          patient.dateOfBirth ? formatDate(patient.dateOfBirth, locale) : "",
      },
    ],
    [t, locale, formatPatientName]
  );

  return (
    <DataGrid
      data={patients}
      columns={columns}
      getRowId={(patient) => patient.id ?? ""}
      aria-label={t("clinic.patients.title")}
      sortDescriptor={sortDescriptor}
      onSortChange={handleSortChange}
      onRowAction={handleRowAction}
      className="[&_.table__row]:cursor-pointer"
      renderEmptyState={() => (
        <ClinicEmptyState title={t("clinic.patients.searchEmpty")} />
      )}
    />
  );
}
