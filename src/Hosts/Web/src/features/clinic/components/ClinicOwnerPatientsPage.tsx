"use client";

import { useCallback, useState } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@heroui/react";
import { ClinicLoadingState } from "@/features/clinic/components/ClinicStateSurface";
import { Routes } from "@/app/routes";
import { usePatientList } from "../hooks/usePatientList";
import { usePatientCount } from "../hooks/usePatientCount";
import { PatientSearchBar } from "./PatientSearchBar";
import { PatientsGrid } from "./PatientsGrid";

import type { PatientSearchCriteria, PatientSortBy } from "../types";
import { useLocalizedText } from "@/localization";

const defaultPageSize = 20;

export function ClinicOwnerPatientsPage() {
  const router = useRouter();
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

  const handleSort = useCallback(
    (field: PatientSortBy) => {
      if (sortBy === field) {
        setDescending(!descending);
      } else {
        setSortBy(field);
        setDescending(false);
      }
      setSkip(0);
    },
    [sortBy, descending]
  );

  const handleSelectPatient = useCallback(
    (patientId: string) => {
      router.push(Routes.Clinic.Patients.Detail(patientId));
    },
    [router]
  );

  const handlePreviousPage = useCallback(() => {
    setSkip(Math.max(0, skip - defaultPageSize));
  }, [skip]);

  const handleNextPage = useCallback(() => {
    setSkip(skip + defaultPageSize);
  }, [skip]);

  const totalCount = typeof count === "number" ? count : Number(count) || 0;
  const canPrevious = skip > 0;
  const canNext = skip + defaultPageSize < totalCount;

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">{t("clinic.patients.title")}</h1>
        <Button
          variant="primary"
          onPress={() => router.push(Routes.Clinic.Patients.New)}
        >
          {t("clinic.patients.newPatient")}
        </Button>
      </div>

      <PatientSearchBar onSearch={handleSearch} />

      {isLoading ? (
        <ClinicLoadingState label={t("clinic.patients.loading")} />
      ) : (
        <PatientsGrid
          patients={patients ?? []}
          sortBy={sortBy}
          descending={descending}
          onSort={handleSort}
          onSelectPatient={handleSelectPatient}
        />
      )}

      <div className="flex items-center justify-between text-sm text-muted">
        <span>
          {totalCount === 1
            ? t("clinic.patients.countFound.one", { count: String(totalCount) })
            : t("clinic.patients.countFound.other", { count: String(totalCount) })}
        </span>
        <div className="flex gap-2">
          <Button
            variant="ghost"
            isDisabled={!canPrevious}
            onPress={handlePreviousPage}
          >
            {t("common.actions.previous")}
          </Button>
          <Button
            variant="ghost"
            isDisabled={!canNext}
            onPress={handleNextPage}
          >
            {t("common.actions.next")}
          </Button>
        </div>
      </div>
    </div>
  );
}
