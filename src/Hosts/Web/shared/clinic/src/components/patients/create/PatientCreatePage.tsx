"use client";

import { ErrorStateSurface as ClinicErrorState, HeaderIcon } from "@vitarish/ui";
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
      <header>
        <div className="flex items-center gap-4">
          <HeaderIcon icon="user-plus" level="page" />
          <div className="min-w-0">
            <h1 id="clinic-patient-create-title" className="text-2xl font-bold">
              {t("clinic.patients.newPatient")}
            </h1>
            <p className="text-sm text-muted">
              {t("clinic.patients.createPatient")}
            </p>
          </div>
        </div>
      </header>
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
