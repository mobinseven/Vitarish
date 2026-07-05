"use client";

import { useCallback, useState } from "react";
import { Card, Button, TextField, Label, Input, Spinner } from "@heroui/react";
import { AdvancedSearchForm } from "@/features/clinic/components/AdvancedSearchForm";
import { AdvancedSearchResults } from "@/features/clinic/components/AdvancedSearchResults";
import { PatientProfileForm } from "@/features/clinic/patients/profile/components/PatientProfileForm";
import { emptyFormData } from "@/features/clinic/patients/profile/models/PatientProfileForm";
import { usePatientFindOrCreate } from "../hooks/usePatientFindOrCreate";
import { useLocalizedText } from "@/localization";
import type { PatientProfileFormData } from "@/features/clinic/patients/profile/models/PatientProfileForm";

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
      {step === "lookup" && (
        <>
          <Card>
            <Card.Header>
              <h2 className="text-lg font-semibold">{t("clinic.checkIn.step1")}</h2>
            </Card.Header>
            <Card.Content className="flex flex-col gap-4">
              <TextField>
                <Label>{t("clinic.patient.identification.label")}</Label>
                <Input
                  placeholder={t("clinic.patient.identification.placeholder")}
                  value={code}
                  onChange={(e) => setCode(e.target.value)}
                  onKeyDown={(e) => { if (e.key === "Enter") handleLookupSubmit(); }}
                />
              </TextField>
              {isLooking && (
                <div className="flex items-center gap-2 text-muted">
                  <Spinner size="sm" />
                  <span>{t("clinic.patient.identification.lookingUp")}</span>
                </div>
              )}
              {lookupError === "not-found" && (
                <p className="text-sm text-muted">{t("clinic.patient.identification.notFound")}</p>
              )}
              {lookupError === "error" && (
                <p className="text-sm text-danger">{t("clinic.patient.identification.error")}</p>
              )}
              <Button variant="primary" isPending={isLooking} onPress={handleLookupSubmit}>
                {t("clinic.patient.identification.lookupAction")}
              </Button>
            </Card.Content>
          </Card>

          <div className="flex items-center gap-4">
            <div className="h-px flex-1 bg-surface-secondary" />
            <span className="text-sm text-muted">{t("clinic.patient.findOrCreate.or")}</span>
            <div className="h-px flex-1 bg-surface-secondary" />
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

      {step === "search" && (
        <Card>
          <Card.Header>
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-semibold">{t("clinic.patient.search.results")}</h2>
              <Button variant="ghost" onPress={handleGoToLookup}>
                ← {t("common.actions.previous")}
              </Button>
            </div>
          </Card.Header>
          <Card.Content>
            <AdvancedSearchResults
              results={searchResults ?? []}
              isLoading={false}
              onSelectPatient={handleSearchSelect}
            />
          </Card.Content>
        </Card>
      )}

      {step === "create" && (
        <div className="flex flex-col gap-6">
          <div className="flex items-center justify-between">
            <h1 className="text-2xl font-bold">{t("clinic.patients.newPatient")}</h1>
            <Button variant="ghost" onPress={handleGoToLookup}>
              ← {t("common.actions.previous")}
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

      {step === "selected" && (
        <Card>
          <Card.Content className="py-8 text-center">
            <p className="text-sm text-muted">{t("clinic.patient.findOrCreate.patientSelected")}</p>
            <Button variant="ghost" className="mt-4" onPress={handleGoToLookup}>
              {t("clinic.patient.findOrCreate.findAnother")}
            </Button>
          </Card.Content>
        </Card>
      )}
    </div>
  );
}
