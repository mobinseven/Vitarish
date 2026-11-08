"use client";

import {
  ErrorStateSurface as ClinicErrorState,
  LoadingStateSurface as ClinicLoadingState,
  PageHeader,
} from "@vitarish/ui";
import { ApiError } from "@vitarish/api-client";
import type { NavigationIntent } from "../../../navigation";
import { useLocalizedText } from "../../../localization/index";
import { usePatientUpdate } from "../../../hooks/patients/usePatientUpdate";
import { PatientProfileForm } from "../profile/PatientProfileForm";
import { usePatientProfileEditor } from "../../../hooks/patients/profile/usePatientProfileEditor";
import { getDisplayName } from "../overview/PatientOverviewHeader";
import { pageAncestors, usePageBack } from "../../navigation/usePageBack";
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
  const patientName = query.patient ? getDisplayName(query.patient, t("clinic.patients.unnamedPatient")) : "";
  const back = usePageBack(
    [
      pageAncestors.patients,
      { label: patientName, intent: { kind: "patientDetail", patientId } },
    ],
    onNavigate,
  );
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
      <PageHeader
        icon="user-pen"
        title={t("clinic.patient.edit.title")}
        titleId="clinic-patient-edit-title"
        details={t("clinic.patient.edit.description")}
        back={back}
      />
      <Card>
        <Card.Content>
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
