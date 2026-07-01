"use client";

import { useCallback } from "react";
import { Card, Button } from "@heroui/react";
import { IdentificationLookup } from "@/features/clinic/components/IdentificationLookup";
import { AdvancedSearchForm } from "@/features/clinic/components/AdvancedSearchForm";
import { AdvancedSearchResults } from "@/features/clinic/components/AdvancedSearchResults";
import { PatientProfileForm } from "@/features/clinic/patients/profile/components/PatientProfileForm";
import { emptyFormData } from "@/features/clinic/patients/profile/models/PatientProfileForm";
import { usePatientFindOrCreate } from "../hooks/usePatientFindOrCreate";
import { useLocalizedText } from "@/localization";
import type { PatientProfileFormData } from "@/features/clinic/patients/profile/models/PatientProfileForm";
import type { PatientViewDto } from "@/api/generated/clinic/model";

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
    state,
    identificationLookup,
    handleAdvancedSearch,
    handleSelectPatient,
    handleGoToLookup,
    createPatient,
    isPending,
  } = usePatientFindOrCreate();

  const handleLookupSelect = useCallback(
    (patientId: string) => {
      handleSelectPatient(patientId);
      onPatientSelected(patientId);
    },
    [handleSelectPatient, onPatientSelected]
  );

  const handleSearchSelect = useCallback(
    (patientId: string) => {
      handleSelectPatient(patientId);
      onPatientSelected(patientId);
    },
    [handleSelectPatient, onPatientSelected]
  );

  const handleCreateSubmit = useCallback(
    async (data: PatientProfileFormData) => {
      try {
        const result = await createPatient.create(data);
        if (result?.id) {
          onPatientSelected(result.id);
          onPatientCreated?.(result.id);
        }
      } catch {
        // Error surfaced through hook
      }
    },
    [createPatient, onPatientSelected, onPatientCreated]
  );

  return (
    <div className="flex flex-col gap-6">
      {state.step === "lookup" && (
        <>
          <Card>
            <Card.Header>
              <h2 className="text-lg font-semibold">{t("clinic.checkIn.step1")}</h2>
            </Card.Header>
            <Card.Content>
              <IdentificationLookup onSelectPatient={handleLookupSelect} />
            </Card.Content>
          </Card>

          <div className="flex items-center gap-4">
            <div className="h-px flex-1 bg-default-200" />
            <span className="text-sm text-default-500">{t("clinic.patient.findOrCreate.or")}</span>
            <div className="h-px flex-1 bg-default-200" />
          </div>

          <Card>
            <Card.Header>
              <h2 className="text-lg font-semibold">{t("clinic.patient.advancedSearch.title")}</h2>
            </Card.Header>
            <Card.Content>
              <AdvancedSearchForm onSearch={handleAdvancedSearch} isPending={isPending} />
            </Card.Content>
          </Card>
        </>
      )}

      {state.step === "search" && (
        <Card>
          <Card.Header>
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-semibold">{t("clinic.patient.search.results")}</h2>
              <Button variant="ghost" onPress={handleGoToLookup}>
                ← {t("clinic.patient.findOrCreate.backToLookup")}
              </Button>
            </div>
          </Card.Header>
          <Card.Content>
            <AdvancedSearchResults
              results={(state.searchResults ?? []) as PatientViewDto[]}
              isLoading={false}
              onSelectPatient={handleSearchSelect}
            />
          </Card.Content>
        </Card>
      )}

      {state.step === "create" && (
        <div className="flex flex-col gap-6">
          <div className="flex items-center justify-between">
            <h1 className="text-2xl font-bold">{t("clinic.patients.newPatient")}</h1>
            <Button variant="ghost" onPress={handleGoToLookup}>
              ← {t("clinic.patient.findOrCreate.backToLookup")}
            </Button>
          </div>
          <PatientProfileForm
            initialData={emptyFormData()}
            onSubmit={handleCreateSubmit}
            onCancel={handleGoToLookup}
            isPending={createPatient.isPending}
          />
        </div>
      )}

      {state.step === "selected" && (
        <Card>
          <Card.Content className="py-8 text-center">
            <p className="text-sm text-default-500">{t("clinic.patient.findOrCreate.patientSelected")}</p>
            <Button variant="ghost" className="mt-4" onPress={handleGoToLookup}>
              {t("clinic.patient.findOrCreate.findAnother")}
            </Button>
          </Card.Content>
        </Card>
      )}
    </div>
  );
}
