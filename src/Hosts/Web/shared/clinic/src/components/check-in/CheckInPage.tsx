"use client";

import { HeaderIcon } from "@vitarish/ui";
import { useLocalizedText } from "../../localization";
import { PatientCheckInWizard } from "./PatientCheckInWizard";

export function CheckInPage() {
  const { t } = useLocalizedText();
  return (
    <section className="mx-auto flex w-full max-w-xl flex-col gap-6" aria-labelledby="clinic-check-in-title">
      <header className="flex items-center gap-4">
        <HeaderIcon context="check-in" icon="clipboard-check" level="page" />
        <div className="min-w-0">
          <h1 id="clinic-check-in-title" className="text-2xl font-bold">
            {t("clinic.checkIn.title")}
          </h1>
          <p className="text-sm text-muted">{t("clinic.checkIn.description")}</p>
        </div>
      </header>
      <PatientCheckInWizard
        onPatientSelected={() => undefined}
        onCheckInComplete={() => undefined}
      />
    </section>
  );
}
