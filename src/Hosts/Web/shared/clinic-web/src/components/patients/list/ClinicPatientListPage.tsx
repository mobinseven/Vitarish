"use client";

import { useCallback, useMemo, useState } from "react";
import { Button, Chip } from "@heroui/react";
import { ActionBar } from "@heroui-pro/react";
import { FaIcon, LoadingStateSurface } from "@vitarish/web-ui";
import type { ClinicNavigationIntent } from "../../../navigation";
import { useLocalizedText } from "../../../localization/index";
import type { PatientSearchCriteria, PatientSortBy } from "../../../models/patients/patientSearchModels";
import { usePatientCount } from "../../../hooks/patients/usePatientCount";
import { usePatientList } from "../../../hooks/patients/usePatientList";
import { PatientSearchBar } from "./PatientSearchBar";
import { PatientsGrid } from "./PatientsGrid";

export interface ClinicPatientListPageProps {
  onNavigate: (intent: ClinicNavigationIntent) => void;
}

export function ClinicPatientListPage({ onNavigate }: ClinicPatientListPageProps) {
  const { t } = useLocalizedText();
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
  const countLabel =
    totalCount === 1
      ? t("clinic.patients.countFound.one", { count: String(totalCount) })
      : t("clinic.patients.countFound.other", { count: String(totalCount) });

  return (
    <div className="flex h-full min-h-0 flex-col">
      <ActionBar
        isOpen
        aria-label={t("clinic.patients.title")}
        className="w-[calc(100vw-2rem)] max-w-3xl"
      >
        <ActionBar.Prefix>
          <span aria-label={countLabel} className="tabular-nums">{totalCount}</span>
        </ActionBar.Prefix>
        <ActionBar.Content className="min-w-0 flex-1">
          <PatientSearchBar onSearch={handleSearch} />
        </ActionBar.Content>
        <ActionBar.Suffix>
          <Button
            aria-label={t("clinic.patients.newPatient")}
            size="sm"
            variant="primary"
            onPress={() => onNavigate({ kind: "patientNew" })}
          >
            <FaIcon aria-hidden="true" className="size-4 shrink-0" icon="plus" />
            <span className="action-bar__label">{t("clinic.patients.newPatient")}</span>
          </Button>
        </ActionBar.Suffix>
      </ActionBar>
      <div className="min-h-0 flex-1">
        {patientsQuery.isLoading ? (
          <LoadingStateSurface label={t("clinic.patients.loading")} />
        ) : (
          <PatientsGrid
            patients={patients}
            sortBy={sortBy}
            descending={descending}
            onSort={handleSort}
            onSelectPatient={(patientId) => onNavigate({ kind: "patientDetail", patientId })}
            isLoadingMore={patientsQuery.isFetchingNextPage}
            onLoadMore={
              canLoadMore
                ? () => void patientsQuery.fetchNextPage()
                : undefined
            }
          />
        )}
      </div>
    </div>
  );
}
