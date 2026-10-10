"use client";

import { EmptyStateSurface as ClinicEmptyState, HeaderIcon } from "@vitarish/ui";
import { useLocalizedText } from "../../localization/index";

export function StatisticsDashboard() {
  const { t } = useLocalizedText();
  return (
    <div className="flex flex-col gap-6">
      <div>
        <div className="flex items-center gap-4">
          <HeaderIcon icon="chart-line" level="page" />
          <div className="min-w-0">
            <h1 className="text-2xl font-bold">{t("clinic.statistics.title")}</h1>
            <p className="text-sm text-muted">
              {t("clinic.statistics.description")}
            </p>
          </div>
        </div>
      </div>

      <ClinicEmptyState title={t("clinic.statistics.comingSoon")} />
    </div>
  );
}
