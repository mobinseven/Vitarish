"use client";

import { useCallback, useEffect, useState } from "react";
import { Button, Input, Label, TextField } from "@heroui/react";

import { AdvancedSearchForm } from "./AdvancedSearchForm";
import { AdvancedSearchResults } from "./AdvancedSearchResults";
import {
  EmptyStateSurface as ClinicEmptyState,
  ErrorStateSurface as ClinicErrorState,
  FaIcon,
  LoadingStateSurface as ClinicLoadingState,
} from "@vitarish/web-ui";
import { PatientProfileForm } from "../profile/PatientProfileForm";
import { emptyFormData } from "../../../models/patients/profile/PatientProfileForm";
import { usePatientFindOrCreate } from "../../../hooks/patients/find-or-create/usePatientFindOrCreate";
import { useLocalizedText } from "../../../localization/index";
import type { PatientProfileFormData } from "../../../models/patients/profile/PatientProfileForm";

interface PatientFindOrCreateProps {
  onPatientSelected: (patientId: string) => void;
  onPatientCreated?: (patientId: string) => void;
  /** When true, the "selected" step is hidden — the parent handles post-selection. */
  hideSelectedState?: boolean;
}

export function PatientFindOrCreate({
  onPatientSelected,
  onPatientCreated,
  hideSelectedState,
}: PatientFindOrCreateProps) {
  const { t } = useLocalizedText();
  const {
    step,
    searchResults,
    lookupError,
    createIdentificationCode,
    isLooking,
    autocompleteResults,
    isAutocompleting,
    handleLookup,
    handleAutocomplete,
    handleAdvancedSearch,
    handleSelectPatient,
    handleGoToLookup,
    createPatient,
    isPending,
  } = usePatientFindOrCreate();

  const [code, setCode] = useState("");
  const [createError, setCreateError] = useState(false);
  const [showAdvanced, setShowAdvanced] = useState(false);

  useEffect(() => {
    const query = code.trim();
    const digitCount = (query.match(/\d/g) ?? []).length;
    const search = () => void handleAutocomplete(query);

    if (!query) {
      search();
      return;
    }

    if (digitCount > 4) {
      const timer = window.setTimeout(search, 300);
      return () => window.clearTimeout(timer);
    }

    search();
  }, [code, handleAutocomplete]);

  const handleLookupSubmit = useCallback(() => {
    void handleLookup(code).then((patient) => {
      if (patient?.id) onPatientSelected(patient.id);
    });
  }, [code, handleLookup, onPatientSelected]);

  const handleSearchSelect = useCallback(
    (patientId: string) => {
      handleSelectPatient();
      onPatientSelected(patientId);
    },
    [handleSelectPatient, onPatientSelected],
  );

  const handleCreateSubmit = useCallback(
    async (data: PatientProfileFormData) => {
      setCreateError(false);
      try {
        const result = await createPatient.create(data);
        if (result?.id) {
          handleSelectPatient();
          onPatientSelected(result.id);
          onPatientCreated?.(result.id);
        }
      } catch {
        setCreateError(true);
      }
    },
    [createPatient, handleSelectPatient, onPatientSelected, onPatientCreated],
  );

  if (hideSelectedState && step === "selected") return null;

  return (
    <div className="flex flex-col gap-4">
      {step === "lookup" && (
        <>
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

          {code.trim() && (isAutocompleting || autocompleteResults.length > 0) && (
            <AdvancedSearchResults
              results={autocompleteResults}
              isLoading={isAutocompleting}
              onSelectPatient={handleSearchSelect}
            />
          )}

          {isLooking && (
            <ClinicLoadingState
              label={t("clinic.patient.identification.lookingUp")}
            />
          )}

          {lookupError === "not-found" && (
            <ClinicEmptyState
              title={t("clinic.patient.identification.notFound")}
            />
          )}

          {lookupError === "error" && (
            <ClinicErrorState
              title={t("clinic.patient.identification.error")}
            />
          )}

          <Button
            variant="primary"
            isPending={isLooking}
            isDisabled={!code.trim()}
            onPress={handleLookupSubmit}
          >
            {t("clinic.patient.identification.lookupAction")}
          </Button>

          <button
            type="button"
            onClick={() => setShowAdvanced(!showAdvanced)}
            className="self-start text-sm text-muted transition-colors hover:text-foreground"
          >
            <FaIcon icon={showAdvanced ? "chevron-up" : "chevron-down"} aria-hidden="true" className="mr-1.5" />
            {t("clinic.checkIn.cantFindPatient")}
          </button>

          {showAdvanced && (
            <>
              <AdvancedSearchForm
                onSearch={handleAdvancedSearch}
                isPending={isPending}
              />

              <div className="flex items-center gap-4">
                <div className="h-px flex-1 bg-divider" />
                <span className="text-xs text-muted">
                  {t("clinic.patient.findOrCreate.or")}
                </span>
                <div className="h-px flex-1 bg-divider" />
              </div>

              <Button
                variant="ghost"
                size="sm"
                onPress={handleGoToLookup}
              >
                <FaIcon icon="plus" aria-hidden="true" className="mr-1.5" />
                {t("clinic.checkIn.createNewPatient")}
              </Button>
            </>
          )}
        </>
      )}

      {step === "search" && (
        <div className="flex flex-col gap-3">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-medium">
              {t("clinic.patient.search.results")}
            </h3>
            <Button variant="ghost" size="sm" onPress={handleGoToLookup}>
              <FaIcon icon="arrow-left" aria-hidden="true" className="mr-1" />
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
        <div className="flex flex-col gap-4">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-medium">
              {t("clinic.patients.newPatient")}
            </h3>
            <Button variant="ghost" size="sm" onPress={handleGoToLookup}>
              <FaIcon icon="arrow-left" aria-hidden="true" className="mr-1" />
              {t("common.actions.previous")}
            </Button>
          </div>
          {(createError || createPatient.error) && (
            <ClinicErrorState title={t("clinic.patients.createFailed")} />
          )}
          <PatientProfileForm
            initialData={{
              ...emptyFormData(),
              identificationCode: createIdentificationCode,
            }}
            onSubmit={handleCreateSubmit}
            onCancel={handleGoToLookup}
            isPending={createPatient.isPending}
          />
        </div>
      )}

      {step === "selected" && (
        <ClinicEmptyState
          title={t("clinic.patient.findOrCreate.patientSelected")}
        >
          <Button variant="ghost" onPress={handleGoToLookup}>
            {t("clinic.patient.findOrCreate.findAnother")}
          </Button>
        </ClinicEmptyState>
      )}
    </div>
  );
}
