"use client";

import { useCallback, useMemo } from "react";
import { Spinner } from "@heroui/react";
import { DataGrid, EmptyState, type DataGridColumn, type DataGridSortDescriptor } from "@heroui-pro/react";
import type { PatientViewDto, PatientSortBy } from "../../../models/patients/patientSearchModels";
import { useLocalizedText, formatDate } from "../../../localization/index";

interface PatientsGridProps {
  patients: PatientViewDto[];
  sortBy?: PatientSortBy;
  descending: boolean;
  isLoadingMore?: boolean;
  onSort: (field: PatientSortBy) => void;
  onSelectPatient: (patientId: string) => void;
  onLoadMore?: () => void;
}

const stickyHeaderClassName = "sticky top-0 z-10 bg-surface-secondary";

export function PatientsGrid({
  patients,
  sortBy,
  descending,
  isLoadingMore = false,
  onSort,
  onSelectPatient,
  onLoadMore,
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
        headerClassName: stickyHeaderClassName,
        allowsSorting: true,
        isRowHeader: true,
        cell: (patient) => (
          <span className="block truncate font-medium">{formatPatientName(patient)}</span>
        ),
      },
      {
        id: "IdentificationCode",
        header: t("clinic.patient.fields.identificationCode"),
        headerClassName: stickyHeaderClassName,
        accessorKey: "identificationCode",
        allowsSorting: true,
      },
      {
        id: "DateOfBirth",
        header: t("clinic.patient.fields.dateOfBirth"),
        headerClassName: stickyHeaderClassName,
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
      virtualized
      rowHeight={48}
      headingHeight={40}
      onLoadMore={onLoadMore}
      isLoadingMore={isLoadingMore}
      loadMoreContent={<Spinner aria-label={t("common.states.loading")} size="sm" />}
      className="h-full [&_.table__row]:h-12 [&_.table__row]:cursor-pointer"
      scrollContainerClassName="h-full min-h-0 overflow-y-auto after:block after:h-20 after:content-['']"
      renderEmptyState={() => (
        <EmptyState size="sm">
          <EmptyState.Header>
            <EmptyState.Media variant="icon">
              <svg aria-hidden="true" fill="none" viewBox="0 0 24 24" className="size-6" stroke="currentColor" strokeWidth="1.5">
                <path strokeLinecap="round" strokeLinejoin="round" d="M18 18.75a7.5 7.5 0 1 0-12 0m12 0a9.75 9.75 0 0 0-12 0M18 18.75a9.75 9.75 0 0 1 3.75 3.75M6 18.75A9.75 9.75 0 0 0 2.25 22.5M12 12a3.75 3.75 0 1 0 0-7.5A3.75 3.75 0 0 0 12 12Z" />
              </svg>
            </EmptyState.Media>
            <EmptyState.Title>{t("clinic.patients.emptyState")}</EmptyState.Title>
            <EmptyState.Description>{t("clinic.patients.searchEmpty")}</EmptyState.Description>
          </EmptyState.Header>
        </EmptyState>
      )}
    />
  );
}
