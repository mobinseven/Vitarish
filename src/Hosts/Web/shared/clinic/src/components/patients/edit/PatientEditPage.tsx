"use client";

import {
  ErrorStateSurface as ClinicErrorState,
  LoadingStateSurface as ClinicLoadingState,
  HeaderIcon,
} from "@vitarish/ui";
import { ApiError } from "@vitarish/api-client";
import type { NavigationIntent } from "../../../navigation";
import { useLocalizedText } from "../../../localization/index";
import { usePatientUpdate } from "../../../hooks/patients/usePatientUpdate";
import { PatientProfileForm } from "../profile/PatientProfileForm";
import { usePatientProfileEditor } from "../../../hooks/patients/profile/usePatientProfileEditor";
import { PatientOverviewHeader } from "../overview/PatientOverviewHeader";
import { Card } from "@heroui/react";

export interface PatientEditPageProps {
  patientId: string;
  onNavigate: (intent: NavigationIntent) => void;
}

export function PatientEditPage({
  patientId,
  onNavigate,
}: PatientEditPageProps) {
  const { t } = useLocalizedText();
  const query = usePatientProfileEditor(patientId);
  const mutation = usePatientUpdate();
  const errorTitle =
    mutation.error instanceof ApiError && mutation.error.status === 403
      ? t("clinic.patient.edit.forbidden")
      : t("clinic.patient.edit.error");

  if (query.isLoading)
    return <ClinicLoadingState label={t("clinic.patient.detail.loading")} />;
  if (query.error || !query.formData || !query.patient)
    return <ClinicErrorState title={t("clinic.patient.detail.notFound")} />;

  return (
    <section
      className="mx-auto flex w-full max-w-6xl flex-col gap-6"
      aria-labelledby="clinic-patient-edit-title"
    >
      <Card className="overflow-hidden" variant="transparent">
        <PatientOverviewHeader patient={query.patient} />
      </Card>
      <Card>
        <Card.Content>
          <header>
            <div className="flex items-center gap-4">
              <HeaderIcon context="patient-record" icon="user-pen" level="page" />
              <div className="min-w-0">
                <h1 id="clinic-patient-edit-title" className="text-2xl font-bold">
                  {t("clinic.patient.edit.title")}
                </h1>
                <p className="text-sm text-muted">
                  {t("clinic.patient.edit.description")}
                </p>
              </div>
            </div>
          </header>
          {mutation.error ? <ClinicErrorState title={errorTitle} /> : null}
          <PatientProfileForm
            initialData={query.formData}
            onSubmit={(data) => {
              void mutation
                .update(patientId, data)
                .then(() => {
                  onNavigate({ kind: "patientDetail", patientId });
                })
                .catch(() => undefined);
            }}
            onCancel={() => onNavigate({ kind: "patientDetail", patientId })}
            isPending={mutation.isPending}
          />
        </Card.Content>
      </Card>
    </section>
  );
}
