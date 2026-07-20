"use client";

import { useCallback, useMemo, useState } from "react";
import type { NavigationIntent } from "../../../navigation";
import type { PatientSearchCriteria, PatientSortBy } from "../../../models/patients/patientSearchModels";
import { usePatientCount } from "../../../hooks/patients/usePatientCount";
import { usePatientList } from "../../../hooks/patients/usePatientList";
import { PatientsGrid } from "./PatientsGrid";

export interface PatientListPageProps {
  onNavigate: (intent: NavigationIntent) => void;
}

export function PatientListPage({ onNavigate }: PatientListPageProps) {
  const [search, setSearch] = useState("");
  const [sortBy, setSortBy] = useState<PatientSortBy | undefined>();
  const [descending, setDescending] = useState(false);
  const criteria: PatientSearchCriteria = useMemo(
    () => ({
      search: search || undefined,
      sortBy,
      descending,
    }),
    [descending, search, sortBy],
  );
  const patientsQuery = usePatientList(criteria);
  const { data: count } = usePatientCount(search || undefined);

  const handleSearch = useCallback((value: string) => {
    setSearch(value);
  }, []);
  const handleAddPatient = useCallback(() => {
    onNavigate({ kind: "patientNew" });
  }, [onNavigate]);
  const handleSelectPatient = useCallback((patientId: string) => {
    onNavigate({ kind: "patientDetail", patientId });
  }, [onNavigate]);
  const handleSort = useCallback((field: PatientSortBy) => {
    if (sortBy === field) {
      setDescending(!descending);
    } else {
      setSortBy(field);
      setDescending(false);
    }
  }, [sortBy, descending]);
  const totalCount = typeof count === "number" ? count : Number(count) || 0;
  const patients = patientsQuery.data?.pages.flat() ?? [];
  const canLoadMore =
    patientsQuery.hasNextPage && (count == null || patients.length < totalCount);

  return (
    <div className="h-full min-h-0">
      <PatientsGrid
        patients={patients}
        totalCount={totalCount}
        sortBy={sortBy}
        descending={descending}
        onAddPatient={handleAddPatient}
        onSearch={handleSearch}
        onSort={handleSort}
        onSelectPatient={handleSelectPatient}
        isLoading={patientsQuery.isLoading}
        isLoadingMore={patientsQuery.isFetchingNextPage}
        onLoadMore={
          canLoadMore
            ? () => void patientsQuery.fetchNextPage()
            : undefined
        }
      />
    </div>
  );
}
