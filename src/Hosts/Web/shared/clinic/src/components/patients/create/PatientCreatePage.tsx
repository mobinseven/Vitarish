"use client";

import { ErrorStateSurface as ClinicErrorState, PageHeader } from "@vitarish/ui";
import { pageAncestors, usePageBack } from "../../navigation/usePageBack";
import { ApiError } from "@vitarish/api-client";
import type { NavigationIntent } from "../../../navigation";
import { useLocalizedText } from "../../../localization/index";
import { PatientProfileForm } from "../profile/PatientProfileForm";
import { useCreatePatientProfile } from "../../../hooks/patients/profile/useCreatePatientProfile";
import { emptyFormData } from "../../../models/patients/profile/PatientProfileForm";

export interface PatientCreatePageProps {
  onNavigate: (intent: NavigationIntent) => void;
}

export function PatientCreatePage({ onNavigate }: PatientCreatePageProps) {
  const { t } = useLocalizedText();
  const back = usePageBack([pageAncestors.patients], onNavigate);
  const mutation = useCreatePatientProfile();
  const errorTitle =
    mutation.error instanceof ApiError && mutation.error.status === 403
      ? t("clinic.patients.createForbidden")
      : t("clinic.patients.createFailed");

  return (
    <section
      className="flex flex-col gap-6"
      aria-labelledby="clinic-patient-create-title"
    >
      <PageHeader
        icon="user-plus"
        title={t("clinic.patients.newPatient")}
        titleId="clinic-patient-create-title"
        details={t("clinic.patients.createPatient")}
        back={back}
      />
      {mutation.error ? <ClinicErrorState title={errorTitle} /> : null}
      <PatientProfileForm
        initialData={emptyFormData()}
        onSubmit={(data) => {
          void mutation
            .create(data)
            .then((patient) => {
              if (patient?.id)
                onNavigate({ kind: "patientDetail", patientId: patient.id });
            })
            .catch(() => undefined);
        }}
        onCancel={() => onNavigate({ kind: "patientIndex" })}
        isPending={mutation.isPending}
      />
    </section>
  );
}
