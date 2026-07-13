"use client";

import { ErrorStateSurface as ClinicErrorState } from "@vitarish/web-ui";
import type { ClinicNavigationIntent } from "../../../../../navigation";
import { useLocalizedText } from "../../../../../localization";
import { PatientProfileForm } from "../../profile/components/PatientProfileForm";
import { useCreatePatientProfile } from "../../profile/hooks/useCreatePatientProfile";
import { emptyFormData } from "../../profile/models/PatientProfileForm";

export interface ClinicPatientCreatePageProps {
  onNavigate: (intent: ClinicNavigationIntent) => void;
}

export function ClinicPatientCreatePage({ onNavigate }: ClinicPatientCreatePageProps) {
  const { t } = useLocalizedText();
  const mutation = useCreatePatientProfile();

  return (
    <section className="flex flex-col gap-6" aria-labelledby="clinic-patient-create-title">
      <header>
        <h1 id="clinic-patient-create-title" className="text-2xl font-bold">{t("clinic.patients.newPatient")}</h1>
        <p className="text-sm text-muted">{t("clinic.patients.createPatient")}</p>
      </header>
      {mutation.error ? <ClinicErrorState title={t("clinic.patients.createFailed")} /> : null}
      <PatientProfileForm
        initialData={emptyFormData()}
        onSubmit={(data) => {
          void mutation.create(data).then((patient) => {
            if (patient?.id) onNavigate({ kind: "patientDetail", patientId: patient.id });
          }).catch(() => undefined);
        }}
        onCancel={() => onNavigate({ kind: "patientIndex" })}
        isPending={mutation.isPending}
      />
    </section>
  );
}
