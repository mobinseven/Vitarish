"use client";

import { formatDate, useLocalizedText } from "@/localization";
import { AppointmentsWidget } from "../widgets/appointments/AppointmentsWidget";
import { SurgeriesWidget } from "../widgets/surgeries/SurgeriesWidget";
import { InsuranceWidget } from "../widgets/insurance/InsuranceWidget";

export function HomeDashboard() {
  const { t, locale } = useLocalizedText();
  return (
    <div className="flex flex-col gap-4 pb-8 pt-2">
      <header>
        <div className="mb-2 flex items-center gap-2 text-sm font-semibold text-accent">
          <i aria-hidden="true" className="fa-light fa-sparkles" />
          {t("home.dashboard.eyebrow")}
        </div>
        <h1 className="text-3xl font-semibold tracking-tight text-foreground sm:text-4xl">
          {t("home.dashboard.greeting")}
          <i
            aria-hidden="true"
            className="fa-light fa-hand-wave ms-3 text-amber-500"
          />
        </h1>
        <p className="mt-2 max-w-2xl text-base text-muted">
          {t("home.dashboard.dateSummary", {
            date: formatDate(new Date(), locale, { dateStyle: "long" }),
          })}
        </p>
      </header>
      <div className="grid grid-cols-1 items-stretch gap-6 xl:grid-cols-3">
        <AppointmentsWidget />
        <SurgeriesWidget />
        <InsuranceWidget />
      </div>
    </div>
  );
}
