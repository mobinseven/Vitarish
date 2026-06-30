"use client";

import { useCallback, useState } from "react";
import { useRouter } from "next/navigation";
import { Card, Button, Spinner } from "@heroui/react";
import { Routes } from "@/app/routes";
import { usePatientList } from "../hooks/usePatientList";
import { usePatientCount } from "../hooks/usePatientCount";
import { PatientSearchBar } from "./PatientSearchBar";
import { PatientsGrid } from "./PatientsGrid";
import type { PatientSearchCriteria, PatientSortBy } from "../types";

const defaultPageSize = 20;

export function ClinicOwnerPatientsPage() {
  const router = useRouter();
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
        <h1 className="text-2xl font-bold">Patients</h1>
        <Button
          variant="primary"
          onPress={() => router.push(Routes.Clinic.Patients.New)}
        >
          New Patient
        </Button>
      </div>

      <PatientSearchBar onSearch={handleSearch} />

      {isLoading ? (
        <Card className="glass-card p-8">
          <div className="flex items-center justify-center gap-2">
            <Spinner size="sm" />
            <span className="text-default-500">Loading patients...</span>
          </div>
        </Card>
      ) : (
        <PatientsGrid
          patients={patients ?? []}
          sortBy={sortBy}
          descending={descending}
          onSort={handleSort}
          onSelectPatient={handleSelectPatient}
        />
      )}

      <div className="flex items-center justify-between text-sm text-default-500">
        <span>
          {totalCount} patient{totalCount !== 1 ? "s" : ""} found
        </span>
        <div className="flex gap-2">
          <Button
            variant="ghost"
            isDisabled={!canPrevious}
            onPress={handlePreviousPage}
          >
            Previous
          </Button>
          <Button
            variant="ghost"
            isDisabled={!canNext}
            onPress={handleNextPage}
          >
            Next
          </Button>
        </div>
      </div>
    </div>
  );
}
