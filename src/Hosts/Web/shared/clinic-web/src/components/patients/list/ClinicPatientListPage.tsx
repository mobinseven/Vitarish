"use client";

import { useCallback, useState } from "react";
import { Button } from "@heroui/react";
import { LoadingStateSurface } from "@vitarish/web-ui";
import type { ClinicNavigationIntent } from "../../../navigation";
import { useLocalizedText } from "../../../localization/index";
import type { PatientSearchCriteria, PatientSortBy } from "../../../models/patients/patientSearchModels";
import { usePatientCount } from "../../../hooks/patients/usePatientCount";
import { usePatientList } from "../../../hooks/patients/usePatientList";
import { PatientSearchBar } from "./PatientSearchBar";
import { PatientsGrid } from "./PatientsGrid";

const defaultPageSize = 20;

export interface ClinicPatientListPageProps {
  onNavigate: (intent: ClinicNavigationIntent) => void;
}

export function ClinicPatientListPage({ onNavigate }: ClinicPatientListPageProps) {
  const { t } = useLocalizedText();
  const [search, setSearch] = useState("");
  const [sortBy, setSortBy] = useState<PatientSortBy | undefined>();
  const [descending, setDescending] = useState(false);
  const [skip, setSkip] = useState(0);
  const criteria: PatientSearchCriteria = {
    search: search || undefined,
    skip,
    take: defaultPageSize,
    sortBy,
    descending,
  };
  const { data: patients, isLoading } = usePatientList(criteria);
  const { data: count } = usePatientCount(search || undefined);

  const handleSearch = useCallback((value: string) => {
    setSearch(value);
    setSkip(0);
  }, []);
  const handleSort = useCallback((field: PatientSortBy) => {
    if (sortBy === field) {
      setDescending(!descending);
    } else {
      setSortBy(field);
      setDescending(false);
    }
    setSkip(0);
  }, [sortBy, descending]);
  const totalCount = typeof count === "number" ? count : Number(count) || 0;

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">{t("clinic.patients.title")}</h1>
        <Button variant="primary" onPress={() => onNavigate({ kind: "patientNew" })}>
          {t("clinic.patients.newPatient")}
        </Button>
      </div>
      <PatientSearchBar onSearch={handleSearch} />
      {isLoading ? (
        <LoadingStateSurface label={t("clinic.patients.loading")} />
      ) : (
        <PatientsGrid
          patients={patients ?? []}
          sortBy={sortBy}
          descending={descending}
          onSort={handleSort}
          onSelectPatient={(patientId) => onNavigate({ kind: "patientDetail", patientId })}
        />
      )}
      <div className="flex items-center justify-between text-sm text-muted">
        <span>
          {totalCount === 1
            ? t("clinic.patients.countFound.one", { count: String(totalCount) })
            : t("clinic.patients.countFound.other", { count: String(totalCount) })}
        </span>
        <div className="flex gap-2">
          <Button variant="ghost" isDisabled={skip === 0} onPress={() => setSkip(Math.max(0, skip - defaultPageSize))}>
            {t("common.actions.previous")}
          </Button>
          <Button variant="ghost" isDisabled={skip + defaultPageSize >= totalCount} onPress={() => setSkip(skip + defaultPageSize)}>
            {t("common.actions.next")}
          </Button>
        </div>
      </div>
    </div>
  );
}
