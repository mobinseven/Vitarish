"use client";

import { EmptyStateSurface as ClinicEmptyState, PageHeader } from "@vitarish/ui";
import { pageAncestors, usePageBack } from "../navigation/usePageBack";
import { useLocalizedText } from "../../localization/index";
import type { NavigationIntent } from "../../navigation";

export function StatisticsDashboard({ onNavigate }: { onNavigate?: (intent: NavigationIntent) => void }) {
  const { t } = useLocalizedText();
  const back = usePageBack([pageAncestors.settings], onNavigate);
  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        icon="chart-line"
        title={t("clinic.statistics.title")}
        details={t("clinic.statistics.description")}
        back={back}
      />

      <ClinicEmptyState title={t("clinic.statistics.comingSoon")} />
    </div>
  );
}
