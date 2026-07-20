"use client";

import { ErrorStateSurface as ClinicErrorState, LoadingStateSurface as ClinicLoadingState } from "@vitarish/web-ui";
import { ApiError } from "@vitarish/web-api-client";
import type { ClinicNavigationIntent } from "../../../navigation";
import { useLocalizedText } from "../../../localization/index";
import { usePatientUpdate } from "../../../hooks/patients/usePatientUpdate";
import { PatientProfileForm } from "../profile/PatientProfileForm";
import { usePatientProfileEditor } from "../../../hooks/patients/profile/usePatientProfileEditor";

export interface ClinicPatientEditPageProps {
  patientId: string;
  onNavigate: (intent: ClinicNavigationIntent) => void;
}

export function ClinicPatientEditPage({ patientId, onNavigate }: ClinicPatientEditPageProps) {
  const { t } = useLocalizedText();
  const query = usePatientProfileEditor(patientId);
  const mutation = usePatientUpdate();
  const errorTitle = mutation.error instanceof ApiError && mutation.error.status === 403
    ? t("clinic.patient.edit.forbidden")
    : t("clinic.patient.edit.error");

  if (query.isLoading) return <ClinicLoadingState label={t("clinic.patient.detail.loading")} />;
  if (query.error || !query.formData) return <ClinicErrorState title={t("clinic.patient.detail.notFound")} />;

  return (
    <section className="flex flex-col gap-6" aria-labelledby="clinic-patient-edit-title">
      <header>
        <h1 id="clinic-patient-edit-title" className="text-2xl font-bold">{t("clinic.patient.edit.title")}</h1>
        <p className="text-sm text-muted">{t("clinic.patient.edit.description")}</p>
      </header>
      {mutation.error ? <ClinicErrorState title={errorTitle} /> : null}
      <PatientProfileForm
        initialData={query.formData}
        onSubmit={(data) => {
          void mutation.update(patientId, data).then(() => {
            onNavigate({ kind: "patientDetail", patientId });
          }).catch(() => undefined);
        }}
        onCancel={() => onNavigate({ kind: "patientDetail", patientId })}
        isPending={mutation.isPending}
      />
    </section>
  );
}
