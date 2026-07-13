"use client";

import { useCallback, useState } from "react";
import { Button, Input, Label, TextField } from "@heroui/react";

import { AdvancedSearchForm } from "./AdvancedSearchForm";
import { AdvancedSearchResults } from "./AdvancedSearchResults";
import { EmptyStateSurface as ClinicEmptyState, ErrorStateSurface as ClinicErrorState, LoadingStateSurface as ClinicLoadingState } from "@vitarish/web-ui";
import { PatientProfileForm } from "../profile/PatientProfileForm";
import { emptyFormData } from "../../../models/patients/profile/PatientProfileForm";
import { usePatientFindOrCreate } from "../../../hooks/patients/find-or-create/usePatientFindOrCreate";
import { useLocalizedText } from "../../../localization/index";
import type { PatientProfileFormData } from "../../../models/patients/profile/PatientProfileForm";

interface PatientFindOrCreateProps {
  onPatientSelected: (patientId: string) => void;
  onPatientCreated?: (patientId: string) => void;
}

export function PatientFindOrCreate({
  onPatientSelected,
  onPatientCreated,
}: PatientFindOrCreateProps) {
  const { t } = useLocalizedText();
  const {
    step,
    searchResults,
    lookupError,
    isLooking,
    handleLookup,
    handleAdvancedSearch,
    handleSelectPatient,
    handleGoToLookup,
    createPatient,
    isPending,
  } = usePatientFindOrCreate();

  const [code, setCode] = useState("");
  const [createError, setCreateError] = useState(false);

  const handleLookupSubmit = useCallback(() => {
    handleLookup(code);
  }, [code, handleLookup]);

  const handleSearchSelect = useCallback(
    (patientId: string) => {
      handleSelectPatient();
      onPatientSelected(patientId);
    },
    [handleSelectPatient, onPatientSelected]
  );

  const handleCreateSubmit = useCallback(
    async (data: PatientProfileFormData) => {
      setCreateError(false);
      try {
        const result = await createPatient.create(data);
        if (result?.id) {
          onPatientSelected(result.id);
          onPatientCreated?.(result.id);
        }
      } catch {
        setCreateError(true);
      }
    },
    [createPatient, onPatientSelected, onPatientCreated]
  );

  return (
    <div className="flex flex-col gap-6">
      {step === "lookup" && (
        <>
          <section className="rounded-medium border border-divider p-4">
            <header className="mb-3">
              <h2>{t("clinic.checkIn.step1")}</h2>
            </header>
            <div className="flex flex-col gap-4">
              <TextField>
                <Label>{t("clinic.patient.identification.label")}</Label>
                <Input
                  placeholder={t("clinic.patient.identification.placeholder")}
                  value={code}
                  onChange={(event) => setCode(event.target.value)}
                  onKeyDown={(event) => {
                    if (event.key === "Enter") handleLookupSubmit();
                  }}
                />
              </TextField>
              {isLooking && (
                <ClinicLoadingState label={t("clinic.patient.identification.lookingUp")} />
              )}
              {lookupError === "not-found" && (
                <ClinicEmptyState title={t("clinic.patient.identification.notFound")} />
              )}
              {lookupError === "error" && (
                <ClinicErrorState title={t("clinic.patient.identification.error")} />
              )}
              <Button variant="primary" isPending={isLooking} onPress={handleLookupSubmit}>
                {t("clinic.patient.identification.lookupAction")}
              </Button>
            </div>
          </section>

          <div className="flex items-center gap-4">
            <div className="h-px flex-1 bg-surface-secondary" />
            <span className="text-sm text-muted">{t("clinic.patient.findOrCreate.or")}</span>
            <div className="h-px flex-1 bg-surface-secondary" />
          </div>

          <AdvancedSearchForm onSearch={handleAdvancedSearch} isPending={isPending} />
        </>
      )}

      {step === "search" && (
        <div className="flex flex-col gap-4">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-semibold">{t("clinic.patient.search.results")}</h2>
            <Button variant="ghost" onPress={handleGoToLookup}>
              <i aria-hidden className="fa-solid fa-arrow-left" />
              {t("common.actions.previous")}
            </Button>
          </div>
          <AdvancedSearchResults
            results={searchResults ?? []}
            isLoading={false}
            onSelectPatient={handleSearchSelect}
          />
        </div>
      )}

      {step === "create" && (
        <div className="flex flex-col gap-6">
          <div className="flex items-center justify-between">
            <h1 className="text-2xl font-bold">{t("clinic.patients.newPatient")}</h1>
            <Button variant="ghost" onPress={handleGoToLookup}>
              <i aria-hidden className="fa-solid fa-arrow-left" />
              {t("common.actions.previous")}
            </Button>
          </div>
          {(createError || createPatient.error) && (
            <ClinicErrorState title={t("clinic.patients.createFailed")} />
          )}
          <PatientProfileForm
            initialData={emptyFormData()}
            onSubmit={handleCreateSubmit}
            onCancel={handleGoToLookup}
            isPending={createPatient.isPending}
          />
        </div>
      )}

      {step === "selected" && (
        <ClinicEmptyState title={t("clinic.patient.findOrCreate.patientSelected")}>
          <Button variant="ghost" onPress={handleGoToLookup}>
            {t("clinic.patient.findOrCreate.findAnother")}
          </Button>
        </ClinicEmptyState>
      )}
    </div>
  );
}
