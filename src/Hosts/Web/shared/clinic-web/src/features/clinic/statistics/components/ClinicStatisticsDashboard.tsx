"use client";

import { EmptyStateSurface as ClinicEmptyState } from "@vitarish/web-ui";
import { useLocalizedText } from "../../../../localization";

export function ClinicStatisticsDashboard() {
  const { t } = useLocalizedText();
  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">{t("clinic.statistics.title")}</h1>
        <p className="text-sm text-muted">{t("clinic.statistics.description")}</p>
      </div>

      <ClinicEmptyState title={t("clinic.statistics.comingSoon")} />
    </div>
  );
}
