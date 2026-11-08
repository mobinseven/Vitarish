"use client";

import { PageHeader } from "@vitarish/ui";
import { useLocalizedText } from "../../localization";
import { PatientCheckInWizard } from "./PatientCheckInWizard";

export function CheckInPage() {
  const { t } = useLocalizedText();
  return (
    <section className="mx-auto flex w-full max-w-xl flex-col gap-6" aria-labelledby="clinic-check-in-title">
      <PageHeader
        icon="clipboard-check"
        title={t("clinic.checkIn.title")}
        titleId="clinic-check-in-title"
        details={t("clinic.checkIn.description")}
      />
      <PatientCheckInWizard
        onPatientSelected={() => undefined}
        onCheckInComplete={() => undefined}
      />
    </section>
  );
}
